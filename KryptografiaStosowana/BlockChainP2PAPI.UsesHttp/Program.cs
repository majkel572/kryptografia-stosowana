using BlockChainP2P.Lib;
using BlockChainP2P.Lib.Interfaces.Models;
using BlockChainP2P.Lib.Managers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
            //.AllowCredentials();
        });
});
builder.Services.AddBlockchainP2PLibrary();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHsts();
app.UseHttpsRedirection();

app.UseCors("AllowAllOrigins");

app.MapControllers();

app.Run();
