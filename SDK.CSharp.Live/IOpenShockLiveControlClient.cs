using OneOf;
using OneOf.Types;
using OpenShock.SDK.CSharp.Live.LiveControlModels;

namespace OpenShock.SDK.CSharp.Live;

public interface IOpenShockLiveControlClient
{
    public ulong Latency { get; }
    public WebsocketConnectionState State { get; }

    # region Events

    public event Func<Task>? OnDispose;
    public event Func<WebsocketConnectionState, Task>? OnStateUpdate;
    public event Func<Task>? OnDeviceNotConnected;
    public event Func<Task>? OnDeviceConnected;

    #endregion

    #region Send Methods

    public Task SendFrame(ClientLiveFrame frame);

    #endregion
}