using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public interface IChannel
    {
        void Start();
        void Stop();
        void Send(Message message);
        Message TryReceive();
        Message Receive();

        bool IsAlive { get; set; }
    }
}
