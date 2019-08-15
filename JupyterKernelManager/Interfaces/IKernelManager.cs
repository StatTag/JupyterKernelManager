using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public interface IKernelManager
    {
        bool IsAlive { get; }
        bool HasKernel { get; }
        KernelConnection ConnectionInformation { get; set; }
        KernelClient CreateClient();
    }
}
