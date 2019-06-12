using System;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class KernelConnectionTests
    {
        [TestMethod]
        public void IsLocalIp_NullEmpty()
        {
            var connection = new KernelConnection();
            connection.IpAddress = null;
            Assert.IsFalse(connection.IsLocalIp());
            connection.IpAddress = string.Empty;
            Assert.IsFalse(connection.IsLocalIp());
            connection.IpAddress = "    ";
            Assert.IsFalse(connection.IsLocalIp());
        }

        [TestMethod]
        public void IsLocalIp_NotLocal()
        {
            var connection = new KernelConnection();
            connection.IpAddress = "1.2.3.4";
            Assert.IsFalse(connection.IsLocalIp());
        }

        [TestMethod]
        public void IsLocalIp_IsLocal()
        {
            var connection = new KernelConnection();
            connection.IpAddress = KernelConnection.LOCALHOST;
            Assert.IsTrue(connection.IsLocalIp());
        }

        [TestMethod]
        public void Constructor()
        {
            var connection = new KernelConnection();
            Assert.AreEqual(KernelConnection.TCP_TRANSPORT, connection.Transport);
            Assert.AreEqual(KernelConnection.LOCALHOST, connection.IpAddress);
        }

        [TestMethod]
        public void MakeUrl_InvalidChannel()
        {
            var connection = new KernelConnection();
            Assert.ThrowsException<InvalidChannelException>(() => connection.MakeUrl("invalid"));
        }

        [TestMethod]
        public void MakeUrl_Tcp()
        {
            var connection = new KernelConnection()
            {
                Transport = KernelConnection.TCP_TRANSPORT,
                IpAddress = "1.2.3.4",
                ShellPort = 1234,
                IoPubPort = 1235,
                StdinPort = 1236,
                HbPort = 1237,
                ControlPort = 1238
            };
            Assert.AreEqual("tcp://1.2.3.4:1234", connection.MakeUrl("shell"));
            Assert.AreEqual("tcp://1.2.3.4:1234", connection.MakeUrl("ShElL"));  // Case doesn't matter
            Assert.AreEqual("tcp://1.2.3.4:1235", connection.MakeUrl("iopub"));
            Assert.AreEqual("tcp://1.2.3.4:1236", connection.MakeUrl("stdin"));
            Assert.AreEqual("tcp://1.2.3.4:1237", connection.MakeUrl("hb"));
            Assert.AreEqual("tcp://1.2.3.4:1238", connection.MakeUrl("control"));
        }

        [TestMethod]
        public void MakeUrl_NonTcp()
        {
            var connection = new KernelConnection()
            {
                Transport = "file",
                IpAddress = "1.2.3.4",
                ShellPort = 1234,
                IoPubPort = 1235,
                StdinPort = 1236,
                HbPort = 1237,
                ControlPort = 1238
            };
            Assert.AreEqual("file://1.2.3.4-1234", connection.MakeUrl("shell"));
            Assert.AreEqual("file://1.2.3.4-1234", connection.MakeUrl("ShElL"));  // Case doesn't matter
            Assert.AreEqual("file://1.2.3.4-1235", connection.MakeUrl("iopub"));
            Assert.AreEqual("file://1.2.3.4-1236", connection.MakeUrl("stdin"));
            Assert.AreEqual("file://1.2.3.4-1237", connection.MakeUrl("hb"));
            Assert.AreEqual("file://1.2.3.4-1238", connection.MakeUrl("control"));
        }
    }
}
