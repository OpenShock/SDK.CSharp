using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Hub;
using OpenShock.SDK.CSharp.Models;
using Serilog;

namespace SDK.CSharp.Example.Http;

public sealed class HubDemo : IExample
{
    private readonly ExampleConfig _config;

    public HubDemo(ExampleConfig config)
    {
        _config = config;
    }


    public async Task Start()
    {
        var apiClient = new OpenShockApiClient(new ApiClientOptions
        {
            Token = _config.ApiToken
        });

        var openshockSignalrHub = new OpenShockHubClient();

        await openshockSignalrHub.Setup(new HubClientOptions
        {
            Token = _config.ApiToken,
            Server = _config.ApiUrl,
            ConfigureLogging = builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            }
        });
        
        await using var onWelcomeSub = await openshockSignalrHub.OnWelcome.SubscribeAsync(async connectionId =>
        {
            Console.WriteLine($"OnWelcome to OpenShock Hub with connection ID: {connectionId}");
            await Task.Delay(10000);
        });
        
        await using var onConnectedSub = await openshockSignalrHub.OnConnected.SubscribeAsync(hubId =>
        {
            Console.WriteLine($"OnConnected to OpenShock Hub with ID: {hubId}");
            return Task.CompletedTask;
        });
        
        await using var onReconnected = await openshockSignalrHub.OnReconnected.SubscribeAsync(hubId =>
        {
            Console.WriteLine($"OnReconnected from OpenShock Hub with ID: {hubId}");
            return Task.CompletedTask;
        });
        
        await using var onReconnectingSub = await openshockSignalrHub.OnReconnecting.SubscribeAsync(exception =>
        {
            Console.WriteLine($"OnReconnecting to OpenShock Hub due to: {exception?.Message}");
            return Task.CompletedTask;
        });
        
        
        await using var onLogSub = await openshockSignalrHub.OnLog.SubscribeAsync(logEvent =>
        {
            Console.WriteLine($"OnLog: {logEvent}");
            return Task.CompletedTask;
        });
        
        await using var onHubUpdateSub = await openshockSignalrHub.OnHubUpdate.SubscribeAsync(hubUpdateEventArgs =>
        {
            Console.WriteLine($"OnHubUpdate: {hubUpdateEventArgs}");
            return Task.CompletedTask;
        });
        
        await using var onHubStatusSub = await openshockSignalrHub.OnHubStatus.SubscribeAsync(hubOnlineStates =>
        {
            Console.WriteLine($"OnHubStatus: {string.Join(", ", hubOnlineStates)}");
            return Task.CompletedTask;
        });
        
        await openshockSignalrHub.StartAsync();

        Console.WriteLine("OpenShock Hub client started. Press Enter to send a control command... type any character to exit.");
        while (true)
        {
            if(!string.IsNullOrWhiteSpace(Console.ReadLine()))
            {
                Console.WriteLine("Exiting OpenShock Hub demo.");
                break;
            }    
            
            await openshockSignalrHub.Control(_config.Shockers.Select(x => new Control
            {
                Id = x,
                Type = ControlType.Vibrate,
                Intensity = 50,
                Duration = 1000
            }));
        }
    }
}