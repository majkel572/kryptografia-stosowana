using BlockChainP2P.P2PNetwork.Api.Lib;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Manager.ServiceHelpers;
using BlockChainP2P.P2PNetwork.Api.Middlewares;
using BlockChainP2P.P2PNetwork.Api.Persistence.ServiceHelpers;
using BlockChainP2P.P2PNetwork.Api.Hubs;
using BlockChainP2P.WalletHandler;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
Console.WriteLine("Ipaddress: " + builder.Configuration.GetSection("NodeIpAddress").Value!);
Console.WriteLine("port: " + builder.Configuration.GetSection("NodePort").Value!);


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .CreateLogger();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddPersistenceData();
builder.Services.AddP2PTransientManagers();
builder.Services.AddWallet();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHsts();

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.MapHub<BlockchainHub>("/blockchainHub");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var blockChainManager = services.GetRequiredService<IBlockChainManager>();
    
    await blockChainManager.CreateGenesisBlockAsync();
}

if (args[0]!="init")
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        try
        {
            var peerManager = services.GetRequiredService<IPeerManager>();

            var peerInNetwork = new PeerLib
            {
                IPAddress = args[0],
                Port = "8080",
            };

            var result = await peerManager.ConnectWithPeerNetworkAsync(peerInNetwork);
            if(result == true)
            {
                var resText = $"Successfully registered new peer with IP address: {peerInNetwork.IPAddress} and port number: {peerInNetwork.Port}";
                Log.Information(resText);
            }
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred while connecting to the peer network: {Error}", ex.Message);
        }
    }
}

app.Run();