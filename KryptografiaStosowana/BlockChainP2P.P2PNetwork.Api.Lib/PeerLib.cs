namespace BlockChainP2P.P2PNetwork.Api.Lib;

public class PeerLib
{
    public string IPAddress { get; set; }
    public string Port { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is PeerLib peer)
        {
            return IPAddress == peer.IPAddress && Port == peer.Port;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IPAddress, Port);
    }
}
