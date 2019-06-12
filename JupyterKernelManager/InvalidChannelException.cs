using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class InvalidChannelException : Exception
    {
        public InvalidChannelException(string error) : base(error) {}
    }
}
