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

        private Thread HeartbeatThread { get; set; }
        private const int TimeToDead = 1;

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

            HeartbeatThread = new Thread(() => Poll(this))
            {
                Name = "Heartbeat Channel"
            };
            HeartbeatThread.Start();
        }

        public override void Stop()
        {
            if (HeartbeatThread != null)
            {
                HeartbeatThread.Interrupt();
                HeartbeatThread.Join(1000);
                HeartbeatThread = null;
            }
            base.Stop();
        }

        /// <summary>
        /// poll for heartbeat replies until we reach self.time_to_dead.
        /// Ignores interrupts, and returns the result of poll(), which
        /// will be an empty list if no messages arrived before the timeout,
        /// or the event tuple if there is a message to receive.
        /// </summary>
        /// <param name="channel"></param>
        private void Poll(IChannel channel)
        {
            try
            {
                using (var poller = new NetMQPoller() {this.Socket})
                {
                    NetMQTimer heartbeatTimer = new NetMQTimer(1000);
                    poller.Add(heartbeatTimer);
                    heartbeatTimer.Elapsed += (sender, args) =>
                    {
                        Console.WriteLine("Sending heartbeat");
                        this.Socket.SendFrame(HeartbeatMessage);
                        while (!Receive())
                        {
                            Thread.Sleep(1000);
                        }
                    };
                    poller.Run();
                    //this.Socket.ReceiveReady += (sender, args) =>
                    //{
                    //    IsBeating = this.Receive();
                    //    Console.WriteLine("Heartbeat: {0}", IsBeating);
                    //    if (!IsBeating)
                    //    {
                    //        IsAlive = false;
                    //    }
                    //};
                    //poller.RunAsync();

                    //while (this.IsAlive)
                    //{
                    //    this.Socket.SendFrame(HeartbeatMessage);
                    //    Thread.Sleep(1000 * TimeToDead);
                    //}
                    //IsAlive = true;
                    //int failureCount = 0;

                    //while (this.IsAlive)
                    //{
                    //    this.Socket.SendFrame(HeartbeatMessage);
                    //    Thread.Sleep(1000 * TimeToDead);
                    //    IsBeating = this.Receive();
                    //    if (!IsBeating)
                    //    {
                    //        failureCount++;
                    //    }

                    //    if (failureCount < 2)
                    //    {
                    //        Console.WriteLine("Retrying heartbeat");
                    //        continue;
                    //    }

                    //    Console.WriteLine("Heartbeat: {0}", IsBeating);

                    //    if (!IsBeating)
                    //    {

                    //        IsAlive = false;
                    //        break;
                    //    }

                    //    Thread.Sleep(1000 * TimeToDead);
                    //}

                    //poller.StopAsync();
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
            catch (Exception)
            {
                IsAlive = false;
                IsBeating = false;
            }
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
                if (Socket.TryReceiveMultipartBytes(TimeSpan.FromSeconds(TimeToDead), ref rawFrames))
                {
                    var response = rawFrames.Select(frame => Encoding.GetString(frame)).FirstOrDefault();
                    return string.Equals(response, HeartbeatMessage);
                }

                return false;
            }
        }
    }
}
