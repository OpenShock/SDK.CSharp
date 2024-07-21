using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using OneOf;
using OneOf.Types;
using OpenShock.SDK.CSharp.Errors;
using OpenShock.SDK.CSharp.Models;
using OpenShock.SDK.CSharp.Problems;
using OpenShock.SDK.CSharp.Serialization;
using OpenShock.SDK.CSharp.Utils;

namespace OpenShock.SDK.CSharp;

public sealed class OpenShockApiClient : IOpenShockApiClient
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

    /// <inheritdoc />
    public async Task<OneOf<Success<IReadOnlyCollection<ResponseDeviceWithShockers>>, UnauthenticatedError>>
        GetOwnShockers(CancellationToken cancellationToken = default)
    {
        using var ownShockersResponse =
            await _httpClient.GetAsync(OpenShockEndpoints.V1.Shockers.OwnShockers, cancellationToken);
        if (!ownShockersResponse.IsSuccess())
        {
            if (ownShockersResponse.StatusCode == HttpStatusCode.Unauthorized) return new UnauthenticatedError();

            throw new OpenShockApiError("Failed to get own shockers", ownShockersResponse.StatusCode);
        }

        return new Success<IReadOnlyCollection<ResponseDeviceWithShockers>>(
            await ownShockersResponse.Content
                .ReadBaseResponseAsJsonAsync<IReadOnlyCollection<ResponseDeviceWithShockers>>(cancellationToken,
                    JsonSerializerOptions));
    }

    /// <inheritdoc />
    public async
        Task<OneOf<Success<LcgResponse>, NotFound, DeviceOffline, DeviceNotConnectedToGateway, UnauthenticatedError>>
        GetDeviceGateway(Guid deviceId, CancellationToken cancellationToken = default)
    {
        using var gatewayResponse =
            await _httpClient.GetAsync(OpenShockEndpoints.V1.Devices.GetGateway(deviceId), cancellationToken);
        if (gatewayResponse.IsSuccess())
        {
            return new Success<LcgResponse>(
                await gatewayResponse.Content.ReadBaseResponseAsJsonAsync<LcgResponse>(cancellationToken,
                    JsonSerializerOptions));
        }

        if (gatewayResponse.StatusCode == HttpStatusCode.Unauthorized) return new UnauthenticatedError();

        if (!gatewayResponse.IsProblem())
            throw new OpenShockApiError("Error from backend is not a problem response", gatewayResponse.StatusCode);

        var problem =
            await gatewayResponse.Content.ReadAsJsonAsync<ProblemDetails>(cancellationToken,
                JsonSerializerOptions);

        return problem.Type switch
        {
            "Device.NotFound" => new NotFound(),
            "Device.NotOnline" => new DeviceOffline(),
            "Device.NotConnectedToGateway" => new DeviceNotConnectedToGateway(),
            _ => throw new OpenShockApiError($"Unknown problem type [{problem.Type}]", gatewayResponse.StatusCode)
        };
    }

    /// <inheritdoc />
    public async Task<RootResponse> GetRoot(CancellationToken cancellationToken = default)
    {
        using var rootResponse = await _httpClient.GetAsync(OpenShockEndpoints.V1.Root, cancellationToken);
        return await rootResponse.Content.ReadBaseResponseAsJsonAsync<RootResponse>(cancellationToken,
            JsonSerializerOptions);
    }

    /// <inheritdoc />
    public async Task<OneOf<Success<SelfResponse>, UnauthenticatedError>> GetSelf(
        CancellationToken cancellationToken = default)
    {
        using var selfResponse = await _httpClient.GetAsync(OpenShockEndpoints.V1.Users.Self, cancellationToken);

        if (!selfResponse.IsSuccess())
        {
            if (selfResponse.StatusCode == HttpStatusCode.Unauthorized) return new UnauthenticatedError();

            throw new OpenShockApiError("Failed to get user self", selfResponse.StatusCode);
        }

        return new Success<SelfResponse>(
            await selfResponse.Content.ReadBaseResponseAsJsonAsync<SelfResponse>(cancellationToken,
                JsonSerializerOptions));
    }

    public async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission, UnauthenticatedError>> ControlShocker(ControlRequest controlRequest)
    {
        using var controlResponse =
            await _httpClient.PostAsJsonAsync(OpenShockEndpoints.V2.Shockers.Control, controlRequest);

        if (controlResponse.IsSuccess()) return new Success();
        
        if (controlResponse.StatusCode == HttpStatusCode.Unauthorized) return new UnauthenticatedError();
        
        if (!controlResponse.IsProblem())
            throw new OpenShockApiError("Error from backend is not a problem response", controlResponse.StatusCode);

        var problem =
            await controlResponse.Content.ReadAsJsonAsync<ShockerControlProblem>(default,
                JsonSerializerOptions);

        return problem.Type switch
        {
            "Shocker.Control.NotFound" => new ShockerNotFoundOrNoAccess(problem.ShockerId),
            "Shocker.Control.Paused" => new ShockerPaused(problem.ShockerId),
            "Shocker.Control.NoPermission" => new ShockerNoPermission(problem.ShockerId),
            _ => throw new OpenShockApiError($"Unknown problem type [{problem.Type}]", controlResponse.StatusCode)
        };
    }

    private string GetUserAgent()
    {
        var liveClientAssembly = GetType().Assembly;
        var liveClientVersion = liveClientAssembly.GetName().Version!;

        string programName;
        Version programVersion;

        if (_apiClientOptions.Program == null)
        {
            (programName, programVersion) = UserAgentUtils.GetAssemblyInfo();
        }
        else
        {
            programName = _apiClientOptions.Program.Name;
            programVersion = _apiClientOptions.Program.Version;
        }

        var runtimeVersion = RuntimeInformation.FrameworkDescription;
        if (string.IsNullOrEmpty(runtimeVersion)) runtimeVersion = "Unknown Runtime";

        return
            $"OpenShock.SDK.CSharp/{liveClientVersion.Major}.{liveClientVersion.Minor}.{liveClientVersion.Build} " +
            $"({runtimeVersion}; {UserAgentUtils.GetOs()};" +
            $" {programName} {programVersion.Major}.{programVersion.Minor}.{programVersion.Build})";
    }
    
}