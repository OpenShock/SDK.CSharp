namespace OpenShock.SDK.CSharp.Live.LiveControlModels;

public sealed class LatencyAnnounceData
{
    public required ulong DeviceLatency { get; set; }
    public required ulong OwnLatency { get; set; }
}