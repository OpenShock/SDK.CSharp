using Microsoft.Extensions.Logging;

namespace OpenShock.SDK.CSharp.Hub;

public sealed class HubClientOptions : ApiClientOptions
{
    /// <summary>
    /// Optional configuration for the logging builder.
    /// </summary>
    public Action<ILoggingBuilder>? ConfigureLogging { get; set; } = null;

    
}