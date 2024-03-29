using System.Net;
using System.Text.Json;
using OneOf;
using OneOf.Types;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp;

public class UserRestClient : IUserRestClient
{
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRestClient"/> class. See parameters' descriptions for more information.
    /// </summary>
    /// <param name="httpClient">Pre-configured <see cref="HttpClient"/>. Pre configure authentication, base address and a user agent</param>
    public UserRestClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<OneOf<Success<IReadOnlyCollection<ResponseDeviceWithShockers>>, UnauthenticatedError>> GetOwnShockers(CancellationToken cancellationToken = default)
    {
        using var ownShockersResponse = await _httpClient.GetAsync(OpenShockEndpoints.OwnShockersV1, cancellationToken);
        if (!ownShockersResponse.IsSuccessStatusCode)
        {
            if (ownShockersResponse.StatusCode == HttpStatusCode.Unauthorized) return new UnauthenticatedError();

            throw new OpenShockApiError("Failed to get own shockers", ownShockersResponse.StatusCode);
        }

        var ownShockersStream = await ownShockersResponse.Content.ReadAsStreamAsync();
        var ownShockers =
            await JsonSerializer.DeserializeAsync<IReadOnlyCollection<ResponseDeviceWithShockers>>(
                ownShockersStream, cancellationToken: cancellationToken);
        if (ownShockers == null) throw new OpenShockSdkError("Failed to deserialize own shockers");
        return new Success<IReadOnlyCollection<ResponseDeviceWithShockers>>(ownShockers);
    }
}