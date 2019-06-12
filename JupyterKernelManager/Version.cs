using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class Version
    {
        public const int Major = 5;
        public const int Minor = 3;

        public static readonly string ProtocolVersion = string.Format("{0}.{1}", Major, Minor);
    }
}
