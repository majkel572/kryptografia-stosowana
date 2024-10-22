using BlockChainP2P.P2PNetwork.Interfaces;
using BlockChainP2P.P2PNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Services;

internal class MessageHandlerService : IMessageHandlerService
{
    public void HandleMessage(Message message)
    {
        // Implement message handling logic here
        // For example, log the message or process commands
        Console.WriteLine($"Received message: {message.Command} - {message.Payload}");
    }
}
