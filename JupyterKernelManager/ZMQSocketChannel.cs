using JupyterKernelManager.Protocol;
using NetMQ;
using System;
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

        public string Send(Message message)
        {
            //var msg = new NetMQMessage(new List<byte[]>() { message.Serialize() });
            NetMQMessage msg = new NetMQMessage();
            //msg.AppendEmptyFrame();
            msg.Append("59e41542-9a1b3708330a10e913e38765");
            msg.Append("<IDS|MSG>");
            msg.AppendEmptyFrame();
            msg.Append(message.Serialize());
            msg.Append(Encoding.ASCII.GetBytes("{}"));
            msg.Append(Encoding.ASCII.GetBytes("{}"));
            msg.Append(Encoding.ASCII.GetBytes("{'test':'test'}"));
            Socket.SendMultipartMessage(msg);
            //Socket.SendMoreFrameEmpty();
            //Socket.SendMoreFrame(Encoding.ASCII.GetBytes("<IDS|MSG>"));
            //Socket.SendMoreFrameEmpty();
            //Socket.SendMoreFrame(message.Serialize());
            //Socket.SendMoreFrameEmpty();
            //Socket.SendMoreFrameEmpty();
            //Socket.SendFrameEmpty();

            // TODO: Finish implementation
            return "";
        }
    }
}
