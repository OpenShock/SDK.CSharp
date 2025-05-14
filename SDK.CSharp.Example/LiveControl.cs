using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Live;
using OpenShock.SDK.CSharp.Models;
using System;
using System.Threading.Tasks;

namespace SDK.CSharp.Example;

public sealed class LiveControl
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<LiveControl> _logger;

    public LiveControl(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<LiveControl>();
    }
    
    public async Task ControlExample(string apiToken, Guid hubId, Guid shockerId)
    {
        var apiClient = new OpenShockApiClient(new ApiClientOptions
        {
            Token = apiToken
        });
        
        var gatewayRequest = await apiClient.GetDeviceGateway(hubId);

        if (gatewayRequest.IsT1)
        {
            _logger.LogError("Failed to get gateway, make sure you used a valid device id");
            return;
        }

        if (gatewayRequest.IsT2)
        {
            _logger.LogError("Device is offline");
            return;
        }

        if (gatewayRequest.IsT3)
        {
            _logger.LogError("Device is not connected to a gateway");
            return;
        }

        var gateway = gatewayRequest.AsT0.Value;

        _logger.LogInformation("Device is connected to gateway {GatewayId} in region {Region}", gateway.Gateway, gateway.Country);

        OpenShockLiveControlClient controlClient = new(gateway.Gateway, hubId, apiToken, _loggerFactory.CreateLogger<OpenShockLiveControlClient>());
        await controlClient.InitializeAsync();

        while (true)
        {
            Console.ReadLine();
            controlClient.IntakeFrame(shockerId, ControlType.Vibrate, 100);
            Console.WriteLine("Sent frame");
        }
    }
}