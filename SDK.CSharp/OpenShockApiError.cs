using System.Net;

namespace OpenShock.SDK.CSharp;

public sealed class OpenShockApiError : Exception
{
    public OpenShockApiError(string message, HttpStatusCode statusCode) : base(message)
    {
    }
}