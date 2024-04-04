using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Live;

var apiClient = new OpenShockApiClient(new ApiClientOptions()
{
    Token = "vYqcHzz0XeALfo3vzQD4Wh7KjqbJeuvZsPz8jlJrtBlfGTF9qKhxtKSrzvZO1A53"
});

var a = await apiClient.GetOwnShockers();

var apiLiveClient = new OpenShockApiLiveClient(new ApiLiveClientOptions()
{
    Token = "API_TOKEN_HERE"
});

await apiLiveClient.StartAsync();

await Task.Delay(-1);