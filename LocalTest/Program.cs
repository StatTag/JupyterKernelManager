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

                client.Execute("x <- 100; x");
                client.Execute("y <- 25");
                client.Execute("x + y");

                while (client.HasPendingExecute())
                {
                    Pause();
                }
;
                client.StopChannels();

                // Now echo out everything we did
                var executeLog = client.ExecuteLog.Values.OrderBy(x => x.ExecutionIndex);
                foreach (var entry in executeLog)
                {
                    Console.WriteLine("Item {0} ------------------------------------------", entry.ExecutionIndex);
                    Console.WriteLine(entry.Request.Content.code);
                    Console.WriteLine();

                    var dataResponse = entry.Response.FirstOrDefault(x => x.Header.MessageType.Equals(MessageType.DisplayData));
                    if (dataResponse == null)
                    {
                        Console.WriteLine("  ( No data returned for this code block )");
                    }
                    else
                    {
                        Console.WriteLine(dataResponse.Content);
                    }
                    Console.WriteLine("--------------------------------------------------\r\n");
                }
            }
        }

        static void Pause()
        {
            Console.WriteLine("Sleeping...");
            Thread.Sleep(500);
        }
    }
}
