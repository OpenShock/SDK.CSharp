using Microsoft.Extensions.Logging;
using OpenShock.SDK.CSharp;

namespace OpenShock.SDK.CSharp.Hub;

public sealed class HubClientOptions : ApiClientOptions
{
    /// <summary>
    /// Optional configuration for the logging builder.
    /// </summary>
    public Action<ILoggingBuilder>? ConfigureLogging { get; set; } = null;

    
}