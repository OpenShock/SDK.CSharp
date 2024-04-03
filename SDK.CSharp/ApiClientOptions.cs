namespace OpenShock.SDK.CSharp;

public sealed class ApiClientOptions
{
    public Uri Server { get; set; } = new Uri("https://api.shocklink.net");
    public required string Token { get; set; }
}