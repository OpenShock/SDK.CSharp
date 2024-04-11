using OneOf;
using OneOf.Types;
using OpenShock.SDK.CSharp.Live.LiveControlModels;

namespace OpenShock.SDK.CSharp.Live;

public interface IOpenShockLiveControlClient
{
    public ulong Latency { get; }

    public Task SendFrame(ClientLiveFrame frame);
}