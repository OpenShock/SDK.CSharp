using System.Text.Json.Serialization;
using OpenShock.SDK.CSharp.Live.Utils;
using Semver;

namespace OpenShock.SDK.CSharp.Live.Models;

public sealed class DeviceOnlineState
{
    public required Guid Device { get; set; }
    public required bool Online { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion? FirmwareVersion { get; set; }
}