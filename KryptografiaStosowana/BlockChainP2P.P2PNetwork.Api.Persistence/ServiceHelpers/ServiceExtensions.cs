using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Persistence.ServiceHelpers;

public static class ServiceExtensions
{
    public static IServiceCollection AddPersistenceData(this IServiceCollection services)
    {
        services
            .AddSingleton<IPeerData, PeerData>()
            ;
        return services;
    }
}
