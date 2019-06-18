using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using JupyterKernelManager;
using NetMQ;
using NetMQ.Sockets;

namespace LocalTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //NetMQMessage msg = new NetMQMessage();
            ////msg.AppendEmptyFrame();
            //msg.Append("<IDS|MSG>");
            //msg.AppendEmptyFrame();
            //msg.Append(Encoding.ASCII.GetBytes("{}"));
            //msg.Append(Encoding.ASCII.GetBytes("{}"));
            //msg.Append(Encoding.ASCII.GetBytes("{}"));
            //msg.Append(Encoding.ASCII.GetBytes("{}"));

            //var socket = new DealerSocket();
            //socket.Options.Linger = TimeSpan.FromSeconds(1);
            //socket.Connect("tcp://127.0.0.1:51382");

            //socket.SendMultipartMessage(msg);

            var manager = new KernelSpecManager();
            var kernelSpecs = manager.GetAllSpecs();
            //foreach (var kernelSpec in kernelSpecs)
            //{
            //    Console.WriteLine("Found {0} at {1}", kernelSpec.Key, kernelSpec.Value.ResourceDirectory);
            //}

            Console.WriteLine("Launching first kernel");
            using (var kernelManager = new KernelManager(kernelSpecs.First().Key))
            {
                kernelManager.StartKernel();
                var client = kernelManager.CreateClient();
                client.StartChannels();
            }
            //Console.WriteLine("Exiting");
        }
    }
}
