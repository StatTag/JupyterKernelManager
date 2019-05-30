using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace JupyterKernelManager
{
    public class KernelConnection
    {
        public const string TCP_TRANSPORT = "tcp";
        public const string LOCALHOST = "127.0.0.1";

        [JsonIgnore]
        private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);

        /// <summary>
        /// JSON file in which to store connection info [default: kernel-<pid>.json]
        /// This file will contain the IP, ports, and authentication key needed to connect
        /// clients to this kernel.By default, this file will be created in the security dir
        /// of the current profile, but can be specified by absolute path.
        /// </summary>
        [JsonIgnore]
        public string ConnectionFile { get; set; }

        /// <summary>
        /// The port to use for ROUTER (shell) channel.
        /// </summary>
        [JsonProperty("shell_port")]
        public int ShellPort { get; set; }

        /// <summary>
        /// The port to use for the SUB channel.
        /// </summary>
        [JsonProperty("iopub_port")]
        public int IoPubPort { get; set; }

        /// <summary>
        /// The port to use for the ROUTER (raw input) channel.
        /// </summary>
        [JsonProperty("stdin_port")]
        public int StdinPort { get; set; }

        /// <summary>
        /// The port to use for the heartbeat REP channel.
        /// </summary>
        [JsonProperty("hb_port")]
        public int HbPort { get; set; }

        /// <summary>
        /// The port to use for the ROUTER (control) channel.
        /// </summary>
        [JsonProperty("control_port")]
        public int ControlPort { get; set; }

        /// <summary>
        /// The ip address the kernel will bind to.
        /// </summary>
        [JsonProperty("ip")]
        public string IpAddress { get; set; }

        /// <summary>
        /// The Session key used for message authentication.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// The transport protocol.  Currently only tcp is supported.
        /// </summary>
        [JsonProperty("transport")]
        public string Transport { get; set; }

        /// <summary>
        /// The scheme used for message authentication.
        /// This has the form 'digest-hash', where 'digest'
        /// is the scheme used for digests, and 'hash' is the name of the hash function
        /// used by the digest scheme.
        /// Currently, 'hmac' is the only supported digest scheme,
        /// and 'sha256' is the default hash function.
        /// </summary>
        [JsonProperty("signature_scheme")]
        public string SignatureScheme { get; set; }

        /// <summary>
        /// The name of the kernel currently connected to.
        /// </summary>
        [JsonProperty("kernel_name")]
        public string KernelName { get; set; }

        /// <summary>
        /// Tracks if the connection file was written or not
        /// </summary>
        [JsonIgnore]
        private bool ConnectionFileWritten { get; set; }

        public KernelConnection()
        {
            Transport = TCP_TRANSPORT;
            IpAddress = LOCALHOST;
            ConnectionFileWritten = false;
        }

        /// <summary>
        /// Generates a JSON config file, including the selection of random ports.
        /// </summary>
        /// <param name="connectionFilePath"></param>
        public void WriteConnectionFile()
        {
            if (ConnectionFileWritten && File.Exists(ConnectionFile))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                IpAddress = LOCALHOST;
            }

            // default to temporary connector file
            if (string.IsNullOrWhiteSpace(ConnectionFile))
            {
                ConnectionFile = Path.GetTempFileName() + ".json";
            }

            // Find open ports as necessary.
            if (string.Equals(Transport, TCP_TRANSPORT, StringComparison.CurrentCultureIgnoreCase))
            {
                ShellPort = EnsureTcpPortSet(ShellPort);
                IoPubPort = EnsureTcpPortSet(IoPubPort);
                StdinPort = EnsureTcpPortSet(StdinPort);
                ControlPort = EnsureTcpPortSet(ControlPort);
                HbPort = EnsureTcpPortSet(HbPort);
            }
            else
            {
                // Code not yet translated: https://github.com/jupyter/jupyter_client/blob/44980c13680f4e4226cf25f199ce4e4bb6e11296/jupyter_client/connect.py#L107-L113
                throw new NotSupportedException("We currently do not support non-TCP transport");
            }

            using (var file = File.CreateText(ConnectionFile))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, this);
            }

            ConnectionFileWritten = true;
        }

        /// <summary>
        /// Cleanup connection file *if we wrote it*
        /// Will not raise if the connection file was already removed somehow.
        /// </summary>
        public void CleanupConnectionFile()
        {
            if (!ConnectionFileWritten)
            {
                return;
            }

            // cleanup connection files on full shutdown of kernel we started
            try
            {
                File.Delete(ConnectionFile);
            }
            catch (Exception exc)
            {
                // Purposefully eating the exception
            }
        }

        /// <summary>
        /// Take a port value and if it is not correctly set, get the next available TCP port.
        /// </summary>
        /// <param name="port"></param>
        private int EnsureTcpPortSet(int port)
        {
            if (port <= 0)
            {
                port = GetAvailableTcpPort();
            }

            return port;
        }

        /// <summary>
        /// Code from: https://stackoverflow.com/a/49408267/5670646
        /// </summary>
        /// <returns></returns>
        public static int GetAvailableTcpPort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(DefaultLoopbackEndpoint);
                int port = ((IPEndPoint)socket.LocalEndPoint).Port;
                socket.Close();
                return port;
            }
        }

        /// <summary>
        /// Determine if the currently set IpAddress value represents a local IP address
        /// </summary>
        /// <returns></returns>
        public bool IsLocalIp()
        {
            return string.Equals(LOCALHOST, IpAddress);
        }
    }
}
