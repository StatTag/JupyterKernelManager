using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class KernelSpecManager
    {
        private JupyterPaths Paths = new JupyterPaths();

        public string DataDirectory { get; set; }
        public string UserKernelDirectory { get; set; }

        /// <summary>
        /// List of kernel directories to search. Later ones take priority over earlier."
        /// </summary>
        public List<string> KernelDirectories { get; set; }

        private string DefaultDataDirectory()
        {
            return Paths.GetDataDir();
        }

        private string DefaultUserKernelDirectory()
        {
            return Path.Combine(DataDirectory, "kernels");
        }

        private List<string> DefaultKernelDirectories()
        {
            return Paths.GetPath("kernels");
        }
    }
}
