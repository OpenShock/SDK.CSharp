using LucHeart.WebsocketLibrary;
using OpenShock.MinimalEvents;
using OpenShock.SDK.CSharp.Models;
using OpenShock.SDK.CSharp.Updatables;

namespace OpenShock.SDK.CSharp.Live;

public interface IOpenShockLiveControlClient
{
    public string? Gateway { get; }
    public Guid HubId { get; }
    public byte Tps { get; }
    
    public IAsyncUpdatable<ulong> Latency { get; }
    public IAsyncUpdatable<WebsocketConnectionState> State { get; }

    # region Events

    public IAsyncMinimalEventObservable OnHubNotConnected { get; }
    public IAsyncMinimalEventObservable OnHubConnected { get; }
    public IAsyncMinimalEventObservable OnDispose { get; }

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