using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using OneOf;
using OneOf.Types;
using OpenShock.SDK.CSharp.Models;
using OpenShock.SDK.CSharp.Serialization;
using OpenShock.SDK.CSharp.Utils;

namespace OpenShock.SDK.CSharp;

public class OpenShockApiClient : IOpenShockApiClient
{
    private readonly ApiClientOptions _apiClientOptions;
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new CustomJsonStringEnumConverter() }
    };

    
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenShockApiClient"/> class. See parameters' descriptions for more information.
    /// </summary>
    /// <param name="apiClientOptions">Options</param>
    public OpenShockApiClient(ApiClientOptions apiClientOptions)
    {
        _apiClientOptions = apiClientOptions;
        _httpClient = new HttpClient
        {
            BaseAddress = apiClientOptions.Server,
            DefaultRequestHeaders =
            {
                { "User-Agent", GetUserAgent() },
                { "OpenShockToken", apiClientOptions.Token }
            }
        };
    }

    public async Task<OneOf<Success<IReadOnlyCollection<ResponseDeviceWithShockers>>, UnauthenticatedError>>
        GetOwnShockers(CancellationToken cancellationToken = default)
    {
        using var ownShockersResponse = await _httpClient.GetAsync(OpenShockEndpoints.OwnShockersV1, cancellationToken);
        if (!ownShockersResponse.IsSuccessStatusCode)
        {
            if (ownShockersResponse.StatusCode == HttpStatusCode.Unauthorized) return new UnauthenticatedError();

            throw new OpenShockApiError("Failed to get own shockers", ownShockersResponse.StatusCode);
        }

        #if NETSTANDARD2_1
        var ownShockersStream = await ownShockersResponse.Content.ReadAsStringAsync();
        #else
        var ownShockersStream = await ownShockersResponse.Content.ReadAsStringAsync(cancellationToken);
        #endif
        

        var ownShockers =
            JsonSerializer.Deserialize<BaseResponse<IReadOnlyCollection<ResponseDeviceWithShockers>>>(ownShockersStream, JsonSerializerOptions);
        if (ownShockers == null) throw new OpenShockSdkError("Failed to deserialize own shockers");
        return new Success<IReadOnlyCollection<ResponseDeviceWithShockers>>(ownShockers.Data!);
    }

    private string GetUserAgent()
    {
        var liveClientAssembly = GetType().Assembly;
        var liveClientVersion = liveClientAssembly.GetName().Version!;


        var entryAssembly = Assembly.GetEntryAssembly();
        var entryAssemblyName = entryAssembly!.GetName();
        var entryAssemblyVersion = entryAssemblyName.Version;

        var runtimeVersion = RuntimeInformation.FrameworkDescription;
        if (string.IsNullOrEmpty(runtimeVersion)) runtimeVersion = "Unknown Runtime";

        return
            $"OpenShock.SDK.CSharp/{liveClientVersion.Major}.{liveClientVersion.Minor}.{liveClientVersion.Build} " +
            $"({runtimeVersion}; {UserAgentUtils.GetOs()};" +
            $" {entryAssemblyName.Name} {entryAssemblyVersion!.Major}.{entryAssemblyVersion.Minor}.{entryAssemblyVersion.Build})";
    }
}