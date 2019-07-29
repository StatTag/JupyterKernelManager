using System;
using System.Security.Cryptography.X509Certificates;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Tests
{
    [TestClass]
    public class KernelClientTests
    {
        private readonly Mock<IKernelManager> KernelManagerMock = new Mock<IKernelManager>();
        private readonly Mock<IChannelFactory> ChannelFactoryMock = new Mock<IChannelFactory>();
        private readonly KernelConnection ConnectionInformationFake = new KernelConnection();
        private readonly Mock<IKernelSpecManager> KernelSpecManagerMock = new Mock<IKernelSpecManager>();
        private readonly Mock<IChannel> ChannelMock = new Mock<IChannel>();

        [TestInitialize]
        public void Initialize()
        {
            ConnectionInformationFake.Key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };
            KernelSpecManagerMock.Setup(x => x.GetKernelSpec(It.IsAny<string>())).Returns(new KernelSpec());

            ChannelMock.SetupGet(x => x.IsAlive).Returns(true);
            ChannelFactoryMock.Setup(x => x.CreateChannel(It.IsAny<string>())).Returns(ChannelMock.Object);

            KernelManagerMock.SetupGet(x => x.ConnectionInformation).Returns(ConnectionInformationFake);
        }

        [TestMethod]
        public void TrackExecuteRequests()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            client.StartChannels();
            client.Execute("test");
            Assert.IsTrue(client.HasPendingExecute());
            Assert.AreEqual(1, client.GetPendingExecuteCount());

            client.Execute("test");
            Assert.IsTrue(client.HasPendingExecute());
            Assert.AreEqual(2, client.GetPendingExecuteCount());
        }

        [TestMethod]
        public void ClearExecuteLog()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            client.StartChannels();
            client.Execute("test");
            client.ClearExecuteLog();
            Assert.IsFalse(client.HasPendingExecute());
            Assert.AreEqual(0, client.GetPendingExecuteCount());
        }

        [TestMethod]
        public void IsAlive()
        {
            // I don't love that we have to change the kernel manager's status, but the underlying checks
            // for the client rely on that status being set appropriately.

            KernelManagerMock.SetupGet(x => x.IsAlive).Returns(false);
            KernelManagerMock.SetupGet(x => x.HasKernel).Returns(false);
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object, false);
            Assert.IsFalse(client.IsAlive);

            KernelManagerMock.SetupGet(x => x.IsAlive).Returns(true);
            KernelManagerMock.SetupGet(x => x.HasKernel).Returns(true);
            client.StartChannels();
            Assert.IsTrue(client.IsAlive);

            KernelManagerMock.SetupGet(x => x.IsAlive).Returns(false);
            KernelManagerMock.SetupGet(x => x.HasKernel).Returns(false);
            client.StopChannels();
            Assert.IsFalse(client.IsAlive);
        }
    }
}
