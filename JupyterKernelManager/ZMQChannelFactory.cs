using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetMQ;

namespace JupyterKernelManager
{
    public class ZMQChannelFactory : IChannelFactory
    {
        public KernelConnection Connection { get; set; }
        public Session ClientSession { get; set; }

        public ZMQChannelFactory(KernelConnection connection, Session session)
        {
            Connection = connection;
            ClientSession = session;
        }

        /// <summary>
        /// Create a ZeroMQ channel for the given Jupyter channel name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IChannel CreateChannel(string name)
        {
            switch (name)
            {
                case ChannelNames.Shell:
                    return new ZMQSocketChannel(ChannelNames.Shell, Connection.ConnectShell(), ClientSession);
                case ChannelNames.IoPub:
                    return new ZMQSocketChannel(ChannelNames.IoPub, Connection.ConnectIoPub(), ClientSession);
                case ChannelNames.StdIn:
                    return new ZMQSocketChannel(ChannelNames.StdIn, Connection.ConnectStdin(), ClientSession);
                case ChannelNames.Heartbeat:
                    return new HeartbeatChannel(Connection.ConnectHb(), ClientSession);
                case ChannelNames.Control:
                    return new ZMQSocketChannel(ChannelNames.Control, Connection.ConnectControl(), ClientSession);
                default:
                    throw new ArgumentOutOfRangeException($"Unable to create unknown channel {name}");
            }
        }
    }
}
