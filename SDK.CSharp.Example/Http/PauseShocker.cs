using OpenShock.SDK.CSharp;

namespace SDK.CSharp.Example.Http;

public sealed class PauseShocker : IExample
{
    private readonly ExampleConfig _config;

    public PauseShocker(ExampleConfig config)
    {
        _config = config;
    }


    public async Task Start()
    {
        var apiClient = new OpenShockApiClient(new ApiClientOptions
        {
            Token = _config.ApiToken
        });

        var firstShocker = _config.Shockers.First();

        var response = await apiClient.PauseShocker(firstShocker, true);

        response.Switch(
            success => Console.WriteLine("Shocker paused: " + success.Value),
            error => Console.WriteLine("Shocker not found")
        );

        Console.WriteLine("Press enter to unpause again");
        Console.ReadLine();
        
        response = await apiClient.PauseShocker(firstShocker, false);
        
        response.Switch(
            success => Console.WriteLine("Shocker paused: " + success.Value),
            error => Console.WriteLine("Shocker not found")
        );
    }
}