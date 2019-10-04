using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace JupyterKernelManager
{
    /// <summary>
    /// Object for handling serialization and sending of messages.
    /// 
    /// The Session object handles building messages and sending them
    /// with ZMQ sockets or ZMQStream objects.Objects can communicate with each
    /// other over the network via Session objects, and only need to work with the
    /// dict-based IPython message spec.The Session will handle
    /// serialization/deserialization, security, and metadata.
    /// 
    /// Sessions support configurable serialization via packer/unpacker traits,
    /// and signing with HMAC digests via the key/keyfile traits.
    /// </summary>
    public class Session : ICloneable
    {
        /// <summary>
        /// Debug output in the Session
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Whether to check PID to protect against calls after fork.
        /// This check can be disabled if fork-safety is handled elsewhere.
        /// </summary>
        public bool CheckPid { get; set; }

        /// <summary>
        /// The UUID identifying this session.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Username for the Session. Default is your system username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Metadata dictionary, which serves as the default top-level metadata dict for each message.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// execution key, for signing messages.
        /// </summary>
        public byte[] Key { get; private set; }

        /// <summary>
        /// for protecting against sends from forks
        /// </summary>
        private int Pid { get; set; }

        private readonly HashHelper HashHelper = new HashHelper();

        public HMAC Auth { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Session(byte[] key)
        {
            Initialize(false, null, null, key);
        }

        /// <summary>
        /// Constructor to create a session using a key
        /// </summary>
        /// <param name="debug"></param>
        /// <param name="sessionId">the ID of this Session object.  The default is to generate a new UUID.</param>
        /// <param name="username">username added to message headers.  The default is to ask the OS.</param>
        public Session(string sessionId, string username, byte[] key = null, bool debug = false)
        {
            Initialize(debug, sessionId, username, key);
        }

        /// <summary>
        /// Internal initializer, which handles setting default values and ensuring all internal members are initialized.
        /// </summary>
        /// <param name="debug"></param>
        /// <param name="sessionId"></param>
        /// <param name="username"></param>
        /// <param name="key"></param>
        /// <param name="keyFile"></param>
        private void Initialize(bool debug, string sessionId, string username, byte[] key)
        {
            CheckPid = true;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = HashHelper.NewId();
            }
            SessionId = sessionId;

            if (string.IsNullOrWhiteSpace(username))
            {
                // Gets the user name of the person who is running the application.
                username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
            Username = username;
            if (key == null)
            {
                Key = null;
            }
            else
            {
                Key = key.ToArray();
            }

            Pid = Process.GetCurrentProcess().Id;
            Auth = (Key != null && Key.Length > 0) ? new HMACSHA256(Key) : null;
        }

        /// <summary>
        /// always return new uuid
        /// </summary>
        public string MsgId
        {
            get { return HashHelper.NewId(); }
        }

        /// <summary>
        /// Create a new message of a specific type
        /// 
        /// This format is different from what is sent over the wire. The
        /// serialize/deserialize methods converts this nested message dict to the wire
        /// format, which is a list of message parts.
        /// </summary>
        /// <param name="msgType"></param>
        public Message CreateMessage(string msgType)
        {
            var message = new Message(this);
            message.Header = CreateMessageHeader(MsgId, msgType, Username, SessionId);
            message.Metadata = Metadata;
            return message;
        }

        /// <summary>
        /// Create a new message header.
        /// </summary>
        public MessageHeader CreateMessageHeader(string messageId, string messageType, string username, string sessionId)
        {
            var header = new MessageHeader()
            {
                Date = DateTime.UtcNow,
                ProtocolVersion = Version.ProtocolVersion,
                Id = messageId,
                MessageType = messageType,
                Username = username,
                Session = sessionId
            };
            return header;
        }

        /// <summary>
        /// Create a copy of the session object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new Session(this.SessionId, this.Username, this.Key, this.Debug);
        }
    }
}
