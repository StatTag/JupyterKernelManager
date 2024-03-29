﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public const string ExecuteResult = "execute_result";
        public const string ExecuteReply = "execute_reply";
        public const string DisplayData = "display_data";
        public const string Stream = "stream";
        public const string ShutdownRequest = "shutdown_request";
        public const string ShutdownReply = "shutdown_reply";
        public const string Error = "error";
    }

    public class ExecuteStatus
    {
        public const string Ok = "ok";
        public const string Error = "error";
        // The specification documents have both said 'abort' and 'aborted'.  To be safe
        // we are including both and will check both.
        public const string Abort = "abort";
        public const string Aborted = "aborted";
    }

    public class ChannelNames
    {
        public const string Shell = "shell";
        public const string IoPub = "iopub";
        public const string StdIn = "stdin";
        public const string Heartbeat = "hb";
        public const string Control = "control";
    }

    public class LogLevel
    {
        public const int Debug = 1;
        public const int Info = 2;
        public const int Warn = 3;
        public const int Error = 4;

        public const int Default = Info;
    }

    public class StreamName
    {
        public const string StdOut = "stdout";
        public const string StdErr = "stderr";
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
