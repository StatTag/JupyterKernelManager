using JupyterKernelManager.Protocol;
using NetMQ;
using Newtonsoft.Json;
using System;
using Microsoft.Jupyter.Core;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    /// <summary>
    /// A ZMQ socket invoking a callback in the ioloop
    /// </summary>
    public class ZMQSocketChannel
    {
        public const string JUPYTER_KERNEL_DELIMITER = "<IDS|MSG>";

        public NetMQSocket Socket { get; set; }
        public bool IsAlive { get; set; }

        /// <summary>
        /// Create a channel.
        /// </summary>
        /// <param name="socket">The NetMQ socket to use</param>
        /// <param name="ioloop">The zmq IO loop to connect the socket to using a ZMQStream</param>
        public ZMQSocketChannel(NetMQSocket socket)
        {
            Socket = socket;
        }

        public void Start()
        {
            IsAlive = true;
        }

        public void Stop()
        {
            IsAlive = false;
        }

        public void Close()
        {
            if (Socket == null)
            {
                return;
            }

            Socket.Close();
            Socket = null;
        }

        public void Send(Message message)
        {
            var zmqMessage = new NetMQMessage();
            var frames = message.SerializeFrames();
            var digest = message.NewAuth().ComputeHash(frames.ToArray());

            message.ZmqIdentities?.ForEach(ident => zmqMessage.Append(ident));
            zmqMessage.Append(JUPYTER_KERNEL_DELIMITER);
            zmqMessage.Append(BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant());
            frames.ForEach(ident => zmqMessage.Append(ident));

            Socket.SendMultipartMessage(zmqMessage);
        }

        public Message Receive(Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            // Get all the relevant message frames.
            var rawFrames = Socket.ReceiveMultipartBytes();
            var frames = rawFrames
                .Select(frame => encoding.GetString(frame))
                .ToList();

            // We know that one of the frames should be the special delimiter
            // <IDS|MSG>. If we don't find it, time to throw an exception.
            var idxDelimiter = frames.IndexOf("<IDS|MSG>");
            if (idxDelimiter < 0)
            {
                throw new ProtocolViolationException("Expected <IDS|MSG> delimiter, but none was present.");
            }

            var message = new Message(frames);

            // At this point, we know that everything before idxDelimter is
            // a ZMQ identity, and that everything after follows the Jupyter
            // wire protocol. In particular, the next five blobs after <IDS|MSG>
            // are as follows:
            //     • An HMAC signature for the entire message.
            //     • A serialized header for this message.
            //     • A serialized header for the previous message in sequence.
            //     • A serialized metadata dictionary.
            //     • A serialized content dictionary.
            // Any remaining blobs are extra raw data buffers.

            // We start by computing the digest, since that is much, much easier
            // to do given the raw frames than trying to unambiguously
            // reserialize everything.
            // To compute the digest and verify the message, we start by pulling
            // out the claimed signature. This is by default a string of
            // hexadecimal characters, so we convert to a byte[] for comparing
            // with the HMAC output.
            var signature = frames[idxDelimiter + 1].HexToBytes();
            // Next, we take the four frames after the <IDS|MSG> delimeter, since
            // those are the subject of the digest.
            var toDigest = rawFrames.Skip(idxDelimiter + 2).Take(4).ToArray();
            //var digest = context.NewHmac().ComputeHash(toDigest);

            //if (!signature.IsEqual(digest))
            //{
            //    var digestStr = Convert.ToBase64String(digest);
            //    var signatureStr = Convert.ToBase64String(signature);
            //    throw new ProtocolViolationException(
            //        $"HMAC {digestStr} did not agree with {signatureStr}.");
            //}

            //// If we made it this far, we can unpack the content of the message
            //// into the right subclass of MessageContent.
            //var header = JsonConvert.DeserializeObject<MessageHeader>(frames[idxDelimiter + 2]);
            //var content = MessageContent.Deserializers.GetValueOrDefault(
            //    header.MessageType,
            //    data =>
            //        new UnknownContent
            //        {
            //            Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(data)
            //        }
            //)(frames[idxDelimiter + 5]);

            //var message = new Message
            //{
            //    ZmqIdentities = rawFrames.Take(idxDelimiter).ToList(),
            //    Signature = signature,
            //    Header = header,
            //    ParentHeader = JsonConvert.DeserializeObject<MessageHeader>(frames[idxDelimiter + 3]),
            //    Metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(frames[idxDelimiter + 4]),
            //    Content = content
            //};

            //return message;
            return null;
        }
    }
}
