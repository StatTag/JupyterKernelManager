using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;

namespace JupyterKernelManager
{
    /// <summary>
    /// Manages a single kernel in a subprocess on this host.
    /// </summary>
    public class KernelManager : IDisposable, IKernelManager
    {
        private const string EXTRA_ARGUMENTS = "extra_arguments";

        /// <summary>
        /// Time to wait for a kernel to terminate before killing it, in milliseconds
        /// </summary>
        private const int SHUTDOWN_WAIT_TIME = 5000;

        private const int WAIT_CYCLE_DURATION = 100;

        private const int HASH_KEY_LENGTH = 64;  // Default length expected by .NET HMAC function

        private IChannelFactory ChannelFactory { get; set; }
        private Session ClientSession { get; set; }
        private IKernelSpecManager SpecManager { get; set; }
        private KernelSpec Spec { get; set; }
        private HashHelper HashHelper { get; set; }

        public KernelConnection ConnectionInformation { get; set; }
        private List<string> KernelCmd { get; set; }
        private Process Kernel { get; set; }

        // Used to manage sending control messages and receiving responses from the kernel
        private IChannel ControlChannel { get; set; }
        private Thread ControlThread { get; set; }

        private Dictionary<string, List<string>> LaunchArgs { get; set; }
        private Dictionary<string, string> ExtraEnvironment { get; set; }

        private bool ShutdownSuccess { get; set; }
        private object ShutdownSync = new object();

        /// <summary>
        /// Construct a manager for a given Jupyter kernel
        /// </summary>
        /// <param name="kernelName">The name of the Jupyter kernel to start and manager</param>
        public KernelManager(string kernelName)
        {
            Initialize(kernelName, null, null);
        }

        /// <summary>
        /// Construct a manager for a given Jupyter kernel.  This version is typically used by unit tests.
        /// </summary>
        /// <param name="kernelName">The name of the Jupyter kernel to start and manager</param>
        /// <param name="specManager">The manager for the kernelspecs</param>
        /// <param name="channelFactory">A factory class to create ZMQ channels</param>
        public KernelManager(string kernelName, IKernelSpecManager specManager, IChannelFactory channelFactory)
        {
            Initialize(kernelName, specManager, channelFactory);
        }

        private void Initialize(string kernelName, IKernelSpecManager specManager, IChannelFactory channelFactory)
        {
            this.HashHelper = new HashHelper();
            this.SpecManager = specManager ?? (new KernelSpecManager());
            this.Spec = SpecManager.GetKernelSpec(kernelName);
            this.ConnectionInformation = new KernelConnection();
            this.ChannelFactory = channelFactory;
        }

        /// <summary>
        /// Clean up the KernelManager's underlying connections and resources
        /// </summary>
        public void Dispose()
        {
            // If we fail to send the shutdown request, proceed with killing the kernel process
            // directly.
            if (!SendShutdownRequest())
            {
                KillKernelProcess();
            }

            if (ConnectionInformation != null)
            {
                ConnectionInformation.CleanupConnectionFile();
            }

            // Ensure all of our objects are cleaned up and nulled out
            ControlChannel = null;
            ControlThread = null;
            Kernel = null;
        }

