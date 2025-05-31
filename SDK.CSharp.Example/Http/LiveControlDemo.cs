using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Live;
using OpenShock.SDK.CSharp.Models;

namespace SDK.CSharp.Example.Http;

public sealed class LiveControlDemo : IExample
{
    private readonly ExampleConfig _config;
    private readonly ILogger<OpenShockLiveControlClient> _liveControlLogger;

    public LiveControlDemo(ExampleConfig config, ILogger<OpenShockLiveControlClient> liveControlLogger)
    {
        _config = config;
        _liveControlLogger = liveControlLogger;
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

        var gateway = await apiClient.GetDeviceGateway(_config.Hub.Value);
        
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
        
        var liveControlClient = new OpenShockLiveControlClient(gateway.AsT0.Value.Gateway, _config.Hub.Value, _config.ApiToken, _liveControlLogger);

        await liveControlClient.InitializeAsync();

        while (true)
        {
            Console.Write("\rHold down enter to continue to send vibrates%   ");
            Console.ReadLine();
            foreach (var configShocker in _config.Shockers)
            {
                liveControlClient.IntakeFrame(configShocker, ControlType.Vibrate, 50);
            }
        }
    }
}