using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using JupyterKernelManager.Interfaces;
using Microsoft.Win32;

namespace JupyterKernelManager
{
    public class RegistryService : IRegistryService
    {
        /// <summary>
        /// Find the first descendant key that matches a search string
        /// </summary>
        /// <param name="parentKey">Where to start looking.  Do not include the base part of the key, since multiple will be tried.</param>
        /// <param name="match">The substring to match against in a case-insensitive manner</param>
        /// <returns>The name of the matching key, if one is found.  Null otherwise</returns>
        public string FindFirstDescendantKeyMatching(string parentKey, string match)
        {
            var subKey = FindFirstDescendantKeyMatching(RegistryHive.LocalMachine, parentKey, match);
            if (subKey == null)
            {
                subKey = FindFirstDescendantKeyMatching(RegistryHive.CurrentUser, parentKey, match);
            }
            return subKey;
        }

        /// <summary>
        /// Return a string value for a given key
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="valueName"></param>
        /// <returns></returns>
        public string GetStringValue(string keyName, string valueName)
        {
            return (string)Registry.GetValue(keyName, valueName, "");
        }

        private string FindFirstDescendantKeyMatching(RegistryHive rootKey, string parentKey, string match)
        {
            var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            var keyParts = parentKey.Split(new []{'\\'});
            for (var index = 0; index < keyParts.Length; index++)
            {
                key = key.OpenSubKey(keyParts[index]);
                if (key == null)
                {
                    break;
                }
            }

            return GetDescendantKeyMatching(key, match).Name;
        }

        private RegistryKey GetDescendantKeyMatching(RegistryKey key, string match)
        {
            if (key != null)
            {
                var subKeyNames = key.GetSubKeyNames();
                foreach (var subKeyName in subKeyNames)
                {
                    if (subKeyName.Contains(match))
                    {
                        return key.OpenSubKey(subKeyName);
                    }
                    else
                    {
                        var foundKey = GetDescendantKeyMatching(key.OpenSubKey(subKeyName), match);
                        if (foundKey != null)
                        {
                            return foundKey;
                        }
                    }
                }
            }

            return null;
        }
    }
}
