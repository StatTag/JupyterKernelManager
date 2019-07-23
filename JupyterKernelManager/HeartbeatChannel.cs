using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class HeartbeatChannel : ZMQSocketChannel
    {
        public bool IsBeating { get; set; }

        public HeartbeatChannel(NetMQSocket socket, Session session) : base(socket, session)
        {
            IsBeating = false;
        }
    }
}
