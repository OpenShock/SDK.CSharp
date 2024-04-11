using System.Text.Json.Serialization;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp.Live.LiveControlModels;

public sealed class ClientLiveFrame
{
    public required Guid Shocker { get; set; }
    public required byte Intensity { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ControlType Type { get; set; }
}