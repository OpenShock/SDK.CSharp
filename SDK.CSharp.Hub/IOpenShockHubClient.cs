using Microsoft.AspNetCore.SignalR.Client;
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
    public event Func<ControlLogSender, ICollection<ControlLog>, Task>? OnLog;
    
    /// <summary>
    /// Welcome event handler
    /// </summary>
    public event Func<string, Task>? OnWelcome;
    
    /// <summary>
    /// Whenever something about a device is updated
    /// </summary>
    public event Func<Guid, DeviceUpdateType, Task>? OnDeviceUpdate;
    
    /// <summary>
    /// Device online offline status updates
    /// </summary>
    public event Func<IEnumerable<DeviceOnlineState>, Task>? OnDeviceStatus;
    
    public event Func<Exception?, Task>? Reconnecting;
    public event Func<Exception?, Task>? Closed;
    public event Func<string?, Task>? Reconnected;
    public event Func<string?, Task>? Connected;
    
    public HubConnectionState State { get; }

    /// <summary>
    /// Calls ControlV2
    /// </summary>
    /// <param name="shocks"></param>
    /// <param name="customName"></param>
    /// <returns></returns>
    public Task Control(IEnumerable<Control> shocks, string? customName = null);
}