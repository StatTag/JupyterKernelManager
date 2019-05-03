﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class JupyterPaths
    {
        private Dictionary<string, string> tempDirectories = new Dictionary<string, string>();

        public List<string> GetPath()
        {
            var paths = new List<string>();

            // highest priority is environment variable
            var env = Environment.GetEnvironmentVariable("JUPYTER_PATH");
            if (!string.IsNullOrWhiteSpace(env))
            {
                paths.Add(env);
            }

            // then user dir
            paths.Add(GetDataDir());

            // finally, system
            paths.Add(GetSystemPath());

            return paths;
        }

        /// <summary>
        /// Get the system Jupyter path
        /// </summary>
        /// <returns></returns>
        public string GetSystemPath()
        {
            var env = Environment.GetEnvironmentVariable("PROGRAMDATA");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return Path.Combine(env, "jupyter");
            }

            return null;
        }

        /// <summary>
        /// Get the config directory for Jupyter data files.
        ///
        /// These are non-transient, non-configuration files.
        /// </summary>
        /// <returns>Returns JUPYTER_DATA_DIR if defined, else a platform-appropriate path.</returns>
        public string GetDataDir()
        {
            var env = Environment.GetEnvironmentVariable("JUPYTER_DATA_DIR");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env;
            }

            var home = GetHomeDir();
            var appData = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrWhiteSpace(appData))
            {
                return Path.Combine(appData, "jupyter");
            }
            else
            {
                return Path.Combine(GetConfigDir(), "data");
            }
        }

        /// <summary>
        /// Get the Jupyter config directory for this platform and user.
        /// </summary>
        /// <returns>Returns JUPYTER_CONFIG_DIR if defined, else ~/.jupyter</returns>
        public string GetConfigDir()
        {
            var homeDir = GetHomeDir();
            var env = Environment.GetEnvironmentVariable("JUPYTER_NO_CONFIG");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return MakeTempDirOnce("jupyter-clean-cfg");
            }

            env = Environment.GetEnvironmentVariable("JUPYTER_CONFIG_DIR");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env;
            }

            return Path.Combine(homeDir, ".jupyter");
        }

        /// <summary>
        /// Make or reuse a temporary directory.
        /// If this is called with the same name in the same process, it will return
        /// the same directory.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string MakeTempDirOnce(string name)
        {
            if (!tempDirectories.ContainsKey(name))
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), name + "-" + Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                tempDirectories[name] = tempDirectory;
            }

            return tempDirectories[name];
        }

        /// <summary>
        /// Get the real path of the home directory
        /// </summary>
        /// <returns></returns>
        public string GetHomeDir()
        {
            // Solution from https://stackoverflow.com/q/1143706/5670646
            string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            return homePath;
        }
    }
}
