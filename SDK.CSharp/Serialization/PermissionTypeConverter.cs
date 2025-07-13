﻿using System.Text.Json;
using System.Text.Json.Serialization;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp.Serialization;

public sealed class PermissionTypeConverter : JsonConverter<PermissionType>
{
    public override PermissionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    { 
        return PermissionTypeBindings.NameToPermissionType[reader.GetString()!].PermissionType;
    }

    public override void Write(Utf8JsonWriter writer, PermissionType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(PermissionTypeBindings.PermissionTypeToName[value].Name);
    }
}