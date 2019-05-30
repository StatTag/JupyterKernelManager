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
    }
}
