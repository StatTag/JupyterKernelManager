using System;
using System.Dynamic;
using System.Text;
using JupyterKernelManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class MessageTests
    {
        static readonly Session MessageSession = new Session(new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10
        });
        const int EXPECTED_FRAME_COUNT = 4;

        [TestMethod]
        public void SerializeFrames_Empty()
        {
            var message = new Message(MessageSession);
            Assert.IsNull(message.Header);
            Assert.IsNull(message.ParentHeader);
            Assert.IsNull(message.Metadata);
            Assert.IsNull(message.Content);

            // Even though all of our data structures are null, the serialized data should come back
            // as 2-byte values ("{}")
            var frames = message.SerializeFrames();
            Assert.AreEqual(EXPECTED_FRAME_COUNT, frames.Count);
            foreach (var frame in frames)
            {
                Assert.AreEqual("{}", Encoding.UTF8.GetString(frame));
            }
        }

        [TestMethod]
        public void SerializeFrames_WithData()
        {
            // This is convoluted because we don't normally initialize our messages this way.
            // However, we want full control over what's going into them.
            dynamic content = new ExpandoObject();
            content.execution_state = "idle";
            dynamic metadata = new ExpandoObject();
            metadata.version = "1.0.0";
            var message = new Message(MessageSession, content);
            message.Header = new MessageHeader();
            message.ParentHeader = new MessageHeader();
            message.Metadata = metadata;

            var frames = message.SerializeFrames();
            Assert.AreEqual(EXPECTED_FRAME_COUNT, frames.Count);
            foreach (var frame in frames)
            {
                Assert.AreNotEqual("{}", Encoding.UTF8.GetString(frame));
            }
        }
    }
}
