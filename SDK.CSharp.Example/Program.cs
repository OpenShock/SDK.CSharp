using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Hub;
using OpenShock.SDK.CSharp.Live;

var hostBuilder = Host.CreateDefaultBuilder();

var host = hostBuilder.Build();

// var apiClient = new OpenShockApiClient(new ApiClientOptions()
// {
//     Token = "vYqcHzz0XeALfo3vzQD4Wh7KjqbJeuvZsPz8jlJrtBlfGTF9qKhxtKSrzvZO1A53"
// });
//
// var a = await apiClient.GetOwnShockers();
//
// var b = await apiClient.GetDeviceGateway(Guid.Parse("bc849182-89e0-43ff-817b-32400be3f97d"));

var apiLiveClient = new OpenShockHubClient(new HubClientOptions()
{
    Token = "71WZxCwCAIBJNgNG2pgdaHxHdaipUKmA6MalZUXNZhv3IkV7GB1ObxA35ud4tkPz"
});

await apiLiveClient.StartAsync();

OpenShockLiveControlClient controlClient = new("de1-gateway.shocklink.net", Guid.Parse("bc849182-89e0-43ff-817b-32400be3f97d"), "71WZxCwCAIBJNgNG2pgdaHxHdaipUKmA6MalZUXNZhv3IkV7GB1ObxA35ud4tkPz", host.Services.GetRequiredService<ILogger<OpenShockLiveControlClient>>());
await controlClient.InitializeAsync();

await Task.Delay(-1);