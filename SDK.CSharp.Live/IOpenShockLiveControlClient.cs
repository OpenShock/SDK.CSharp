using OpenShock.SDK.CSharp.Live.LiveControlModels;
using OpenShock.SDK.CSharp.Updatables;

namespace OpenShock.SDK.CSharp.Live;

public interface IOpenShockLiveControlClient
{
    public string Gateway { get; }
    public Guid DeviceId { get; }
    
    public IAsyncUpdatable<ulong> Latency { get; }
    public IAsyncUpdatable<WebsocketConnectionState> State { get; }

    # region Events

    public event Func<Task>? OnDispose;
    public event Func<Task>? OnDeviceNotConnected;
    public event Func<Task>? OnDeviceConnected;

    #endregion

    #region Send Methods

    public Task SendFrame(ClientLiveFrame frame);

    #endregion
}