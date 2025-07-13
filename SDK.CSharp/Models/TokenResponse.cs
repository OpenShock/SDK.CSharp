namespace OpenShock.SDK.CSharp.Models;

public sealed class TokenResponse
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required DateTime CreatedOn { get; set; }
    
    public required DateTime? ValidUntil { get; set; }
    
    public required DateTime? LastUsed { get; set; }
    
    public required IReadOnlyList<PermissionType> Permissions { get; set; }
}