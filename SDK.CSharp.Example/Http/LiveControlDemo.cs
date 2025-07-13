using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Live;
using OpenShock.SDK.CSharp.Models;

namespace SDK.CSharp.Example.Http;

public sealed class LiveControlDemo : IExample
{
    private readonly ExampleConfig _config;
    private readonly ILoggerFactory _loggerFactory;

    public LiveControlDemo(ExampleConfig config, ILoggerFactory loggerFactory)
    {
        _config = config;
        _loggerFactory = loggerFactory;
    }


    public async Task Start()
    {
        var apiClient = new OpenShockApiClient(new ApiClientOptions
        {
            Token = _config.ApiToken
        });
        
        if(!_config.Hub.HasValue) 
        {
            Console.WriteLine("No hub configured, skipping live control demo.");
            return;
        }

        var gateway = await apiClient.GetHubGateway(_config.Hub.Value);
        
       gateway.Switch(success => {},
           found =>
           {
               Console.WriteLine($"Hub with ID {_config.Hub.Value} not found.");
           },
           offline =>
           {
               Console.WriteLine($"Hub with ID {_config.Hub.Value} is not online.");
           },
           unauthenticated =>
           {
               Console.WriteLine($"not authenticated");
           });
       
       if(!gateway.IsT0) throw new Exception("Failed to get gateway for hub " + _config.Hub.Value);
        
        var liveControlClient = new OpenShockLiveControlClient(gateway.AsT0.Value.Gateway, _config.Hub.Value, _config.ApiToken, _loggerFactory);

        await using var stateSub = await liveControlClient.State.Updated.SubscribeAsync(state =>
        {
            Console.WriteLine($"Live control client state updated: {state}");
            return Task.CompletedTask;
        });
        liveControlClient.Start();

        while (true)
        {
            Console.Write("\rHold down enter to continue to send vibrates%   ");
            if (!string.IsNullOrEmpty(Console.ReadLine())) {
                Console.WriteLine("Exiting vibrate loop.");
                break;
            }
        
            foreach (var configShocker in _config.Shockers)
            {
                liveControlClient.IntakeFrame(configShocker, ControlType.Vibrate, 50);
            }
        }

        await liveControlClient.DisposeAsync();
    }
}