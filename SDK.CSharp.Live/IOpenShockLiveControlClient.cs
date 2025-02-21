using OpenShock.SDK.CSharp.Live.LiveControlModels;
using OpenShock.SDK.CSharp.Models;
using OpenShock.SDK.CSharp.Updatables;

namespace OpenShock.SDK.CSharp.Live;

public interface IOpenShockLiveControlClient
{
    public string Gateway { get; }
    public Guid DeviceId { get; }
    
    public IAsyncUpdatable<ulong> Latency { get; }
    public IAsyncUpdatable<WebsocketConnectionState> State { get; }

    # region Events

    public IAsyncObservable<Guid> OnHubNotConnected { get; }
    public IAsyncObservable<Guid> OnHubConnected { get; }
    public IAsyncObservable<Guid> OnDispose { get; }

    #endregion

    #region Send Methods

    /// <summary>
    /// Intake a shocker frame, and send it to the server whenever a tick happens.
    /// </summary>
    /// <param name="shocker"></param>
    /// <param name="type"></param>
    /// <param name="intensity"></param>
    public void IntakeFrame(Guid shocker, ControlType type, byte intensity);

    #endregion
}