// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Text.Json;

namespace OpenShock.SDK.CSharp.Live.LiveControlModels;

public sealed class BaseResponse<T> where T : Enum
{
    public required T ResponseType { get; set; }
    public JsonDocument? Data { get; set; }
    
}