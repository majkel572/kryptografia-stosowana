﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Interfaces;

public interface IBroadcastService
{
    void StartBroadcast(int port);
    void StopBroadcast();
}