using OpenShock.SDK.CSharp.Models;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace OpenShock.SDK.CSharp.Tests;

public sealed class TestTokenEndpoints
{
    [Test]
    public async Task TestGetTokenSelf()
    {
        var client = ClientUtils.ApiClient((message) => ClientUtils.RespondJsonFile("GetTokenSelf/Success"));

        var tokenSelf = await client.GetTokenSelf();
        if (!tokenSelf.IsT0) Assert.Fail("Failed to get token self, not success");

        var permissions = tokenSelf.AsT0.Value.Permissions;

        await Assert.That(permissions.Contains(PermissionType.Shockers_Use)).IsTrue();
        await Assert.That(permissions.Contains(PermissionType.Usershares_Edit)).IsTrue();
        await Assert.That(permissions.Contains(PermissionType.Publicshares_Pause)).IsTrue();

        // Permissions this SDK version doesn't know about must not fail the whole response
        await Assert.That(permissions.Contains(PermissionType.Unknown)).IsTrue();
    }
}
