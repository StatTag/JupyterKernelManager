// Leverages code from Microsoft.Jupyter.Core
// That code is Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MessageHeader
    {
        public MessageHeader()
        {
            ProtocolVersion = "5.2.0";
            Id = Guid.NewGuid().ToString();
        }

        [JsonProperty("msg_id")]
        public string Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("session")]
        public string Session { get; set; }

        // NB: Not an enum here, since we MUST handle unknown message types
        //     gracefully as per the wire protocol.
        [JsonProperty("msg_type")]
        public string MessageType { get; set; }

        [JsonProperty("version")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }

    public class Message
    {
        /// <summary>
        /// Our representation of an empty frame that is sent within a message
        /// </summary>
        protected const string EMPTY_FRAME = "{}";

        private Session Session { get; set; }

        public List<byte[]> ZmqIdentities { get; set; }

        public byte[] Signature { get; set; }

        public MessageHeader Header { get; set; }

        // As per Jupyter's wire protocol, if messages occur in sequence,
        // each message will have the previous message's header in this field.
        public MessageHeader ParentHeader { get; set; }

        // FIXME: make not just an object.
        public object Metadata { get; set; }

        // Dynamic data type
        public dynamic Content { get; set; }

        public Message(Session session)
        {
            this.Session = session;
        }

        public Message(Session session, dynamic content)
        {
            this.Session = session;
            Content = content;
        }

        public HMAC NewAuth()
        {
            return new HMACSHA256(Session.Key);
        }

        public List<byte[]> SerializeFrames()
        {
            var frames = new List<byte[]>();
            frames.Add(Encoding.UTF8.GetBytes(
                (Header == null) ? EMPTY_FRAME : JsonConvert.SerializeObject(Header)));
            frames.Add(Encoding.UTF8.GetBytes(
                (ParentHeader == null) ? EMPTY_FRAME : JsonConvert.SerializeObject(ParentHeader)));
            frames.Add(Encoding.UTF8.GetBytes(
                (Metadata == null) ? EMPTY_FRAME : JsonConvert.SerializeObject(Metadata)));
            frames.Add(Encoding.UTF8.GetBytes(
                (Content == null) ? EMPTY_FRAME : JsonConvert.SerializeObject(Content)));

            return frames;
        }

        //public Message(Message message)
        //{
        //    Data = message.Raw;
        //}

        //public byte[] Serialize()
        //{
        //    if (Raw == null)
        //    {
        //        return null;
        //    }

        //    return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Raw));
        //}

        internal Message AsReplyTo(Message parent)
        {
            // No parent, just return
            if (parent == null) return this;

            var reply = this.MemberwiseClone() as Message;
            reply.ZmqIdentities = parent.ZmqIdentities;
            reply.ParentHeader = parent.Header;
            reply.Header.Session = parent.Header.Session;
            return reply;
        }
    }
}
