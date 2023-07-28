using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace JupyterKernelManager.Interfaces
{
    public interface IRegistryService
    {
        string FindFirstDescendantKeyNameMatching(string parentKey, string match);
        RegistryKey FindFirstDescendantKeyMatching(string parentKey, string match);
    }
}
