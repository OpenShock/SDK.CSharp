using System.Text.Json;
using OpenShock.SDK.CSharp;

namespace SDK.CSharp.Example.Http;

public class GetTokenSelfDemo : IExample
{
    private readonly ExampleConfig _config;

    public GetTokenSelfDemo(ExampleConfig config)
    {
        _config = config;
    }
    
    public async Task Start()
    {
        var apiClient = new OpenShockApiClient(new ApiClientOptions
        {
            Token = _config.ApiToken
        });

        var token = await apiClient.GetTokenSelf();
        if (token.IsT1)
        {
            Console.WriteLine("Failed to retrieve token. Unauthenticated or invalid token.");
            return;
        }
        
        Console.WriteLine($"Token: {JsonSerializer.Serialize(token.AsT0.Value)}");
    }
}