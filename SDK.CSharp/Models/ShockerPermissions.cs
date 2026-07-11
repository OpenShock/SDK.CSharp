namespace OpenShock.SDK.CSharp.Models;

public sealed class ShockerPermissions
{
    public required bool Vibrate { get; set; }
    public required bool Sound { get; set; }
    public required bool Shock { get; set; }
    public bool Live { get; set; } = false;
}
