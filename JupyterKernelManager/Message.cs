using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class Message
    {
        public dynamic Raw { get; set; }

        public Message()
        {
            Raw = null;
        }

        public Message(dynamic message)
        {
            Raw = message;
        }

        public Message(Message message)
        {
            Raw = message.Raw;
        }

        public byte[] Serialize()
        {
            if (Raw == null)
            {
                return null;
            }

            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Raw));
        }
    }
}
