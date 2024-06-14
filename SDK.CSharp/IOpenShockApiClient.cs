using OneOf;
using OneOf.Types;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp;

public interface IOpenShockApiClient
{
    /// <summary>
    /// Get own shockers with devices
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success<IReadOnlyCollection<ResponseDeviceWithShockers>>, UnauthenticatedError>> GetOwnShockers(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the gateway a device is connected to
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success<LcgResponse>, NotFound, DeviceOffline, DeviceNotConnectedToGateway, UnauthenticatedError>>
        GetDeviceGateway(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the root for the API, this has some useful information and can be used to check if the API is reachable
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<RootResponse> GetRoot(CancellationToken cancellationToken = default);

}

public struct DeviceOffline;
public struct DeviceNotConnectedToGateway;