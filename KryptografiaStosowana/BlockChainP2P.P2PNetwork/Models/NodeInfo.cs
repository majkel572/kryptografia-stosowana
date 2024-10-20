﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Models
{
    public class NodeInfo
    {
        public string Address { get; set; }
        public int Port { get; set; }

        public NodeInfo(string address, int port)
        {
            Address = address;
            Port = port;
        }
    }
}
