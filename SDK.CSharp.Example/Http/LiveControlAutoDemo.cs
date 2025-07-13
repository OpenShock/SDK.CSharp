using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Live;
using OpenShock.SDK.CSharp.Models;

namespace SDK.CSharp.Example.Http;

public sealed class LiveControlAutoDemo : IExample
{
    private readonly ExampleConfig _config;
    private readonly ILoggerFactory _loggerFactory;

    public LiveControlAutoDemo(ExampleConfig config, ILoggerFactory loggerFactory)
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
        
        var liveControlClient = new OpenShockLiveControlClient(_config.Hub.Value, _config.ApiToken, apiClient, _loggerFactory);

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