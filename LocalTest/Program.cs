using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        class ConsoleLogger : ILogger
        {
            public void Write(string message, params object[] parameters)
            {
                Write(LogLevel.Default, message, parameters);
            }

            public void Write(int logLevel, string message, params object[] parameters)
            {
                Console.WriteLine(message, parameters);
            }
        }

        static void Main(string[] args)
        {
            var manager = new KernelSpecManager();
            var kernelSpecs = manager.GetAllSpecs();
            Console.WriteLine("Enumerating all kernels");
            foreach (var kernelSpec in kernelSpecs)
            {
                Console.WriteLine("   Found {0} at {1}", kernelSpec.Key, kernelSpec.Value.ResourceDirectory);
            }

            // Multiple iterations to help with testing for intermittent errors
            for (int counter = 0; counter < 50; counter++)
            {
                RunKernel("ir", new string[]
                {
                    "x <- 100; x",
                    "y <- 25",
                    "x + y",
                    "library(tableone)",
                    "Sys.sleep(5)",
                    "x * y"
                });
                //RunKernel("matlab", new string[]
                //{
                //    "x = 100; disp(x)",
                //    "y = 25;",
                //    "disp(x + y)"
                //});
                RunKernel("python3", new string[]
                {
                    "x = 100; print(x)",
                    "y = 25;",
                    "print(y)",
                    "import time\r\ntime.sleep(5)\r\nprint(x * y)",
                    "print(x + y)"
                });
            }
        }

        static void RunKernel(string name, string[] code)
        {
            Console.WriteLine("{0} Kernel", name);
            using (var kernelManager = new KernelManager(name, new ConsoleLogger()))
            {
                kernelManager.Debug = true;
                kernelManager.StartKernel();
                using (var client = kernelManager.CreateClientAndWaitForConnection(3, 5))
                {
                    foreach (var block in code)
                    {
                        Console.WriteLine("Sending code block...");
                        client.Execute(block);
                        Console.WriteLine("Code block sent");
                        while (client.HasPendingExecute() && client.IsAlive)
                        {
                            Console.WriteLine("... Waiting ...");
                            Pause();
                        }

                        if (client.HasExecuteError())
                        {
                            Console.WriteLine("*** THERE WAS AN ERROR DURING EXECUTION - Stopping further execution");
                            break;
                        }
                    }

                    // Now echo out everything we did
                    var executeLog = client.ExecuteLog.Values.OrderBy(x => x.ExecutionIndex);
                    foreach (var entry in executeLog)
                    {
                        Console.WriteLine("Item {0} ------------------------------------------", entry.ExecutionIndex);
                        Console.WriteLine(entry.Request.Content.code);
                        Console.WriteLine();

                        if (entry.Abandoned)
                        {
                            Console.WriteLine("  !! This code had to be abandoned !!");
                        }
                        else if (entry.Error)
                        {
                            var errorResponse = entry.Response.FirstOrDefault(
                                x => x.Header.MessageType.Equals(MessageType.Error));
                            Console.WriteLine(errorResponse.Content);
                        }
                        else
                        {
                            var dataResponse = entry.Response.FirstOrDefault(
                                x => x.Header.MessageType.Equals(MessageType.DisplayData) ||
                                     x.Header.MessageType.Equals(MessageType.Stream) ||
                                     x.Header.MessageType.Equals(MessageType.ExecuteResult));
                            if (dataResponse == null)
                            {
                                Console.WriteLine("  ( No data returned for this code block )");
                            }
                            else
                            {
                                Console.WriteLine(dataResponse.Content);
                            }
                        }

                        Console.WriteLine("--------------------------------------------------\r\n");
                    }
                }
            }
            Console.WriteLine();
        }

        static void Pause()
        {
            Thread.Sleep(500);
        }
    }
}
