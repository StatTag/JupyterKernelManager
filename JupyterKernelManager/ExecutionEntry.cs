﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class ExecutionEntry
    {
        public bool Complete { get; set; }
        public bool Abandoned { get; set; }

        public bool Error { get; set; }
        public int ExecutionIndex { get; set; }
        public Message Request { get; set; }
        public List<Message> Response { get; set; }

        public ExecutionEntry()
        {
            ExecutionIndex = -1;
            Response = new List<Message>();
            Complete = false;
            Error = false;
            Abandoned = false;
        }
    }
}
