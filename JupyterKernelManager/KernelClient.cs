using JupyterKernelManager.Protocol;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;

namespace JupyterKernelManager
{
    /// <summary>
    /// Communicates with a single kernel on any host via zmq channels.
    /// 
    /// There are four channels associated with each kernel:
    ///   * shell: for request/reply calls to the kernel.
    ///   * iopub: for the kernel to publish results to frontends.
    ///   * hb: for monitoring the kernel's heartbeat.
    ///   * stdin: for frontends to reply to raw_input calls in the kernel.
    ///   
    /// The messages that can be sent on these channels are exposed as methods of the
    /// client (KernelClient.execute, complete, history, etc.). These methods only
    /// send the message, they don't wait for a reply. To get results, use e.g.
    /// :meth:`get_shell_msg` to fetch messages from the shell channel.
    /// </summary>
    public class KernelClient : IDisposable
    {
        private IKernelManager Parent { get; set; }
        private IChannelFactory ChannelFactory { get; set; }

        private KernelConnection Connection
        {
            get
            {
                if (Parent == null)
                {
                    throw new NullReferenceException(
                        "The parent KernelManager needs to be set in order to establish connections");
                }

                return Parent.ConnectionInformation;
            }
        }

        private Session ClientSession { get; set; }

        private IChannel _ShellChannel { get; set; }
        private IChannel _IoPubChannel { get; set; }
        private IChannel _StdInChannel { get; set; }
        private IChannel _HbChannel { get; set; }

        private Thread StdInThread { get; set; }
        private Thread ShellThread { get; set; }
        private Thread IoPubThread { get; set; }

        private object ExecuteLogSync = new object();
        public Dictionary<string, ExecutionEntry> ExecuteLog { get; private set; }

        public KernelClient(IKernelManager parent, bool autoStartChannels = true)
        {
            Initialize(parent, null, autoStartChannels);
        }

        public KernelClient(IKernelManager parent, IChannelFactory channelFactory, bool autoStartChannels = true)
        {
            Initialize(parent, channelFactory, autoStartChannels);
        }

        /// <summary>
        /// Internal initialization method to create all necessary members
        /// </summary>
        /// <param name="parent">The parent kernel manager that created us.</param>
        /// <param name="channelFactory">Factory used to create the various channels</param>
        /// <param name="autoStartChannels">If true, will automatically start the channels to the kernel</param>
        private void Initialize(IKernelManager parent, IChannelFactory channelFactory, bool autoStartChannels)
        {
            this.Parent = parent;
            this.ClientSession = new Session(Connection.Key);
            this.ExecuteLog = new Dictionary<string, ExecutionEntry>();
            this.ChannelFactory = channelFactory ?? new ZMQChannelFactory(Connection, ClientSession);

            if (autoStartChannels)
            {
                StartChannels();
            }
        }

        /// <summary>
        /// Flag for whether execute requests should be allowed to call raw_input
        /// </summary>
        public bool AllowStdin { get; set; }

        public void Execute(string code)
        {
            var message = ClientSession.CreateMessage(MessageType.ExecuteRequest);
            message.Content = new ExpandoObject();
            message.Content.code = code;
            message.Content.silent = false;
            message.Content.store_history = true;
            message.Content.allow_stdin = false;
            message.Content.stop_on_error = true;
            _ShellChannel.Send(message);

            // We start tracking an execute request once it is sent on the shell channel.
            lock (ExecuteLogSync)
            {
                ExecuteLog.Add(message.Header.Id, new ExecutionEntry() { Request = message });
            }
        }

        /// <summary>
        /// Check if there are any execute requests that have no response yet
        /// </summary>
        /// <returns></returns>
        public bool HasPendingExecute()
        {
            return GetPendingExecuteCount() > 0;
        }

        /// <summary>
        /// Get the number of execute requests that have no response yet
        /// </summary>
        /// <returns></returns>
        public int GetPendingExecuteCount()
        {
            lock (ExecuteLogSync)
            {
                return ExecuteLog.Count(x => !x.Value.Complete);
            }
        }

        /// <summary>
        /// Reset the log of code execute requests that took place.  This will clear ALL entries,
        /// including those that may not have been completed yet.  To avoid this, call <see cref="HasPendingExecute"/>
        /// before clearing the log.
        /// </summary>
        public void ClearExecuteLog()
        {
            ExecuteLog = new Dictionary<string, ExecutionEntry>();
        }

        /// <summary>
        /// Starts the channels for this kernel.
        /// 
        /// This will create the channels if they do not exist and then start
        /// them(their activity runs in a thread). If port numbers of 0 are
        /// being used(random ports) then you must first call
        /// :meth:`start_kernel`. If the channels have been stopped and you
        /// call this, :class:`RuntimeError` will be raised.
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="iopub"></param>
        /// <param name="stdin"></param>
        /// <param name="hb"></param>
        public void StartChannels(bool shell = true, bool iopub = true, bool stdin = true, bool hb = true)
        {
            if (shell)
            {
                ShellChannel.Start();

                ShellThread = new Thread(() => EventLoop(ShellChannel))
                {
                    Name = "Shell Channel"
                };
                ShellThread.Start();
            }

            if (iopub)
            {
                IoPubChannel.Start();

                IoPubThread = new Thread(() => EventLoop(IoPubChannel))
                {
                    Name = "IoPub Channel"
                };
                IoPubThread.Start();
            }

            if (stdin)
            {
                StdInChannel.Start();
                AllowStdin = true;

                StdInThread = new Thread(() => EventLoop(StdInChannel))
                {
                    Name = "StdIn Channel"
                };
                StdInThread.Start();

            }
            else
            {
                AllowStdin = false;
            }

            if (hb)
            {
                HbChannel.Start();
            }

            // Now that the channels have started, collect the kernel information
            if (shell && ShellChannel.IsAlive)
            {
                KernelInfo();
            }
        }

