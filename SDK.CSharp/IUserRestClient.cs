using OneOf;
using OneOf.Types;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp;

public interface IUserRestClient
{
    public Task<OneOf<Success<IReadOnlyCollection<ResponseDeviceWithShockers>>, UnauthenticatedError>> GetOwnShockers(CancellationToken cancellationToken = default);
}