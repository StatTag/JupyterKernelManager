using System;
using JupyterKernelManager;
using JupyterKernelManager.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class MessageTests
    {
        [TestMethod]
        public void Serialize_Null()
        {
            var message = new Message();
            Assert.IsNull(message.Raw);
            Assert.IsNull(message.Serialize());
        }

        [TestMethod]
        public void Serialize_NotNull()
        {
            var session = new Session();
            var msg = session.Msg("test");
            var message = new Message(msg);
            Assert.IsNotNull(message.Raw);
            Assert.IsNotNull(message.Serialize());
        }
    }
}
