using System;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class HashHelperTests
    {
        [TestMethod]
        public void NewId()
        {
            var helper = new HashHelper();
            string sessionId1 = helper.NewId();
            // Format should be 32 characters + 1 hyphen
            Assert.AreEqual(33, sessionId1.Length);
            Assert.AreEqual('-', sessionId1[8]);

            string sessionId2 = helper.NewId();
            Assert.AreNotEqual(sessionId1, sessionId2);
        }

        [TestMethod]
        public void NewId_NoDelim()
        {
            var helper = new HashHelper();
            string sessionId1 = helper.NewId(false);
            // Format should be 32 characters
            Assert.AreEqual(32, sessionId1.Length);
        }

        [TestMethod]
        public void NewIdBytes_NoDelim()
        {
            var helper = new HashHelper();
            byte[] sessionId = helper.NewIdBytes(false);
            Assert.AreEqual(32, sessionId.Length);
        }
    }
}