        /// <summary>
        /// Send a request over the control channel to shut down the kernel
        /// </summary>
        /// <returns>True if successful, False otherwise</returns>
        private bool SendShutdownRequest()
        {
            if (ControlChannel == null)
            {
                return false;
            }
            else
            {
                try
                {
                    var message = ClientSession.CreateMessage(MessageType.ShutdownRequest);
                    message.Content = new ExpandoObject();
                    message.Content.restart = false;
                    ControlChannel.Send(message);

                    // Wait up to SHUTDOWN_WAIT_TIME seconds for a response.  We are going to poll in small increments
                    // to see if we have gotten a response.
                    int waitCycles = SHUTDOWN_WAIT_TIME / WAIT_CYCLE_DURATION;
                    for (int index = 0; index < waitCycles; index++)
                    {
                        lock (ShutdownSync)
                        {
                            if (ShutdownSuccess)
                            {
                                return true;
                            }
                        }

                        Thread.Sleep(WAIT_CYCLE_DURATION);
                    }
                }
                catch (Exception exc)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Forcibly kill the kernel process
        /// </summary>
        private void KillKernelProcess()
        {
            if (Kernel != null)
            {
                if (!Kernel.CloseMainWindow() && !Kernel.HasExited)
                {
                    Kernel.Kill();
                }
                Kernel = null;
            }
        }

        /// <summary>
        /// Starts a kernel on this host in a separate process.
        /// If random ports(port= 0) are being used, this method must be called
        /// before the channels are created.
        /// </summary>
        public void StartKernel(Dictionary<string, List<string>> kw = null)
        {
            if (string.Equals(ConnectionInformation.Transport, KernelConnection.TCP_TRANSPORT)
                && !ConnectionInformation.IsLocalIp())
            {
                throw new Exception(string.Format("Can only launch a kernel on a local interface.  This one is not: {0}.  Make sure that the '*_address' attributes are configured properly.",
                    ConnectionInformation.IpAddress));
            }

            ConnectionInformation.Key = HashHelper.NewIdBytes(false, HASH_KEY_LENGTH);
            ConnectionInformation.KernelName = this.Spec.DisplayName;
            ConnectionInformation.SignatureScheme = SignatureScheme.HmacSha256;
            ConnectionInformation.WriteConnectionFile();

            // Save args for use in restart
            LaunchArgs = (kw == null) ? new Dictionary<string, List<string>>() : new Dictionary<string, List<string>>(kw);

            // Build the Popen cmd
            List<string> extraArguments = null;
            if (kw != null && kw.ContainsKey(EXTRA_ARGUMENTS))
            {
                extraArguments = kw[EXTRA_ARGUMENTS];
            }

            var kernelCmd = FormatKernelCmd(extraArguments);
            var envVarDict = Environment.GetEnvironmentVariables();
            IDictionary<string, string> env = new Dictionary<string, string>();
            foreach (var ev in envVarDict.OfType<DictionaryEntry>())
            {
                env.Add((string)ev.Key, (string)ev.Value);
            }
            // Don't allow PYTHONEXECUTABLE to be passed to kernel process.
            // If set, it can bork all the things.
            env.Remove("PYTHONEXECUTABLE");
            if (KernelCmd == null || KernelCmd.Count == 0)
            {
                // If kernel_cmd has been set manually, don't refer to a kernel spec
                // Environment variables from kernel spec are added to os.environ
                env = MergeDicts(env, Spec.Environment);
            }
            else if (ExtraEnvironment != null)
            {
                env = MergeDicts(env, ExtraEnvironment);
            }

            Kernel = LaunchKernel(kernelCmd, env, kw);

            this.ClientSession = new Session(ConnectionInformation.Key);
            this.ChannelFactory = this.ChannelFactory ?? new ZMQChannelFactory(ConnectionInformation, ClientSession);
            CreateControlChannel();
        }

        /// <summary>
        /// Establishes the connection to the control socket, which allows us to send important kernel messages like shutdown.
        /// <remarks>Given the nature of these commands, this channel is only available to the overall kernel manager, and not each
        /// individual client.</remarks>
        /// </summary>
        private void CreateControlChannel()
        {
            if (ControlChannel != null)
            {
                throw new Exception("The channel has already been initialized");
            }

            ControlChannel = ChannelFactory.CreateChannel(ChannelNames.Control);
            ControlChannel.Start();
            ControlThread = new Thread(() => EventLoop(ControlChannel))
            {
                Name = "Control Channel"
            };
            ControlThread.Start();
        }

        private void EventLoop(IChannel channel)
        {
            try
            {
                while (IsAlive)
                {
                    // Try to get the next response message from the kernel.  Note that we are using the non-blocking,
                    // so the IsAlive check ensures we continue to poll for results.
                    var nextMessage = channel.TryReceive();
                    if (nextMessage == null)
                    {
                        continue;
                    }

                    // If this is our first message, we need to set the session id.
                    if (ClientSession == null)
                    {
                        throw new NullReferenceException("The client session must be established, but is null");
                    }
                    else if (string.IsNullOrEmpty(ClientSession.SessionId))
                    {
                        ClientSession.SessionId = nextMessage.Header.Session;
                    }

                    if (nextMessage.Header.MessageType.Equals(MessageType.ShutdownReply))
                    {
                        lock (ShutdownSync)
                        {
                            ShutdownSuccess = true;
                        }
                    }
                }
            }
            catch (ProtocolViolationException ex)
            {
                Console.WriteLine("Protocol violation when trying to receive next ZeroMQ message.");
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }
        }

        /// <summary>
        /// Launches a localhost kernel, binding to the specified ports.
        /// </summary>
        /// <param name="cmd">A string of Python code that imports and executes a kernel entry point.</param>
        /// <param name="env">Environment variables passed to the kernel</param>
        /// <param name="kw">Additional arguments for Popen</param>
        /// <returns>Popen instance for the kernel subprocess</returns>
        private Process LaunchKernel(List<string> cmd, IDictionary<string, string> env, Dictionary<string, List<string>> kw)
        {
            var pid = WinApi.GetCurrentProcess();
            IntPtr handle = IntPtr.Zero;
            WinApi.DuplicateHandle(pid, pid, pid, out handle, 0, true, (uint)WinApi.DuplicateOptions.DUPLICATE_SAME_ACCESS);

            //var process = new Process();
            //process.StartInfo = new ProcessStartInfo(string.Join(" ", cmd));
            //process.StartInfo.UseShellExecute = false;
            var process = Process.Start(cmd[0], string.Join(" ", cmd.Skip(1)));
            return process;
        }

        /// <summary>
        /// replace templated args (e.g. {connection_file})
        /// </summary>
        /// <param name="extraArguments"></param>
        /// <returns></returns>
        public List<string> FormatKernelCmd(List<string> extraArguments = null)
        {
            extraArguments = extraArguments ?? new List<string>();

            List<string> cmd = null;
            if (KernelCmd == null || KernelCmd.Count == 0)
            {
                 cmd = Spec.Arguments.Concat(extraArguments).ToList();
            }
            else
            {
                cmd = KernelCmd.Concat(extraArguments).ToList();
            }

            var ns = new Dictionary<string, string>();
            ns.Add("connection_file", ConnectionInformation.ConnectionFile);
            if (Spec != null)
            {
                ns.Add("resource_dir", Spec.ResourceDirectory);
            }

            var pattern = new Regex("\\{([A-Za-z0-9_]+)\\}");
            for (int index = 0; index < cmd.Count; index++)
            {
                var arg = cmd[index];
                var match = pattern.Match(arg);
                if (match.Success && ns.ContainsKey(match.Groups[1].Value))
                {
                    cmd[index] = ns[match.Groups[1].Value];
                }
            }
            
            return cmd;
        }

        public IDictionary<string, string> MergeDicts(IDictionary<string, string> dict1, IDictionary<string, string> dict2)
        {
            if (dict1 == null)
            {
                return dict2;
            }
            else if (dict2 == null)
            {
                return dict1;
            }

            var dictionary = new Dictionary<string, string>(dict1);
            foreach (var kvp in dict2)
            {
                if (!dictionary.ContainsKey(kvp.Key))
                {
                    dictionary.Add(kvp.Key, kvp.Value);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Is the kernel process still running?
        /// </summary>
        /// <returns></returns>
        public bool IsAlive
        {
            get { return HasKernel && !Kernel.HasExited; }
        }

        /// <summary>
        /// Has a kernel been started that we are managing.
        /// </summary>
        /// <returns></returns>
        public bool HasKernel
        {
            get { return Kernel != null; }
        }

        /// <summary>
        /// Create a Client connected to our Kernel
        /// </summary>
        /// <returns></returns>
        public KernelClient CreateClient()
        {
            return new KernelClient(this, ChannelFactory, true);
        }
    }
}
