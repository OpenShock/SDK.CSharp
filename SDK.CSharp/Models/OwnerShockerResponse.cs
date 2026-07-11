namespace OpenShock.SDK.CSharp.Models;

/// <summary>
/// A user that has shared one or more shockers with the authenticated user, grouped by the hub the shockers live on.
/// </summary>
public sealed class OwnerShockerResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Uri Image { get; set; }
    public required IReadOnlyList<SharedDevice> Devices { get; set; }

    public sealed class SharedDevice
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required IReadOnlyList<SharedShocker> Shockers { get; set; }

        public sealed class SharedShocker
        {
            public required Guid Id { get; set; }
            public required string Name { get; set; }
            public required bool IsPaused { get; set; }
            public required ShockerPermissions Permissions { get; set; }
            public required ShockerLimits Limits { get; set; }
        }
    }
}
