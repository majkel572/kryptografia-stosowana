﻿using BlockChainP2P.P2PNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Interfaces
{
    public interface IMessageHandlerService
    {
        void HandleMessage(Message message);
    }
}