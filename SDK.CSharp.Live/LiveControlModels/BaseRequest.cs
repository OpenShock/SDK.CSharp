namespace OpenShock.SDK.CSharp.Live.LiveControlModels;

public class BaseRequest<T>
{
    public required T RequestType { get; set; }
    public object? Data { get; set; }
}