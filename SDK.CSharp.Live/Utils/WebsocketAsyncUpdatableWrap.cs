using OpenShock.MinimalEvents;
using OpenShock.SDK.CSharp.Updatables;

namespace OpenShock.SDK.CSharp.Live.Utils;

public sealed class WebsocketAsyncUpdatableWrap<T>(LucHeart.WebsocketLibrary.Updatables.IAsyncUpdatable<T> observable)
    : IAsyncUpdatable<T>
{
    public T Value => observable.Value;
    public IAsyncMinimalEventObservable<T> Updated => observable.Updated;
}