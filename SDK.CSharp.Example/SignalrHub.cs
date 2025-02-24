using OpenShock.SDK.CSharp.Hub;
using OpenShock.SDK.CSharp.Models;
using Serilog;

namespace SDK.CSharp.Example;

public sealed class SignalrHub : IExample
{
    private readonly ExampleConfig _config;

    public SignalrHub(ExampleConfig config)
    {
        _config = config;
    }
    
    public async Task Start()
    {
        var apiSignalRHubClient = new OpenShockHubClient(new HubClientOptions
        {
            Server = _config.ApiUrl,
            Token = _config.ApiToken,
            ConfigureLogging = builder => builder.AddSerilog(Log.Logger)
        });

        await apiSignalRHubClient.StartAsync();

        
        
        await apiSignalRHubClient.Control(_config.Shockers.Select(x => new OpenShock.SDK.CSharp.Hub.Models.Control
        {
            Id = x,
            Type = ControlType.Shock,
            Intensity = 10,
            Duration = 1000,
            Exclusive = true
        }));


    }
}