using System.Text.Json;
using System.Text.Json.Serialization;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp.Serialization;

public sealed class PermissionTypeConverter : JsonConverter<PermissionType>
{
    public override PermissionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected string value");
        var name = reader.GetString()!;

        // Backends may introduce permissions newer than this SDK, don't blow up on them
        return PermissionTypeBindings.NameToPermissionType.TryGetValue(name, out var record)
            ? record.PermissionType
            : PermissionType.Unknown;
    }

    public override void Write(Utf8JsonWriter writer, PermissionType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(PermissionTypeBindings.PermissionTypeToName[value].Name);
    }
}