        private void EventLoop(IChannel channel)
        {
            try
            {
                while (this.IsAlive)
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

                    // From the message, check to see if it is related to an execute request.  If so, we need to track
                    // that a response is back.
                    var messageType = nextMessage.Header.MessageType;
                    bool isExecuteReply = messageType.Equals(MessageType.ExecuteReply);
                    bool hasData = nextMessage.IsDataMessageType();
                    if (isExecuteReply || hasData)
                    {
                        lock (ExecuteLogSync)
                        {
                            // No parent header means we can't confirm the message identity, so we will skip the result.
                            if (nextMessage.ParentHeader == null)
                            {
                                continue;
                            }

                            var messageId = nextMessage.ParentHeader.Id;
                            if (ExecuteLog.ContainsKey(messageId))
                            {
                                ExecuteLog[messageId].Complete = true;
                                ExecuteLog[messageId].Response.Add(nextMessage);

                                // If we have an execution reply, we can get the execution index from the message
                                if (isExecuteReply)
                                {
                                    ExecuteLog[messageId].ExecutionIndex = nextMessage.Content.execution_count;
                                }
                            }
                        }
                    }
                }
            }
            catch (ProtocolViolationException ex)
            {
                Console.WriteLine("Protocol violation when trying to receive next ZeroMQ message.");
                return;
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
        /// Stops all the running channels for this kernel.
        /// This stops their event loops and joins their threads.
        /// </summary>
        public void StopChannels()
        {
            // Close down the threads polling for results
            if (StdInThread != null && StdInThread.IsAlive)
            {
                StdInThread.Interrupt();
            }
            if (ShellThread != null && ShellThread.IsAlive)
            {
                ShellThread.Interrupt();
            }
            if (IoPubThread != null && IoPubThread.IsAlive)
            {
                IoPubThread.Interrupt();
            }

            // Stop all the channel sockets
            if (ShellChannel.IsAlive)
            {
                ShellChannel.Stop();
            }
            if (IoPubChannel.IsAlive)
            {
                IoPubChannel.Stop();
            }
            if (StdInChannel.IsAlive)
            {
                StdInChannel.Stop();
            }
            if (HbChannel.IsAlive)
            {
                HbChannel.Stop();
            }

            // Join any threads that existed
            if (StdInThread != null)
            {
                StdInThread.Join();
            }
            if (ShellThread != null)
            {
                ShellThread.Join();
            }
            if (IoPubThread != null)
            {
                IoPubThread.Join();
            }

            // Clean up any threads
            StdInThread = null;
            ShellThread = null;
            IoPubThread = null;
        }

        /// <summary>
        /// Are any of the channels created and running?
        /// </summary>
        /// <returns></returns>
        public bool ChannelsRunning
        {
            get
            {
                return ShellChannel.IsAlive || IoPubChannel.IsAlive || StdInChannel.IsAlive || HbChannel.IsAlive;
            }
        }

        /// <summary>
        /// Get the shell channel object for this kernel.
        /// </summary>
        public IChannel ShellChannel
        {
            get { return _ShellChannel ?? (_ShellChannel = ChannelFactory.CreateChannel(ChannelNames.Shell)); }
        }

        /// <summary>
        /// Get the iopub channel object for this kernel.
        /// </summary>
        public IChannel IoPubChannel
        {
            get { return _IoPubChannel ?? (_IoPubChannel = ChannelFactory.CreateChannel(ChannelNames.IoPub)); }
        }

        /// <summary>
        /// Get the stdin channel object for this kernel.
        /// </summary>
        public IChannel StdInChannel
        {
            get { return _StdInChannel ?? (_StdInChannel = ChannelFactory.CreateChannel(ChannelNames.StdIn)); }
        }

        /// <summary>
        /// Get the heartbeat channel object for this kernel.
        /// </summary>
        public IChannel HbChannel
        {
            get { return _HbChannel ?? (_HbChannel = ChannelFactory.CreateChannel(ChannelNames.Heartbeat)); }
        }

        public bool IsAlive
        {
            get
            {
                if (Parent != null)
                {
                    // This KernelClient was created by a KernelManager, and so
                    // we can ask the parent KernelManager:
                    return Parent.IsAlive;
                }

                if (_HbChannel != null && _HbChannel is HeartbeatChannel)
                {
                    // We don't have access to the KernelManager,
                    // so we use the heartbeat.
                    return ((HeartbeatChannel)_HbChannel).IsBeating;
                }

                // no heartbeat and not local, we can't tell if it's running,
                // so naively return True
                return true;
            }
        }

        /// <summary>
        /// Request kernel info
        /// </summary>
        /// <returns>The msg_id of the message sent</returns>
        public string KernelInfo()
        {
            var message = ClientSession.CreateMessage(MessageType.KernelInfoRequest);
            _ShellChannel.Send(message);
            return message.Header.Id;
        }

        /// <summary>
        /// Perform cleanup on all open resources (threads, channels)
        /// </summary>
        public void Dispose()
        {
            StopChannels();
        }

        // TODO - https://github.com/jupyter/jupyter_client/blob/1cec38633c049d916f5e65d4d74129737ee9851e/jupyter_client/client.py#L200
    }
}
