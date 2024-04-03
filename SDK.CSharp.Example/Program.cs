using OpenShock.SDK.CSharp.Live;

var apiLiveClient = new OpenShockApiLiveClient(new ApiLiveClientOptions()
{
    Token = "API_TOKEN_HERE"
});

await apiLiveClient.StartAsync();

await Task.Delay(-1);