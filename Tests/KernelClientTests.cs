using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Tests
{
    [TestClass]
    public class KernelClientTests
    {
        private static Message GetSuccessMessage()
        {
            return new Message()
            {
                Content = JsonConvert.DeserializeObject("{ status: 'ok' }")
            };
        }

        private static Message GetErrorMessage(string errorMessage = null)
        {
            if (errorMessage == null)
            {
                return new Message()
                {
                    Content = JsonConvert.DeserializeObject("{ status: 'error' }")
                };
            }
            else
            {
                return new Message()
                {
                    Content = JsonConvert.DeserializeObject(string.Format("{{ status: 'error', ename: 'TestError', evalue: '{0}'}}", errorMessage))
                };
            }
        }

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

        [TestMethod]
        public void HasExecuteError_Empty()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();
            Assert.IsFalse(client.HasExecuteError());
        }

        [TestMethod]
        public void HasExecuteError_NoError()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();
            client.ExecuteLog.Add("1", new ExecutionEntry()
            {
                Complete = true,
                ExecutionIndex = 1,
                Request = new Message(null),
                Response = new List<Message>()
                {
                    GetSuccessMessage()
                }
            });
            Assert.IsFalse(client.HasExecuteError());
        }

        [TestMethod]
        public void HasExecuteError_NonStandardContent()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();

            // The check expects an element named "status".  We are going to have something different,
            // and even though it's set to the error of a value, it's not actually a status error so
            // the message should be flagged as okay.
            dynamic content = new System.Dynamic.ExpandoObject();
            content.test = ExecuteStatus.Error;
            client.ExecuteLog.Add("1", new ExecutionEntry()
            {
                Complete = true,
                ExecutionIndex = 1,
                Request = new Message(null),
                Response = new List<Message>()
                {
                    new Message() { Content = content }
                }
            });
            Assert.IsFalse(client.HasExecuteError());
        }

        [TestMethod]
        public void HasExecuteError_OneError()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();
            client.ExecuteLog.Add("1", new ExecutionEntry()
            {
                Complete = true,
                ExecutionIndex = 1,
                Request = new Message(null),
                Response = new List<Message>()
                {
                    GetErrorMessage()
                }
            });
            Assert.IsTrue(client.HasExecuteError());
        }

        [TestMethod]
        public void HasExecuteError_Abort()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();
            dynamic content = new System.Dynamic.ExpandoObject();
            // Abort is considered deprecated as of Jupyter 5.1, but we are still going to ensure we handle it, in
            // case there is an older kernel we need to interact with.
            content.status = ExecuteStatus.Abort;
            client.ExecuteLog.Add("1", new ExecutionEntry()
            {
                Complete = true,
                ExecutionIndex = 1,
                Request = new Message(null),
                Response = new List<Message>()
                {
                    new Message() { Content = content }
                }
            });
            Assert.IsTrue(client.HasExecuteError());
        }

        [TestMethod]
        public void HasExecuteError_MixedStatuses()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();
            client.ExecuteLog.Add("1", new ExecutionEntry()
            {
                Complete = true,
                ExecutionIndex = 1,
                Request = new Message(null),
                Response = new List<Message>()
                {
                    GetSuccessMessage(), GetErrorMessage(), GetErrorMessage(), GetSuccessMessage()
                }
            });
            Assert.IsTrue(client.HasExecuteError());
        }

        [TestMethod]
        public void GetExecuteErrors_None()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();
            client.ExecuteLog.Add("1", new ExecutionEntry()
            {
                Complete = true,
                ExecutionIndex = 1,
                Request = new Message(null),
                Response = new List<Message>()
                {
                    GetErrorMessage(), GetErrorMessage()
                }
            });
            Assert.AreEqual(0, client.GetExecuteErrors().Count);
        }

        [TestMethod]
        public void GetExecuteErrors_Single()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();
            client.ExecuteLog.Add("1", new ExecutionEntry()
            {
                Complete = true,
                ExecutionIndex = 1,
                Request = new Message(null),
                Response = new List<Message>()
                {
                    GetErrorMessage("Error message 1")
                }
            });
            Assert.AreEqual(1, client.GetExecuteErrors().Count);
            Assert.AreEqual("TestError: Error message 1", client.GetExecuteErrors().First());
        }

        [TestMethod]
        public void GetExecuteErrors_MultipleWithEmpty()
        {
            var client = new KernelClient(KernelManagerMock.Object, ChannelFactoryMock.Object);
            Assert.IsFalse(client.HasExecuteError());
            client.StartChannels();
            client.ExecuteLog.Add("1", new ExecutionEntry()
            {
                Complete = true,
                ExecutionIndex = 1,
                Request = new Message(null),
                Response = new List<Message>()
                {
                    GetErrorMessage("Error message 1"), GetErrorMessage(), GetErrorMessage("Error message 2")
                }
            });
            Assert.AreEqual(2, client.GetExecuteErrors().Count);
            Assert.AreEqual("TestError: Error message 1", client.GetExecuteErrors().First());
            Assert.AreEqual("TestError: Error message 2", client.GetExecuteErrors().Last());
        }
    }
}
