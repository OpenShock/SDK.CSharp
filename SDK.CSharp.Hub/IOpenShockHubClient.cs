using Microsoft.AspNetCore.SignalR.Client;
using OpenShock.MinimalEvents;
using OpenShock.SDK.CSharp.Hub.Models;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp.Hub;

public interface IOpenShockHubClient
{
    /// <summary>
    /// Starts the connection
    /// </summary>
    /// <returns></returns>
    public Task StartAsync();

    /// <summary>
    /// Log event handler
    /// </summary>
    public IAsyncMinimalEventObservable<LogEventArgs> OnLog { get; }
    
    /// <summary>
    /// Welcome event handler
    /// </summary>
    public IAsyncMinimalEventObservable<string> OnWelcome { get; }
    
    /// <summary>
    /// Whenever something about a hub is updated
    /// </summary>
    public IAsyncMinimalEventObservable<HubUpdateEventArgs> OnHubUpdate { get; }
    
    /// <summary>
    /// Hub online offline status updates
    /// </summary>
    public IAsyncMinimalEventObservable<IReadOnlyList<HubOnlineState>> OnHubStatus { get; }
    
    public IAsyncMinimalEventObservable<string?> OnConnected { get; }
    public IAsyncMinimalEventObservable<string?> OnReconnected { get; }
    public IAsyncMinimalEventObservable<Exception?> OnReconnecting { get; }
    public IAsyncMinimalEventObservable<Exception?> OnClosed { get; }
    
    
    public HubConnectionState State { get; }

    /// <summary>
    /// Calls ControlV2
    /// </summary>
    /// <param name="shocks"></param>
    /// <param name="customName"></param>
    /// <returns></returns>
    public Task Control(IEnumerable<Control> shocks, string? customName = null);
}

public struct HubUpdateEventArgs
{
    public required Guid HubId { get; init; }
    public required HubUpdateType UpdateType { get; init; }
}

public struct LogEventArgs
{
    public required ControlLogSender Sender { get; init; }
    public required IReadOnlyList<ControlLog> Logs { get; init; }
}