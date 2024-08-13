using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Hub;
using OpenShock.SDK.CSharp.Live;
using OpenShock.SDK.CSharp.Models;
using Serilog;

const string apiToken = "";
var deviceId = Guid.Parse("bc849182-89e0-43ff-817b-32400be3f97d");

var hostBuilder = Host.CreateDefaultBuilder();

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

Log.Logger = loggerConfiguration.CreateLogger();

hostBuilder.UseSerilog(Log.Logger);

var host = hostBuilder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

var apiClient = new OpenShockApiClient(new ApiClientOptions
{
    Token = apiToken
});

var shockers = await apiClient.GetOwnShockers();

if (!shockers.IsT0)
{
    logger.LogError("Failed to get own shockers, make sure you used a valid api token");
    return;
}

var apiSignalRHubClient = new OpenShockHubClient(new HubClientOptions
{
    Token = apiToken,
    ConfigureLogging = builder => builder.AddSerilog(Log.Logger)
});

await apiSignalRHubClient.StartAsync();

var gatewayRequest = await apiClient.GetDeviceGateway(deviceId);

if (gatewayRequest.IsT1)
{
    logger.LogError("Failed to get gateway, make sure you used a valid device id");
    return;
}

if (gatewayRequest.IsT2)
{
    logger.LogError("Device is offline");
    return;
}

if (gatewayRequest.IsT3)
{
    logger.LogError("Device is not connected to a gateway");
    return;
}

var gateway = gatewayRequest.AsT0.Value;

logger.LogInformation("Device is connected to gateway {GatewayId} in region {Region}", gateway.Gateway, gateway.Country);

OpenShockLiveControlClient controlClient = new(gateway.Gateway, deviceId, apiToken, host.Services.GetRequiredService<ILogger<OpenShockLiveControlClient>>());
await controlClient.InitializeAsync();

while (true)
{
    Console.ReadLine();
    controlClient.IntakeFrame(Guid.Parse("d9267ca6-d69b-4b7a-b482-c455f75a4408"), ControlType.Vibrate, 100);
    Console.WriteLine("Sent frame");
}