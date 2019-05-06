﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JupyterKernelManager;

namespace LocalTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var manager = new KernelSpecManager();
            var kernelSpecs = manager.GetAllSpecs();
            foreach (var kernelSpec in kernelSpecs)
            {
                Console.WriteLine("Found {0} at {1}", kernelSpec.Key, kernelSpec.Value.ResourceDirectory);
            }
        }
    }
}
