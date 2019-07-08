using JupyterKernelManager.Protocol;
using NetMQ;
using Newtonsoft.Json;
using System;
using Microsoft.Jupyter.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    /// <summary>
    /// A ZMQ socket invoking a callback in the ioloop
    /// </summary>
    public class ZMQSocketChannel
    {
        public NetMQSocket Socket { get; set; }
        public bool IsAlive { get; set; }

        /// <summary>
        /// Create a channel.
        /// </summary>
        /// <param name="socket">The NetMQ socket to use</param>
        /// <param name="ioloop">The zmq IO loop to connect the socket to using a ZMQStream</param>
        public ZMQSocketChannel(NetMQSocket socket)
        {
            Socket = socket;
        }

        public void Start()
        {
            IsAlive = true;
        }

        public void Stop()
        {
            IsAlive = false;
        }

        public void Close()
        {
            if (Socket == null)
            {
                return;
            }

            Socket.Close();
            Socket = null;
        }

        public void Send(Message message)
        {
            var zmqMessage = new NetMQMessage();
            var frames = message.SerializeFrames();
            var digest = message.NewAuth().ComputeHash(frames);

            message.ZmqIdentities?.ForEach(ident => zmqMessage.Append(ident));
            zmqMessage.Append("<IDS|MSG>");
            zmqMessage.Append(BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant());
            frames.ForEach(ident => zmqMessage.Append(ident));
            Socket.SendMultipartMessage(zmqMessage);

            // TODO: Finish implementation
        }

        public void Receive()
        {

        }
    }
}
