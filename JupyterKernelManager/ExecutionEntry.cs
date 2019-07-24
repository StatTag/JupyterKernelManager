using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class ExecutionEntry
    {
        public bool Complete { get; set; }
        public bool Error { get; set; }
        public Message Request { get; set; }
        public Message Response { get; set; }
    }
}
