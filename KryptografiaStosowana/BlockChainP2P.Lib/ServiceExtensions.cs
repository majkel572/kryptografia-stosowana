using BlockChainP2P.Lib.Interfaces.Managers;
using BlockChainP2P.Lib.Interfaces.Models;
using BlockChainP2P.Lib.Managers;
using BlockChainP2P.Lib.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BlockChainP2P.Lib
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddBlockchainP2PLibrary(this IServiceCollection services)
        {
            services
                .AddSingleton<IWallet, Wallet>()
                .AddSingleton<IPeerManager, PeerManager>()
                ;
            return services;
        }
    }
}
