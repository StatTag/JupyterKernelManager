using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    /// <summary>
    /// Provides a heartbeat connection with the kernel to ensure it is alive and well
    /// </summary>
    public class HeartbeatChannel : ZMQSocketChannel
    {
        /// <summary>
        /// Is the heartbeat beating
        /// </summary>
        public bool IsBeating { get; set; }

        /// <summary>
        /// Used to poll for responses in response to our heartbeat ping
        /// </summary>
        private NetMQPoller Poller { get; set; }

        /// <summary>
        /// Timer used to facilitate heartbeat pings on a set basis
        /// </summary>
        private NetMQTimer HeartbeatTimer { get; set; }

        /// <summary>
        /// How long we can wait (in milliseconds) before getting a reply to our heartbeat ping.
        /// Once this time elapses, we consider the kernel dead.
        /// </summary>
        private const int TimeToDead = 1500;

        /// <summary>
        /// How often (in milliseconds) we send a heartbeat message to the kernel.
        /// </summary>
        private const int HeartbeatFrequency = 1000;

        /// <summary>
        /// The payload used for heartbeat messages to the kernel
        /// </summary>
        private const string HeartbeatMessage = "ping";

        /// <summary>
        /// Constructor for the heartbeat channel
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="session"></param>
        public HeartbeatChannel(NetMQSocket socket, Session session, ILogger logger = null) : base(ChannelNames.Heartbeat, socket, session, logger)
        {
            IsBeating = false;
            IsAlive = false;
        }

        public override void Start()
        {
            // We're not "alive" until we start tracking.
            IsAlive = false;

            lock (syncObj)
            {
                Poller = new NetMQPoller() {this.Socket};
            }

            HeartbeatTimer = new NetMQTimer(HeartbeatFrequency);
            HeartbeatTimer.Elapsed += (sender, args) =>
            {
                lock (syncObj)
                {
                    Logger.Write("Sending heartbeat");
                    this.Socket.SendFrame(HeartbeatMessage);
                    var startTime = DateTime.UtcNow;
                    Thread.Sleep(TimeToDead);
                    while (!Receive())
                    {
                        var now = DateTime.UtcNow;
                        if (now.Subtract(startTime).TotalMilliseconds >= TimeToDead)
                        {
                            Logger.Write("No heartbeat response in at least {0} seconds", TimeToDead);
                            IsBeating = false;
                            if (HeartbeatTimer != null)
                            {
                                Logger.Write("Removing heartbeat timer");
                                HeartbeatTimer.Enable = false;
                            }
                            break;
                        }
                    }

                    Logger.Write("Leaving lock zone");
                }
                Logger.Write("Leaving event handler");
            };
            Logger.Write("Adding heartbeat timer");
            Poller.Add(HeartbeatTimer);
            Logger.Write("Starting to run async");
            Poller.RunAsync();
            Logger.Write("Continuing on");

            // We've got everything hooked up.  We consider our heartbeat alive and beating until
            // proven otherwise.
            IsAlive = true;
            IsBeating = true;
        }

        public override void Stop()
        {
            Logger.Write("Stopping");
            if (Poller != null)
            {
                Logger.Write("Disabling timer");
                HeartbeatTimer.Enable = false;
                Logger.Write("Removing timer");
                Poller.Remove(HeartbeatTimer);
                Logger.Write("Setting timer to null");
                HeartbeatTimer = null;
                Logger.Write("Stopping poller");
                Poller.Stop();
                Logger.Write("Setting poller to null");
                Poller = null;
            }

            Logger.Write("Base stop to clean up socket");
            base.Stop();
        }
        
        /// <summary>
        /// Receive the heartbeat response from the kernel
        /// </summary>
        /// <returns></returns>
        public new bool Receive()
        {
            lock (syncObj)
            {
                // If the socket has been cleaned up, we should not continue
                if (Socket == null || !base.IsAlive)
                {
                    return false;
                }

                var rawFrames = new List<byte[]>();
                if (Socket.TryReceiveMultipartBytes(TimeSpan.FromMilliseconds(TimeToDead), ref rawFrames))
                {
                    var response = rawFrames.Select(frame => Encoding.GetString(frame)).FirstOrDefault();
                    return string.Equals(response, HeartbeatMessage);
                }

                return false;
            }
        }
    }
}
