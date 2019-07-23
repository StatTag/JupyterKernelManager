using JupyterKernelManager.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class KernelClient
    {
        private KernelManager Parent { get; set; }

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

        private ZMQSocketChannel _ShellChannel { get; set; }
        private ZMQSocketChannel _IoPubChannel { get; set; }
        private ZMQSocketChannel _StdInChannel { get; set; }
        private HeartbeatChannel _HbChannel { get; set; }

        private Thread StdInThread { get; set; }
        private Thread ShellThread { get; set; }

        public KernelClient(KernelManager parent)
        {
            this.Parent = parent;
            this.ClientSession = new Session(Connection.Key);
        }

        /// <summary>
        /// Flag for whether execute requests should be allowed to call raw_input
        /// </summary>
        public bool AllowStdin { get; set; }

        public void GetShellMsg()
        {
            // TODO: Implement
        }

        public void GetIoPubMsg()
        {
            // TODO: Implement
        }

        public void GetStdinMsg()
        {
            // TODO: Implement
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

        private void EventLoop(ZMQSocketChannel channel)
        {
            while (this.IsAlive)
            {
                try
                {
                    // Start by pulling off the next <action>_request message
                    // from the client.
                    var nextMessage = channel.Receive();
                    // If this is our first message, we need to set the session id.
                    if (ClientSession == null)
                    {
                        throw new NullReferenceException("The client session must be established, but is null");
                    }
                    else if (string.IsNullOrEmpty(ClientSession.SessionId))
                    {
                        ClientSession.SessionId = nextMessage.Header.Session;
                    }
                }
                catch (ProtocolViolationException ex)
                {
                    Console.WriteLine("Protocol violation when trying to receive next ZeroMQ message.");
                }
                catch (ThreadInterruptedException)
                {
                    if (!IsAlive)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Stops all the running channels for this kernel.
        /// This stops their event loops and joins their threads.
        /// </summary>
        public void StopChannels()
        {
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
        public ZMQSocketChannel ShellChannel
        {
            get
            {
                if (_ShellChannel == null)
                {
                    var socket = Connection.ConnectShell();
                    _ShellChannel = new ZMQSocketChannel(socket, ClientSession);
                }

                return _ShellChannel;
            }
        }

        /// <summary>
        /// Get the iopub channel object for this kernel.
        /// </summary>
        public ZMQSocketChannel IoPubChannel
        {
            get
            {
                if (_IoPubChannel == null)
                {
                    var socket = Connection.ConnectIoPub();
                    _IoPubChannel = new ZMQSocketChannel(socket, ClientSession);
                }

                return _IoPubChannel;
            }
        }

        /// <summary>
        /// Get the stdin channel object for this kernel.
        /// </summary>
        public ZMQSocketChannel StdInChannel
        {
            get
            {
                if (_StdInChannel == null)
                {
                    var socket = Connection.ConnectStdin();
                    _StdInChannel = new ZMQSocketChannel(socket, ClientSession);
                }

                return _StdInChannel;
            }
        }

        /// <summary>
        /// Get the heartbeat channel object for this kernel.
        /// </summary>
        public ZMQSocketChannel HbChannel
        {
            get
            {
                if (_HbChannel == null)
                {
                    var socket = Connection.ConnectHb();
                    _HbChannel = new HeartbeatChannel(socket, ClientSession);
                }

                return _HbChannel;
            }
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

                if (_HbChannel != null)
                {
                    // We don't have access to the KernelManager,
                    // so we use the heartbeat.
                    return _HbChannel.IsBeating;
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
            // TODO - Implement
            var message = ClientSession.CreateMessage(MessageType.KernelInfoRequest);
            _ShellChannel.Send(message);
            return message.Header.Id;
        }

        // TODO - https://github.com/jupyter/jupyter_client/blob/1cec38633c049d916f5e65d4d74129737ee9851e/jupyter_client/client.py#L200
    }
}
