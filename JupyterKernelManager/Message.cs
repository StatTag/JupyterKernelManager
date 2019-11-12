// Leverages code from Microsoft.Jupyter.Core
// That code is Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Dynamic;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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

        public Message()
        {

        }

        /// <summary>
        /// Does this message contain an error response
        /// </summary>
        /// <returns>true if the message reflects an error response</returns>
        public bool HasError()
        {
            // Per comments at https://github.com/StatTag/JupyterKernelManager/issues/1, we will consider missing
            // status values to be "ok".
            if (Content == null || !DoesPropertyExist(Content, "status"))
            {
                return false;
            }

            // Otherwise, if it's set, it needs to be "ok" or blank, or we'll consider it an error.
            string status = Content.status.ToString().Trim();
            return !(status.Equals(string.Empty) || status.Equals(ExecuteStatus.Ok));
        }

        /// <summary>
        /// Return the error response message, if one exists
        /// </summary>
        /// <returns>A string containing the error, or an empty string if no error (or no error message) exists.</returns>
        public string GetError()
        {
            if (Content == null || !DoesPropertyExist(Content, "ename") || !DoesPropertyExist(Content, "evalue"))
            {
                return string.Empty;
            }

            return string.Format("{0}: {1}",
                Content.ename, Content.evalue);
        }

        /// <summary>
        /// Determine if the MessageType in the header indicates that this message should
        /// have some returned data associated with it.
        /// </summary>
        /// <returns></returns>
        public bool IsDataMessageType()
        {
            if (Header == null || string.IsNullOrWhiteSpace(Header.MessageType))
            {
                return false;
            }

            return Header.MessageType.Equals(MessageType.DisplayData) ||
                   Header.MessageType.Equals(MessageType.Stream) ||
                   Header.MessageType.Equals(MessageType.ExecuteResult);
        }

        /// <summary>
        /// Returns the data for a particular MIME type.  If there is no data for that MIME type,
        /// the method will return null.  This will protect against exceptions being thrown.
        /// </summary>
        /// <param name="mimeType">The MIME type to search for data under</param>
        /// <returns>null if no data is found, otherwise returns the data as a string</returns>
        public string SafeGetData(string mimeType)
        {
            try
            {
                if (Content == null || Content.data == null || !DoesPropertyExist(Content.data, mimeType))
                {
                    return null;
                }

                return Content.data[mimeType];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Determine if a property exists in a dynamic object
        /// </summary>
        /// <param name="obj">The dynamic object to check</param>
        /// <param name="name">The name of the property to look for</param>
        /// <returns>true if the property exists, false otherwise</returns>
        public bool DoesPropertyExist(dynamic obj, string name)
        {
            if (obj is ExpandoObject)
            {
                return ((IDictionary<string, object>) obj).ContainsKey(name);
            }
            else if (obj is JObject)
            {
                return ((IDictionary<string, JToken>) obj).ContainsKey(name);
            }

            return obj.GetType().GetProperty(name) != null;
        }

        public List<byte[]> SerializeFrames()
        {
            var frames = new List<byte[]>();
            frames.Add(Encoding.UTF8.GetBytes(Header == null ? EMPTY_FRAME :
                JsonConvert.SerializeObject(Header)));
            frames.Add(Encoding.UTF8.GetBytes(ParentHeader == null ? EMPTY_FRAME :
                JsonConvert.SerializeObject(ParentHeader)));
            frames.Add(Encoding.UTF8.GetBytes(Metadata == null ? EMPTY_FRAME :
                JsonConvert.SerializeObject(Metadata)));
            frames.Add(Encoding.UTF8.GetBytes(Content == null ? EMPTY_FRAME :
                JsonConvert.SerializeObject(Content)));

            return frames.ToList();
        }
    }
}
