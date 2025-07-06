using LucHeart.WebsocketLibrary.Reconnection;

namespace OpenShock.SDK.CSharp.Live;

internal sealed class LiveControlReconnectionPolicy : IReconnectPolicy
{
    public TimeSpan NextReconnectionDelay(ReconnectionContext reconnectionContext)
    {
        return reconnectionContext.Attempt switch
        {
            > 10 => TimeSpan.FromSeconds(30),
            > 3 => TimeSpan.FromSeconds(10),
            > 1 => TimeSpan.FromSeconds(5),
            <= 1 => TimeSpan.FromMilliseconds(0),
        };
    }
}