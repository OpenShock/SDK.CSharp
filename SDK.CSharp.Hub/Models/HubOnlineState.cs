using System.Text.Json.Serialization;
using OpenShock.SDK.CSharp.Hub.Utils;
using Semver;

namespace OpenShock.SDK.CSharp.Hub.Models;

public sealed class HubOnlineState
{
    public required Guid Device { get; set; }
    public required bool Online { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion? FirmwareVersion { get; set; }
}