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
        public ILogger Logger { get; set; }
        public KernelConnection Connection { get; set; }
        public Session ClientSession { get; set; }

        public ZMQChannelFactory(KernelConnection connection, Session session, ILogger logger = null)
        {
            Connection = connection;
            ClientSession = session;
            Logger = logger;
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
                    return new ZMQSocketChannel(ChannelNames.Shell, Connection.ConnectShell(), ClientSession, Logger);
                case ChannelNames.IoPub:
                    return new ZMQSocketChannel(ChannelNames.IoPub, Connection.ConnectIoPub(), ClientSession, Logger);
                case ChannelNames.StdIn:
                    return new ZMQSocketChannel(ChannelNames.StdIn, Connection.ConnectStdin(), ClientSession, Logger);
                case ChannelNames.Heartbeat:
                    return new HeartbeatChannel(Connection.ConnectHb(), ClientSession, Logger);
                case ChannelNames.Control:
                    return new ZMQSocketChannel(ChannelNames.Control, Connection.ConnectControl(), ClientSession, Logger);
                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unable to create unknown channel {0}", name));
            }
        }
    }
}
