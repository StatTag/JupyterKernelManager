using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
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

        private ILogger Logger { get; set; }
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

        private bool debug = false;

        public bool Debug
        {
            get { return debug; }
            set
            {
                // If it's not changed, don't do anything different
                if (value == debug)
                {
                    return;
                }

                // If we're turning debugging off or turning it on, either way we want to create
                // the DebugLog builder
                DebugLog = new StringBuilder();
                debug = value;
            }
        }
        public StringBuilder DebugLog { get; set; }

        /// <summary>
        /// Construct a manager for a given Jupyter kernel
        /// </summary>
        /// <param name="kernelName">The name of the Jupyter kernel to start and manager</param>
        public KernelManager(string kernelName, ILogger logger = null)
        {
            Initialize(kernelName, null, null, logger);
        }

        /// <summary>
        /// Construct a manager for a given Jupyter kernel.  This version is typically used by unit tests.
        /// </summary>
        /// <param name="kernelName">The name of the Jupyter kernel to start and manager</param>
        /// <param name="specManager">The manager for the kernelspecs</param>
        /// <param name="channelFactory">A factory class to create ZMQ channels</param>
        public KernelManager(string kernelName, IKernelSpecManager specManager, IChannelFactory channelFactory, ILogger logger = null)
        {
            Initialize(kernelName, specManager, channelFactory, logger);
        }

        private void Initialize(string kernelName, IKernelSpecManager specManager, IChannelFactory channelFactory, ILogger logger)
        {
            this.Logger = logger ?? new DefaultLogger();
            this.HashHelper = new HashHelper();
            this.SpecManager = specManager ?? (new KernelSpecManager());
            this.Spec = SpecManager.GetKernelSpec(kernelName);
            this.ConnectionInformation = new KernelConnection();
            this.ChannelFactory = channelFactory;

            this.Debug = false;
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

        private void WriteDebugLog(string message)
        {
            if (Debug)
            {
                DebugLog.AppendLine(message);
            }
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
                    Logger.Write("Exception when trying to shut down {0}", exc.Message);
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
                if (!Kernel.HasExited && !Kernel.CloseMainWindow())
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

            // Some recommendations have been to give the kernel process half a second
            // to get established, otherwise we run the risk of trying to connect our
            // sockets to it and nothing is there.
            // https://github.com/zeromq/netmq/issues/482
            Thread.Sleep(500);

            this.ClientSession = new Session(ConnectionInformation.Key);
            this.ChannelFactory = this.ChannelFactory ?? new ZMQChannelFactory(ConnectionInformation, ClientSession, Logger);
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

        /// <summary>
        /// Event loop processor for the control channel's processing thread.
        /// </summary>
        /// <param name="channel"></param>
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
                Logger.Write("Protocol violation when trying to receive next ZeroMQ message: {0}", ex.Message);
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
            catch (SocketException se)
            {
                Logger.Write("Socket exception {0}", se.Message);
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
            if (Debug) { WriteDebugLog(string.Format("Launching command {0}", cmd[0])); }
            var info = new ProcessStartInfo(cmd[0], string.Join(" ", cmd.Skip(1)))
            {
                CreateNoWindow = !Debug,  // Lots of negatives here.  If we're debugging, we want a window (not no window).
                UseShellExecute = false
            };
            info = AdjustLaunchCommandForAnaconda(info, Debug);

            if (Debug) { WriteDebugLog(string.Format("Actually launching:\r\n{0}\r\n{1}", info.FileName, info.Arguments)); }
            var process = Process.Start(info);
            return process;
        }

        /// <summary>
        /// Perform a safe test launching a process given a process/command and arguments.
        /// </summary>
        /// <param name="command">The process/command to run (e.g., C:\Path\program.exe)</param>
        /// <param name="arguments">Optional command line arguments for the process (e.g., -h -r)</param>
        /// <param name="debug">If we have enabled debugging</param>
        /// <returns>true if the process launched, false otherwise</returns>
        public static bool TestLaunchProcess(string command, string arguments, bool debug)
        {
            var processInfo = new ProcessStartInfo(command, arguments)
                { CreateNoWindow = !debug, UseShellExecute = false  };

            try
            {
                var process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit(1000);
                }

                // If the process ended and had a successful exit code, we will consider
                // it a successful test.  Otherwise, we assume it failed.  This is useful
                // if the attempt to launch works, but the process isn't actually there.
                // This is an issue with Python on Windows 11, where Microsoft would let
                // the Python process be successfully called ev
                if (process.HasExited && process.ExitCode == 0)
                {
                    return true;
                }
            }
            catch (Exception exc)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Determine if we need to adjust the Jupyter kernel path to get this to launch in an Anaconda-only
        /// installation.  This is currently isolated to Python-based kernels that are launched.
        /// </summary>
        /// <param name="info">The original process information created to launch the kernel</param>
        /// <param name="debug">If debugging is enabled</param>
        /// <returns>The corrected process information and arguments if modified, or the original process information</returns>
        public static ProcessStartInfo AdjustLaunchCommandForAnaconda(ProcessStartInfo info, bool debug = false)
        {
            // The special processing is for Anaconda installations of Python.  Let's see if we fall into that bucket.
            var anacondaPythonPath = KernelManager.GetAnacondaPythonPath();
            if (!string.IsNullOrWhiteSpace(anacondaPythonPath) && Directory.Exists(anacondaPythonPath))
            {
                // We have an Anaconda path.  Now let's see if we can launch Jupyter from the command line.
                // If we can, we're going to assume the environment is all set.  If not, we will assume we
                // need to use our special Anaconda startup.
                if (!TestLaunchProcess("python.exe", "-h", debug))
                {
                    // Fallback plan is to format the command to activate the anaconda environment and then
                    // run the command.
                    var newArguments = string.Format("/C {0}\\condabin\\activate.bat {0} & {1} {2}", anacondaPythonPath, info.FileName, info.Arguments);
                    info.FileName = "cmd.exe";
                    info.Arguments = newArguments;
                }
            }

            return info;
        }

        /// <summary>
        /// Utility method to retrieve an Anaconda python path, if one exists.
        /// </summary>
        /// <returns></returns>
        private static string GetAnacondaPythonPath()
        {
            // This is the old way.  Very straight forward.
            var regSvc = new RegistryService();
            var key = regSvc.FindFirstDescendantKeyNameMatching("SOFTWARE\\Python", "Anaconda");
            if (key != null)
            {
                return regSvc.GetStringValue(key + "\\" + "InstallPath", null);
            }

            // Now Anaconda will install as a regular Python (which makes sense).  However, this
            // means we have to guess from the folder name if it's Anaconda vs. a general Python
            key = regSvc.FindFirstDescendantKeyNameMatching("SOFTWARE\\Python", "InstallPath");
            if (key != null)
            {
                var path = regSvc.GetStringValue(key, null);
                if (!string.IsNullOrWhiteSpace(path) && path.ToLower().Contains("anaconda"))
                {
                    return path;
                }
            }

            return null;
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
            return new KernelClient(this, ChannelFactory, true, Logger);
        }

        /// <summary>
        /// Creates a kernel client and waits for some confirmation that we are connected.
        /// Retry creating the client if there is a hiccup.
        /// </summary>
        /// <param name="retries">Number of retries to establish a client connection</param>
        /// <param name="timeout">Number of seconds to wait before retrying</param>
        /// <returns>The KernelClient if connected, null if there was an error</returns>
        public KernelClient CreateClientAndWaitForConnection(uint retries = 3, uint timeout = 10)
        {
            Logger.Write("Creating client");
            var client = CreateClient();

            uint retryCounter = 1;
            double sleepCounter = 0.0;
            while (!client.IsKernelConnected)
            {
                if (sleepCounter >= timeout)
                {
                    Logger.Write("Stopping client");
                    client.StopChannels();
                    client = null;

                    Logger.Write("Recreating client to retry");
                    client = CreateClient();
                    sleepCounter = 0.0;
                    retryCounter++;
                }

                if (retryCounter >= retries)
                {
                    break;
                }

                Thread.Sleep(100);  // Just a little nap while we wait
                sleepCounter += 0.1; // Increment by our sleep amount (0.1 second)
            }

            return client;
        }
    }
}
