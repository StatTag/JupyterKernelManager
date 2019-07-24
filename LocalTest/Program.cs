using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using JupyterKernelManager;
using NetMQ;
using NetMQ.Sockets;

namespace LocalTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var manager = new KernelSpecManager();
            var kernelSpecs = manager.GetAllSpecs();
            Console.WriteLine("Enumerating all kernels");
            foreach (var kernelSpec in kernelSpecs)
            {
                Console.WriteLine("   Found {0} at {1}", kernelSpec.Key, kernelSpec.Value.ResourceDirectory);
            }

            Console.WriteLine("Launching first kernel");
            using (var kernelManager = new KernelManager(kernelSpecs.First().Key))
            {
                kernelManager.StartKernel();
                var client = kernelManager.CreateClient();
                client.StartChannels();

                client.Execute("x <- 1; x");

                Pause();
;
                client.StopChannels();
            }
            
        }

        static void Pause()
        {
            Console.WriteLine("Sleeping...");
            Thread.Sleep(5000);
            Console.WriteLine("Done sleeping");
        }
    }
}
