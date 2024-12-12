using BlockChainP2P.P2PNetwork.Api.Manager.BlockChain;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Manager.Peers;
using BlockChainP2P.P2PNetwork.Api.Manager.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Manager.ServiceHelpers;

public static class ServiceExtensions
{
    public static IServiceCollection AddP2PTransientManagers(this IServiceCollection services)
    {
        services.AddTransient<IPeerManager, PeerManager>();
        services.AddTransient<IBlockChainManager, BlockChainManager>();
        services.AddTransient<ITransactionManager, TransactionProcessor>();
        return services;
    }
}
