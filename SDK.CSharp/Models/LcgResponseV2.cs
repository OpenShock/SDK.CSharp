namespace OpenShock.SDK.CSharp.Models;

public sealed class LcgResponseV2
{
    /// <summary>Public host of the gateway the hub is connected to.</summary>
    public required string Host { get; set; }

    /// <summary>Public port to connect to (default 443).</summary>
    public required ushort Port { get; set; }

    /// <summary>
    /// Base path prefix the gateway is served under, e.g. "/gateway" (empty for root). The client
    /// builds the full URL and appends the live-control route itself.
    /// </summary>
    public required string PathPrefix { get; set; }

    public required string Country { get; set; }
}
