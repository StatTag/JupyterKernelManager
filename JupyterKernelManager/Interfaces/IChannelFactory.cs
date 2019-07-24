using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;

namespace JupyterKernelManager
{
    public interface IChannelFactory
    {
        IChannel CreateChannel(string name);
    }
}
