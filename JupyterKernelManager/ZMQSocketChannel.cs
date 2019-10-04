using Microsoft.Jupyter.Core;
using NetMQ;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace JupyterKernelManager
{
    /// <summary>
    /// A ZMQ socket invoking a callback in the ioloop
    /// </summary>
    public class ZMQSocketChannel : IChannel
    {
        public const string JUPYTER_KERNEL_DELIMITER = "<IDS|MSG>";

        /// <summary>
        /// The logger for this class
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Internal object to synchronize access to our <see cref="Socket">Socket</see>.
        /// </summary>
        protected object syncObj = new object();

        public string Name { get; set; }
        public NetMQSocket Socket { get; set; }

        protected object sessionSync = new object();
        public Session Session { get; set; }
        public bool IsAlive { get; set; }
        public Encoding Encoding { get; private set; }

        /// <summary>
        /// Create a channel.
        /// </summary>
        /// <param name="name">An identifying name for the channel</param>
        /// <param name="socket">The NetMQ socket to use</param>
        /// <param name="session">The session to connect the socket to</param>
        /// <param name="logger">A specific logger implementation (if any) to use</param>
        public ZMQSocketChannel(string name, NetMQSocket socket, Session session, ILogger logger = null)
        {
            Name = name;
            Socket = socket;
            Session = session.Clone() as Session;
            Encoding = Encoding.UTF8;
            Logger = logger ?? new DefaultLogger();
        }

        /// <summary>
        /// Start up the socket channel
        /// </summary>
        public virtual void Start()
        {
            IsAlive = true;
        }

        /// <summary>
        /// Close the socket and clean it up.  Once called, the underlying socket is no
        /// longer accessible or usable.
        /// </summary>
        public virtual void Stop()
        {
            IsAlive = false;

            lock (syncObj)
            {
                if (Socket == null)
                {
                    return;
                }

                Socket.Close();
                Socket = null;
            }
        }

        /// <summary>
        /// Send a message to the underlying socket channel
        /// </summary>
        /// <param name="message"></param>
        public void Send(Message message)
        {
            lock (syncObj)
            {
                // If the socket has been cleaned up, we should not continue
                if (Socket == null)
                {
                    return;
                }

                var zmqMessage = new NetMQMessage();
                var frames = message.SerializeFrames();
                var digest = Session.Auth.ComputeHash(frames.ToArray());

                if (message.ZmqIdentities != null)
                {
                    message.ZmqIdentities.ForEach(ident => zmqMessage.Append(ident));
                }
                zmqMessage.Append(JUPYTER_KERNEL_DELIMITER);
                zmqMessage.Append(BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant());
                frames.ForEach(ident => zmqMessage.Append(ident));

                Socket.SendMultipartMessage(zmqMessage);
            }
        }

        /// <summary>
        /// Try to receive a response for this channel
        /// </summary>
        /// <returns></returns>
        public Message TryReceive()
        {
            lock (syncObj)
            {
                // If the socket has been cleaned up, we should not continue
                if (Socket == null)
                {
                    return null;
                }

                var rawFrames = new List<byte[]>();
                if (Socket.TryReceiveMultipartBytes(TimeSpan.FromMilliseconds(100), ref rawFrames))
                {
                    return ProcessResults(rawFrames);
                }

                return null;
            }
        }

        /// <summary>
        /// Block until a response is received on this channel
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public Message Receive()
        {
            lock (syncObj)
            {
                // If the socket has been cleaned up, we should not continue
                if (Socket == null)
                {
                    return null;
                }

                // Get all the relevant message frames.
                var rawFrames = Socket.ReceiveMultipartBytes();
                return ProcessResults(rawFrames);
            }
        }

        /// <summary>
        /// Helper method to process a received message on the channel
        /// </summary>
        /// <param name="rawFrames"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        protected Message ProcessResults(List<byte[]> rawFrames)
        {
            var frames = rawFrames
                .Select(frame => Encoding.GetString(frame))
                .ToList();

            // We know that one of the frames should be the special delimiter
            // <IDS|MSG>. If we don't find it, time to throw an exception.
            var idxDelimiter = frames.IndexOf("<IDS|MSG>");
            if (idxDelimiter < 0)
            {
                throw new ProtocolViolationException("Expected <IDS|MSG> delimiter, but none was present.");
            }

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

            byte[] signature = null;
            if (Session.Auth != null)
            {
                // We start by computing the digest, since that is much, much easier
                // to do given the raw frames than trying to unambiguously
                // reserialize everything.
                // To compute the digest and verify the message, we start by pulling
                // out the claimed signature. This is by default a string of
                // hexadecimal characters, so we convert to a byte[] for comparing
                // with the HMAC output.
                signature = frames[idxDelimiter + 1].HexToBytes();
                // Next, we take the four frames after the <IDS|MSG> delimeter, since
                // those are the subject of the digest.
                var toDigest = rawFrames.Skip(idxDelimiter + 2).Take(4).ToArray();
                byte[] digest = null;
                lock (sessionSync)
                {
                    digest = Session.Auth.ComputeHash(toDigest);
                }

                if (!signature.SequenceEqual(digest))
                {
                    var digestStr = Convert.ToBase64String(digest);
                    var signatureStr = Convert.ToBase64String(signature);
                    throw new ProtocolViolationException(
                        string.Format("HMAC {0} did not agree with {1}.", digestStr, signatureStr));
                }
            }

            // If we made it this far, we can unpack the content of the message
            // into the right subclass of MessageContent.
            var header = JsonConvert.DeserializeObject<MessageHeader>(frames[idxDelimiter + 2]);

            var message = new Message
            {
                ZmqIdentities = rawFrames.Take(idxDelimiter).ToList(),
                Signature = signature,
                Header = header,
                ParentHeader = JsonConvert.DeserializeObject<MessageHeader>(frames[idxDelimiter + 3]),
                Metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(frames[idxDelimiter + 4]),
                Content = JsonConvert.DeserializeObject(frames[idxDelimiter + 5])
            };

            Logger.Write("Receive on {0} for {1}", Name, header.MessageType);

            return message;
        }
    }
}
