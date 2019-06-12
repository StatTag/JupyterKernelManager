using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    /// <summary>
    /// Launch a kernel by name in a local subprocess.
    /// </summary>
    public class KernelApp
    {
        /// <summary>
        /// The name of a kernel type to start
        /// </summary>
        private string KernelName { get; set; }

        private KernelManager Manager { get; set; }

        public KernelApp(string kernelName)
        {
            KernelName = kernelName;
            Manager = new KernelManager(kernelName);
        }
    }
}
