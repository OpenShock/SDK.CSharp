using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using OpenShock.SDK.CSharp;

namespace SDK.CSharp.Tests;

public static class ClientUtils
{
    private const string MockBasePath = "ApiMockResponses/"; 
    
    public static OpenShockApiClient ApiClient(Func<HttpRequestMessage, HttpResponseMessage> sendAsync)
    {
        var httpClient =
            new HttpClient(new HttpMessageHandlerStub((message, token) => Task.FromResult(sendAsync(message))))
            {
                BaseAddress = new Uri("http://localhost")
            };
        return new OpenShockApiClient(httpClient);
    }

    public static OpenShockApiClient ApiClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
    {
        var httpClient =
            new HttpClient(new HttpMessageHandlerStub((message, token) => sendAsync(message)))
            {
                BaseAddress = new Uri("http://localhost")
            };
        return new OpenShockApiClient(httpClient);
    }
    
    public static HttpResponseMessage RespondJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK,
        string contentType = MediaTypeNames.Application.Json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, contentType)
        };
    }

    public static async Task<HttpResponseMessage> RespondJsonFile(string path,
        HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = MediaTypeNames.Application.Json)
    {
        var fullPath = Path.Combine(MockBasePath, $"{path}.json");
        if(!File.Exists(fullPath)) throw new FileNotFoundException("File not found " + fullPath, fullPath);
        var fileContent = await File.ReadAllTextAsync(fullPath);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(fileContent, Encoding.UTF8, contentType)
        };
    }
}