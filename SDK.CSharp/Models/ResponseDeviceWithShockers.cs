namespace OpenShock.SDK.CSharp.Models;

public sealed class ResponseDeviceWithShockers : ResponseDevice
{
    public required IEnumerable<ShockerResponse> Shockers { get; set; }
}