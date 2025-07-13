using System.Reflection;
using System.Text.Json.Serialization;
using OpenShock.SDK.CSharp.Serialization;

// ReSharper disable InconsistentNaming

namespace OpenShock.SDK.CSharp.Models;

[JsonConverter(typeof(PermissionTypeConverter))]
public enum PermissionType
{
    [PermissionName("shockers.use")] Shockers_Use,

    [PermissionName("shockers.edit")] Shockers_Edit,

    [PermissionName("shockers.pause")] Shockers_Pause,

    [PermissionName("devices.edit")] Devices_Edit,

    [PermissionName("devices.auth")] Devices_Auth
}

public sealed class ParentPermissionAttribute : Attribute
{
    public PermissionType PermissionType { get; }

    public ParentPermissionAttribute(PermissionType permissionType)
    {
        PermissionType = permissionType;
    }
}

public static class PermissionTypeBindings
{
    public static readonly IReadOnlyDictionary<PermissionType, PermissionTypeRecord> PermissionTypeToName;

    public static readonly IReadOnlyDictionary<string, PermissionTypeRecord> NameToPermissionType;

    static PermissionTypeBindings()
    {
        var bindings = GetBindings();
        PermissionTypeToName = bindings.ToDictionary(x => x.PermissionType, x => x);
        NameToPermissionType = bindings.ToDictionary(x => x.Name, x => x);
    }

    private static PermissionTypeRecord[] GetBindings()
    {
#if NETSTANDARD
        var permissionTypes = Enum.GetValues(typeof(PermissionType)).Cast<PermissionType>().ToArray();
#else
        var permissionTypes = Enum.GetValues<PermissionType>();
#endif
        
        var permissionTypeFields = typeof(PermissionType).GetFields();

        var bindings = new PermissionTypeRecord[permissionTypes.Length];

        for (var i = 0; i < permissionTypes.Length; i++)
        {
            var permissionType = permissionTypes[i];

            var field = permissionTypeFields.First(x => x.Name == permissionType.ToString());
            var parents = field.GetCustomAttributes<ParentPermissionAttribute>().Select(x => x.PermissionType);
            var name = field.GetCustomAttribute<PermissionNameAttribute>()!.Name;

            bindings[i] = new PermissionTypeRecord(permissionType, name, parents.ToArray());
        }

        return bindings;
    }
}

public sealed class PermissionNameAttribute : Attribute
{
    public string Name { get; }

    public PermissionNameAttribute(string name)
    {
        Name = name;
    }
}

public record PermissionTypeRecord(PermissionType PermissionType, string Name, IReadOnlyList<PermissionType> Parents);