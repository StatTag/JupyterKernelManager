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
    }

    public class ExecuteStatus
    {
        public const string Ok = "ok";
        public const string Error = "error";
        public const string Abort = "abort";
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

    //[JsonConverter(typeof(StringEnumConverter))]
    //public enum StreamName
    //{
    //    [EnumMember(Value = "stdin")]
    //    StandardIn,

    //    [EnumMember(Value = "stdout")]
    //    StandardOut,

    //    [EnumMember(Value = "stderr")]
    //    StandardError
    //}

    //public enum Transport
    //{
    //    [EnumMember(Value = "tcp")]
    //    Tcp
    //}

    public class SignatureScheme
    {
        public const string HmacSha256 = "hmac-sha256";
    }

    //public enum SignatureScheme
    //{
    //    [EnumMember(Value = "hmac-sha256")]
    //    HmacSha256
    //}
}
