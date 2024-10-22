using BlockChainP2P.P2PNetwork.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Services;

public class BroadcastService : IBroadcastService
{
    private UdpClient _udpClient;
    private const string BroadcastAddress = "255.255.255.255";

    public void StartBroadcast(int port)
    {
        _udpClient = new UdpClient();
        _udpClient.EnableBroadcast = true;

        Task.Run(async () =>
        {
            while (true)
            {
                var message = "NodeInfo"; 
                var data = Encoding.UTF8.GetBytes(message);
                await _udpClient.SendAsync(data, data.Length, BroadcastAddress, port);
                await Task.Delay(1000);
            }
        });
    }

    public void StopBroadcast()
    {
        _udpClient?.Close();
    }
}
