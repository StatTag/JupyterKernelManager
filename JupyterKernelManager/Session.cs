using JupyterKernelManager.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    public class Session
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
        public byte[] Key { get; set; }

        /// <summary>
        /// The digest scheme used to construct the message signatures.  Currently only hmac-sha256 is supported
        /// </summary>
        public string SignatureScheme { get { return "hmac-sha256"; } }

        /// <summary>
        /// The signature provider
        /// </summary>
        private HMAC Auth { get; set; }

        /// <summary>
        /// for protecting against sends from forks
        /// </summary>
        private int Pid { get; set; }

        /// <summary>
        /// Random number generator, so we aren't recreating one each time it's needed.
        /// </summary>
        private RNGCryptoServiceProvider RandomGenerator = new RNGCryptoServiceProvider();

        /// <summary>
        /// Default constructor
        /// </summary>
        public Session()
        {
            Initialize(false, null, null, null, null);
        }

        /// <summary>
        /// Constructor to create a session using a key
        /// </summary>
        /// <param name="debug"></param>
        /// <param name="sessionId">the ID of this Session object.  The default is to generate a new UUID.</param>
        /// <param name="username">username added to message headers.  The default is to ask the OS.</param>
        /// <param name="key">The key used to initialize an HMAC signature.  If unset, messages will not be signed or checked.</param>
        public Session(string sessionId, string username, byte[] key, bool debug = false)
        {
            Initialize(debug, sessionId, username, key, null);
        }

        /// <summary>
        /// Constructor to create a session using a key file
        /// </summary>
        /// <param name="debug"></param>
        /// <param name="sessionId">the ID of this Session object.  The default is to generate a new UUID.</param>
        /// <param name="username">username added to message headers.  The default is to ask the OS.</param>
        /// <param name="keyFile">The file containing a key.  If this is set, `key` will be initialized to the contents of the file.</param>
        public Session(string sessionId, string username, string keyFile, bool debug = false)
        {
            Initialize(debug, sessionId, username, null, keyFile);
        }

        /// <summary>
        /// Internal initializer, which handles setting default values and ensuring all internal members are initialized.
        /// </summary>
        /// <param name="debug"></param>
        /// <param name="sessionId"></param>
        /// <param name="username"></param>
        /// <param name="key"></param>
        /// <param name="keyFile"></param>
        private void Initialize(bool debug, string sessionId, string username, byte[] key, string keyFile)
        {
            // TODO: Implement defaults (see https://github.com/jupyter/jupyter_client/blob/4da42519adba668282dceb0eb9ddcc9dafb40d12/jupyter_client/session.py)
            CheckPid = true;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = NewId();
            }
            SessionId = sessionId;

            if (string.IsNullOrWhiteSpace(username))
            {
                // Gets the user name of the person who is running the application.
                username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
            Username = username;

            SetKeyAndAuth(key, keyFile);
            Pid = Process.GetCurrentProcess().Id;
            NewAuth();
        }

        /// <summary>
        /// Utility function to determine if we have a key set, or are using a key file, or if we need to generate a key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyFile"></param>
        private void SetKeyAndAuth(byte[] key, string keyFile)
        {
            if ((key == null || key.Length == 0) && string.IsNullOrWhiteSpace(keyFile))
            {
                Key = NewIdBytes();
            }
            else if (key != null && key.Length > 0)
            {
                Key = key;
            }
            else
            {
                Key = File.ReadAllBytes(keyFile);
            }

            NewAuth();
        }

        /// <summary>
        /// Generate a new random id.
        /// </summary>
        /// <returns>id string (16 random bytes as hex-encoded text, chunks separated by '-')</returns>
        public string NewId()
        {
            const int ID_BYTE_SIZE = 16;
            var rand = new byte[ID_BYTE_SIZE];
            RandomGenerator.GetBytes(rand);
            // Convert the bytes to a 2 character hex representation.
            var randString = BitConverter.ToString(rand).Replace("-", string.Empty);
            // This mimics the format Jupyter uses, instead of the built-in UUID generator
            return string.Format("{0}-{1}", randString.Substring(0, 8), randString.Substring(8));
        }

        /// <summary>
        /// Return a new ID as ascii bytes
        /// </summary>
        /// <returns></returns>
        public byte[] NewIdBytes()
        {
            return Encoding.ASCII.GetBytes(NewId());
        }

        /// <summary>
        /// Generate a new HMAC instance for Auth, if we have a key defined
        /// </summary>
        /// <returns></returns>
        private void NewAuth()
        {
            Auth = (Key != null && Key.Length > 0) ? new HMACSHA256(Key) : null;
        }

        /// <summary>
        /// always return new uuid
        /// </summary>
        public string MsgId
        {
            get { return NewId(); }
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
    }
}
