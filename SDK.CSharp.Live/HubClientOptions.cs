using Microsoft.Extensions.Logging;

namespace OpenShock.SDK.CSharp.Live;

public sealed class HubClientOptions
{
    public Uri Server { get; set; } = new Uri("https://api.shocklink.net");
    public required string Token { get; set; }

    public Action<ILoggingBuilder>? ConfigureLogging { get; set; } = null;

}