using OpenShock.SDK.CSharp.Live.Models;

namespace OpenShock.SDK.CSharp.Live;

public interface IOpenShockApiLiveClient
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

    /// <summary>
    /// Calls ControlV2
    /// </summary>
    /// <param name="shocks"></param>
    /// <param name="customName"></param>
    /// <returns></returns>
    public Task Control(IEnumerable<Control> shocks, string? customName = null);
}