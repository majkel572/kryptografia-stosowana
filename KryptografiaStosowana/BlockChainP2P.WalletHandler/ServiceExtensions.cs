using BlockChainP2P.WalletHandler.WalletManagement;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.WalletHandler;

public static class ServiceExtensions
{
    public static IServiceCollection AddWallet(this IServiceCollection services)
    {
        services.AddSingleton<IWallet, Wallet>();
        return services;
    }
}
