using System;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class KernelSpecTests
    {
        [TestMethod]
        public void Deserialize_NullEmptyString()
        {
            Assert.ThrowsException<ArgumentNullException>(() => KernelSpec.DeserializeJson(null));
            Assert.ThrowsException<ArgumentNullException>(() => KernelSpec.DeserializeJson(""));
        }

        [TestMethod]
        public void Deserialize_NoData()
        {
            var spec = KernelSpec.DeserializeJson("{ }");
            Assert.IsNull(spec.Arguments);
            Assert.IsNull(spec.DisplayName);
            Assert.IsNull(spec.Language);
            Assert.IsNull(spec.InterruptMode);
            Assert.IsNull(spec.Environment);
            Assert.IsNull(spec.Metadata);
        }

        [TestMethod]
        public void Deserialize_Partial()
        {
            var spec = KernelSpec.DeserializeJson("{ \"argv\": [\"python3\", \"-m\", \"IPython.kernel\", \"-f\", \"{connection_file}\"], \"display_name\": \"Python 3\", \"language\": \"python\" }");
            Assert.AreEqual(5, spec.Arguments.Length);
            Assert.AreEqual("Python 3", spec.DisplayName);
            Assert.AreEqual("python", spec.Language);
            Assert.IsNull(spec.InterruptMode);
            Assert.IsNull(spec.Environment);
            Assert.IsNull(spec.Metadata);
        }

        [TestMethod]
        public void Deserialize_Full()
        {
            var spec = KernelSpec.DeserializeJson("{ \"argv\": [\"python3\", \"-m\", \"IPython.kernel\", \"-f\", \"{connection_file}\"], \"display_name\": \"Python 3\", \"language\": \"python\", \"interrupt_mode\": \"signal\", \"env\": {\"key1\": \"val1\", \"key2\": \"val2\"}, \"metadata\": {\"key3\": \"val3\", \"key4\": \"val4\", \"key5\": \"val5\"} }");
            Assert.AreEqual(5, spec.Arguments.Length);
            Assert.AreEqual("Python 3", spec.DisplayName);
            Assert.AreEqual("python", spec.Language);
            Assert.AreEqual("signal", spec.InterruptMode);
            Assert.AreEqual(2, spec.Environment.Keys.Count);
            Assert.AreEqual(3, spec.Metadata.Keys.Count);
        }
    }
}
