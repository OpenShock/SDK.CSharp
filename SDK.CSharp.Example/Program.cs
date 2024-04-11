using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Live;

var hostBuilder = Host.CreateDefaultBuilder();

var host = hostBuilder.Build();

var apiClient = new OpenShockApiClient(new ApiClientOptions()
{
    Token = "vYqcHzz0XeALfo3vzQD4Wh7KjqbJeuvZsPz8jlJrtBlfGTF9qKhxtKSrzvZO1A53"
});

var a = await apiClient.GetOwnShockers();

var apiLiveClient = new OpenShockHubClient(new HubClientOptions()
{
    Token = "vYqcHzz0XeALfo3vzQD4Wh7KjqbJeuvZsPz8jlJrtBlfGTF9qKhxtKSrzvZO1A53"
});

await apiLiveClient.StartAsync();

OpenShockLiveControlClient controlClient = new("de1-gateway.shocklink.net", Guid.Empty, "vYqcHzz0XeALfo3vzQD4Wh7KjqbJeuvZsPz8jlJrtBlfGTF9qKhxtKSrzvZO1A53", host.Services.GetRequiredService<ILogger<OpenShockLiveControlClient>>());
await controlClient.InitializeAsync();

await Task.Delay(-1);