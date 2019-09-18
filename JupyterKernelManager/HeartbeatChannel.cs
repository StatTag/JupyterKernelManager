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
        private const int TimeToDead = 1000;

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
        public HeartbeatChannel(NetMQSocket socket, Session session) : base(ChannelNames.Heartbeat, socket, session)
        {
            IsBeating = false;
            IsAlive = false;
        }

        public override void Start()
        {
            base.Start();

            lock (syncObj)
            {
                Poller = new NetMQPoller() {this.Socket};
            }

            HeartbeatTimer = new NetMQTimer(HeartbeatFrequency);
            HeartbeatTimer.Elapsed += (sender, args) =>
            {
                lock (syncObj)
                {
                    Console.WriteLine("Sending heartbeat");
                    var startTime = DateTime.UtcNow;
                    this.Socket.SendFrame(HeartbeatMessage);
                    while (!Receive())
                    {
                        var now = DateTime.UtcNow;
                        if (now.Subtract(startTime).TotalMilliseconds >= TimeToDead)
                        {
                            Console.WriteLine("No heartbeat response in at least {0} seconds", TimeToDead);
                            IsBeating = false;
                            if (HeartbeatTimer != null)
                            {
                                Console.WriteLine("Removing heartbeat timer");
                                HeartbeatTimer.Enable = false;
                            }
                            break;
                        }
                    }

                    Console.WriteLine("Leaving lock zone");
                }
                Console.WriteLine("Leaving event handler");
            };
            Console.WriteLine("Adding heartbeat timer");
            Poller.Add(HeartbeatTimer);
            Console.WriteLine("Starting to run async");
            Poller.RunAsync();
            Console.WriteLine("Continuing on");
        }

        public override void Stop()
        {
            Console.WriteLine("Stopping");
            if (Poller != null)
            {
                Console.WriteLine("Disabling timer");
                HeartbeatTimer.Enable = false;
                Console.WriteLine("Removing timer");
                Poller.Remove(HeartbeatTimer);
                Console.WriteLine("Setting timer to null");
                HeartbeatTimer = null;
                Console.WriteLine("Stopping poller");
                Poller.Stop();
                Console.WriteLine("Setting poller to null");
                Poller = null;
            }

            Console.WriteLine("Base stop to clean up socket");
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
