using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Manager.ServiceHelpers;

public static class ServiceExtensions
{
    public static IServiceCollection AddP2PManagers(this IServiceCollection services)
    {
        services
            .AddTransient<IPeerManager, PeerManager>();
        return services;
    }

    public static IServiceCollection AddBlockChainManagers(this IServiceCollection services)
    {
        services
            .AddTransient<IBlockChainManager, BlockChainManager>();
        return services;
    }
}
