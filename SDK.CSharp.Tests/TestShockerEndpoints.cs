using TUnit.Assertions;
using TUnit.Core;

namespace OpenShock.SDK.CSharp.Tests;

public sealed class TestShockerEndpoints
{
    [Test]
    public async Task TestGetOwnShockers()
    {
        var client = ClientUtils.ApiClient((message) => ClientUtils.RespondJsonFile("GetOwnShockers/Success"));

        var ownShockers = await client.GetOwnShockers();
        if(!ownShockers.IsT0) Assert.Fail("Failed to get own shockers, not success");
    }
}