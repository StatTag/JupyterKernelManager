// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Ideally, end users should not need to directly know about the classes we
// use to serialize and de-serialize messages, so we guard those in a new
// namespace.
namespace JupyterKernelManager.Protocol
{
    //[JsonObject(MemberSerialization.OptIn)]
    //public class MessageHeader
    //{
    //    public MessageHeader()
    //    {
    //        ProtocolVersion = "5.2.0";
    //        Id = Guid.NewGuid().ToString();
    //    }

    //    [JsonProperty("msg_id")]
    //    public string Id { get; set; }

    //    [JsonProperty("username")]
    //    public string Username { get; set; }

    //    [JsonProperty("session")]
    //    public string Session { get; set; }

    //    // NB: Not an enum here, since we MUST handle unknown message types
    //    //     gracefully as per the wire protocol.
    //    [JsonProperty("msg_type")]
    //    public string MessageType { get; set; }

    //    [JsonProperty("version")]
    //    public string ProtocolVersion { get; set; }

    //    // FIXME: Need to add ISO 8601 format date here.
    //}

    //public class Message
    //{
    //    public List<byte[]> ZmqIdentities { get; set; }

    //    public byte[] Signature { get; set; }

    //    public MessageHeader Header { get; set; }

    //    // As per Jupyter's wire protocol, if messages occur in sequence,
    //    // each message will have the previous message's header in this field.
    //    public MessageHeader ParentHeader { get; set; }

    //    // FIXME: make not just an object.
    //    public object Metadata { get; set; }

    //    // FIXME: make not just an object.
    //    public MessageContent Content { get; set; }

    //    internal Message AsReplyTo(Message parent)
    //    {
    //        // No parent, just return
    //        if (parent == null) return this;

    //        var reply = this.MemberwiseClone() as Message;
    //        reply.ZmqIdentities = parent.ZmqIdentities;
    //        reply.ParentHeader = parent.Header;
    //        reply.Header.Session = parent.Header.Session;
    //        return reply;
    //    }

    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class MessageContent
    //{
    //    public readonly static Dictionary<string, Func<string, MessageContent>> Deserializers =
    //        new Dictionary<string, Func<string, MessageContent>>
    //        {
    //            ["kernel_info_request"] = data => new EmptyContent(),
    //            ["execute_request"] = data => JsonConvert.DeserializeObject<ExecuteRequestContent>(data),
    //            ["shutdown_request"] = data => JsonConvert.DeserializeObject<ShutdownRequestContent>(data)
    //        };
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class EmptyContent : MessageContent
    //{
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class UnknownContent : MessageContent
    //{
    //    public Dictionary<string, object> Data { get; set; }
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class ShutdownRequestContent : MessageContent
    //{
    //    [JsonProperty("restart")]
    //    public bool Restart { get; set; }
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class ExecuteRequestContent : MessageContent
    //{
    //    [JsonProperty("code")]
    //    public string Code { get; set; }

    //    [JsonProperty("silent")]
    //    public bool Silent { get; set; }

    //    [JsonProperty("store_history")]
    //    public bool StoreHistory { get; set; }

    //    [JsonProperty("user_expressions")]
    //    public object UserExpressions { get; set; }

    //    [JsonProperty("allow_stdin")]
    //    public bool AllowStandardIn { get; set; }

    //    [JsonProperty("stop_on_error")]
    //    public bool StopOnError { get; set; }
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class KernelStatusContent : MessageContent
    //{
    //    [JsonProperty("execution_state")]
    //    public string ExecutionState { get; set; }
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class StreamContent : MessageContent
    //{
    //    [JsonProperty("name")]
    //    public string StreamName { get; set; }

    //    [JsonProperty("text")]
    //    public string Text { get; set; }
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class TransientDisplayData
    //{
    //    [JsonProperty("display_id")]
    //    public string DisplayId { get; set; }
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class DisplayDataContent : MessageContent
    //{
    //    [JsonProperty("data")]
    //    public Dictionary<string, string> Data { get; set; }

    //    [JsonProperty("metadata")]
    //    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    //    [JsonProperty("transient")]
    //    public TransientDisplayData Transient { get; set; } = null;
    //}
}
