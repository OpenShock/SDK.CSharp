using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SDK.CSharp.Tests;

public class HttpMessageHandlerStub : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

    public HttpMessageHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
    {
        _sendAsync = sendAsync;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _sendAsync(request, cancellationToken);
    }
}