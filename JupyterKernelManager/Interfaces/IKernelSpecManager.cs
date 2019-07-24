using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public interface IKernelSpecManager
    {
        string DataDirectory { get; set; }
        string UserKernelDirectory { get; set; }

        /// <summary>
        /// List of kernel directories to search. Later ones take priority over earlier."
        /// </summary>
        List<string> KernelDirectories { get; set; }

        /// <summary>
        /// Returns a dict mapping kernel names to kernelspecs.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, KernelSpec> GetAllSpecs();

        /// <summary>
        /// Returns a dict mapping kernel names to resource directories
        /// </summary>
        Dictionary<string, string> FindKernelSpecs();

        /// <summary>
        /// Returns a KernelSpec instance for the given kernel name.
        /// </summary>
        /// <param name="kernelName"></param>
        /// <returns></returns>
        KernelSpec GetKernelSpec(string kernelName);

        /// <summary>
        /// Create a name for the kernel given the directory we found it at
        /// </summary>
        /// <param name="kernelDir">The full path of the kernel</param>
        /// <param name="baseKernelDir">The base path where we were looking for kernels (where kernelDir is located)</param>
        /// <returns></returns>
        string GetKernelNameFromDir(string kernelDir, string baseKernelDir);

        /// <summary>
        /// Check that a kernel name is valid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool IsValidKernelName(string name);
    }
}
