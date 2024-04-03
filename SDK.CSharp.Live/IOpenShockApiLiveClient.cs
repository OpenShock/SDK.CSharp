using Microsoft.AspNetCore.SignalR.Client;
using OpenShock.SDK.CSharp.Live.Models;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp.Live;

public interface IOpenShockApiLiveClient
{
    /// <summary>
    /// SignalR connection to the OpenShock API. Use this to subscribe to events for its connection state.
    /// Use the methods of this interface for everything else.
    /// </summary>
    public HubConnection Connection { get; }
    
    /// <summary>
    /// Starts the connection
    /// </summary>
    /// <returns></returns>
    public Task StartAsync();
    
    /// <summary>
    /// Log event handler
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    public IDisposable OnLog(Func<ControlLogSender, ICollection<ControlLog>> handler);
    
    /// <summary>
    /// Welcome event handler
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    public IDisposable OnWelcome(Func<string> handler);

    /// <summary>
    /// Calls ControlV2
    /// </summary>
    /// <param name="shocks"></param>
    /// <param name="customName"></param>
    /// <returns></returns>
    public Task Control(IEnumerable<Control> shocks, string? customName = null);
}