using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class NoSuchKernelException : Exception
    {
        public NoSuchKernelException(string kernelName) : base(string.Format("Could not find the kernel: {0}", kernelName))
        {

        }
    }
}
