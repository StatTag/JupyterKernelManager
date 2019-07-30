using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class MessageType
    {
        public const string KernelInfoRequest = "kernel_info_request";
        public const string KernelInfoReply = "kernel_info_reply";
        public const string ExecuteRequest = "execute_request";
        public const string ExecuteReply = "execute_reply";
        public const string DisplayData = "display_data";
        public const string Stream = "stream";
        public const string ShutdownRequest = "shutdown_request";
        public const string ShutdownReply = "shutdown_reply";
    }

    public class ExecuteStatus
    {
        public const string Ok = "ok";
        public const string Error = "error";
        public const string Abort = "abort";
    }

    public class ChannelNames
    {
        public const string Shell = "shell";
        public const string IoPub = "iopub";
        public const string StdIn = "stdin";
        public const string Heartbeat = "hb";
        public const string Control = "control";
    }

    //[JsonConverter(typeof(StringEnumConverter))]
    //public enum ExecutionState
    //{
    //    [EnumMember(Value = "busy")]
    //    Busy,

    //    [EnumMember(Value = "idle")]
    //    Idle,

    //    [EnumMember(Value = "starting")]
    //    Starting
    //}

    public class SignatureScheme
    {
        public const string HmacSha256 = "hmac-sha256";
    }
}
