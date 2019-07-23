using System;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class SessionTests
    {
        [TestMethod]
        public void CreateMessage()
        {
            Session session = new Session(new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10
            });
            var message = session.CreateMessage("test");
            Assert.AreEqual("test", message.Header.MessageType);
            Assert.IsFalse(string.IsNullOrWhiteSpace(message.Header.Id));
        }
    }
}
