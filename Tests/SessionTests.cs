using System;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class SessionTests
    {
        [TestMethod]
        public void NewId()
        {
            Session session = new Session();
            string sessionId1 = session.NewId();
            // Format should be 32 characters + 1 hyphen
            Assert.AreEqual(33, sessionId1.Length);
            Assert.AreEqual('-', sessionId1[8]);

            string sessionId2 = session.NewId();
            Assert.AreNotEqual(sessionId1, sessionId2);
        }

        [TestMethod]
        public void NewIdBytes()
        {
            Session session = new Session();
            byte[] sessionId = session.NewIdBytes();
            Assert.AreEqual(33, sessionId.Length);
            Assert.AreEqual('-', (char)sessionId[8]);
        }

        [TestMethod]
        public void Msg()
        {
            Session session = new Session();
            dynamic msg = session.Msg("test");
            Assert.AreEqual("test", msg.msg_type);
            Assert.IsFalse(string.IsNullOrWhiteSpace(msg.msg_id));
        }
    }
}
