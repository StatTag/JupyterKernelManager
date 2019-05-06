using System;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class KernelSpecManagerTests
    {
        [TestMethod]
        public void IsValidKernelName_NullEmpty()
        {
            var manager = new KernelSpecManager();
            Assert.IsFalse(manager.IsValidKernelName(null));
            Assert.IsFalse(manager.IsValidKernelName(""));
            Assert.IsFalse(manager.IsValidKernelName("    "));
        }

        [TestMethod]
        public void IsValidKernelName_Invalid()
        {
            var manager = new KernelSpecManager();
            Assert.IsFalse(manager.IsValidKernelName("Name With Spaces"));
            Assert.IsFalse(manager.IsValidKernelName("slashes\\"));
            Assert.IsFalse(manager.IsValidKernelName("mix*ed&char's"));
        }

        [TestMethod]
        public void IsValidKernelName_Valid()
        {
            var manager = new KernelSpecManager();
            Assert.IsTrue(manager.IsValidKernelName("a"));
            Assert.IsTrue(manager.IsValidKernelName("a_0_-.Z"));
        }
    }
}
