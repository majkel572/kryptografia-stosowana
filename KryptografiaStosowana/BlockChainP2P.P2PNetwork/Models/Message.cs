using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Models
{
    public class Message
    {
        public string Command { get; set; }
        public string Payload { get; set; }

        public Message(string command, string payload)
        {
            Command = command;
            Payload = payload;
        }
    }
}
