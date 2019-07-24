using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace JupyterKernelManager
{
    /// <summary>
    /// Implementation notes:
    /// The original version includes references to a NATIVE_KERNEL_NAME, which relates to the version of Python installed.
    /// We don't expect an analagous setup in a .NET implementation, so none of that code was implemented here.
    /// </summary>
    public class KernelSpecManager : IKernelSpecManager
    {
        private static readonly Regex KernelNamePattern = new Regex("^[a-z0-9._\\-]+$", RegexOptions.IgnoreCase);
        private JupyterPaths Paths = new JupyterPaths();

        public string DataDirectory { get; set; }
        public string UserKernelDirectory { get; set; }

        /// <summary>
        /// List of kernel directories to search. Later ones take priority over earlier."
        /// </summary>
        public List<string> KernelDirectories { get; set; }

        public KernelSpecManager()
        {
            DataDirectory = DefaultDataDirectory();
            UserKernelDirectory = DefaultUserKernelDirectory();
            KernelDirectories = DefaultKernelDirectories();
        }

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

        /// <summary>
        /// Returns a dict mapping kernel names to kernelspecs.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, KernelSpec> GetAllSpecs()
        {
            var kernelSpecs = new Dictionary<string, KernelSpec>();
            var specs = FindKernelSpecs();
            foreach (KeyValuePair<string, string> kvp in specs)
            {
                var spec = GetKernelSpecByName(kvp.Key, kvp.Value);
                kernelSpecs[kvp.Key] = spec;
            }
            return kernelSpecs;
        }

        /// <summary>
        /// Returns a dict mapping kernel names to resource directories
        /// </summary>
        public Dictionary<string, string> FindKernelSpecs()
        {
            var specs = new Dictionary<string, string>();
            foreach (var kernelDir in KernelDirectories)
            {
                var kernels = ListKernelsIn(kernelDir);
                foreach (KeyValuePair<string,string> kvp in kernels)
                {
                    if (!specs.ContainsKey(kvp.Key))
                    {
                        specs.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return specs;
        }

        /// <summary>
        /// Returns a KernelSpec instance for the given kernel name.
        /// </summary>
        /// <param name="kernelName"></param>
        /// <returns></returns>
        public KernelSpec GetKernelSpec(string kernelName)
        {
            if (!IsValidKernelName(kernelName))
            {
                throw new ArgumentOutOfRangeException("The kernelspec name is invalid");
            }

            var resourceDir = FindSpecDirectory(kernelName.ToLower());
            if (string.IsNullOrWhiteSpace(resourceDir))
            {
                throw new NoSuchKernelException(kernelName);
            }

            return GetKernelSpecByName(kernelName, resourceDir);
        }

        /// <summary>
        /// Find the resource directory of a named kernel spec
        /// </summary>
        /// <param name="kernelName"></param>
        /// <returns></returns>
        private string FindSpecDirectory(string kernelName)
        {
            foreach (var kernelDir in KernelDirectories)
            {
                try
                {
                    var dirs = Directory.GetDirectories(kernelDir);
                    foreach (var dir in dirs)
                    {
                        var dirKernelName = GetKernelNameFromDir(dir, kernelDir);
                        if (dirKernelName.ToLower().Equals(kernelName) && IsKernelDir(dir))
                        {
                            return dir;
                        }
                    }
                }
                catch (DirectoryNotFoundException exc)
                {
                    // We want to silently continue if a directory wasn't found - this can happen
                    continue;
                }
            }

            throw new NoSuchKernelException(kernelName);
        }

        /// <summary>
        /// Return a KernelSpec instance for a given kernelName and resourceDir
        /// </summary>
        /// <param name="kernelName"></param>
        /// <param name="resourceDir"></param>
        /// <returns></returns>
        private KernelSpec GetKernelSpecByName(string kernelName, string resourceDir)
        {
            // There is a lot of missing implementation code here from the original Python version.  It is
            // checking for native kernels, which we are ignoring given our implementation and environment.
            // We are keeping the same method signature to match the original code, however.
            return KernelSpec.FromResourceDir(resourceDir);
        }

        /// <summary>
        /// Return a mapping of kernel names to resource directories from dir.
        /// If dir is None or does not exist, returns an empty dict.
        /// </summary>
        /// <param name="kernelDir"></param>
        /// <returns></returns>
        private Dictionary<string, string> ListKernelsIn(string kernelDir)
        {
            var kernels = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(kernelDir) || !Directory.Exists(kernelDir))
            {
                return kernels;
            }

            foreach (var directoryPath in Directory.GetDirectories(kernelDir))
            {
                if (!IsKernelDir(directoryPath))
                {
                    continue;
                }

                var kernelName = GetKernelNameFromDir(directoryPath, kernelDir);
                if (!IsValidKernelName(kernelName))
                {
                    // TODO: Logging?
                    string.Format("Invalid kernelspec directory name ({0}): Kernel names can only contain ASCII letters and numbers and these separators: - . _ (hyphen, period, and underscore).",
                        kernelName);
                }

                kernels[kernelName] = directoryPath;
            }

            return kernels;
        }

        /// <summary>
        /// Create a name for the kernel given the directory we found it at
        /// </summary>
        /// <param name="kernelDir">The full path of the kernel</param>
        /// <param name="baseKernelDir">The base path where we were looking for kernels (where kernelDir is located)</param>
        /// <returns></returns>
        public string GetKernelNameFromDir(string kernelDir, string baseKernelDir)
        {
            int dirLen = ((baseKernelDir.EndsWith("\\") || baseKernelDir.EndsWith("/")) ? baseKernelDir.Length : baseKernelDir.Length + 1);
            var kernelName = kernelDir.Remove(0, dirLen).ToLower();
            return kernelName;
        }

        /// <summary>
        /// Is ``path`` a kernel directory?
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsKernelDir(string path)
        {
            return Directory.Exists(path) && File.Exists(Path.Combine(path, KernelSpec.KERNEL_DEFINITION_FILE));    
        }

        /// <summary>
        /// Check that a kernel name is valid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsValidKernelName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return KernelNamePattern.IsMatch(name);
        }
    }
}
