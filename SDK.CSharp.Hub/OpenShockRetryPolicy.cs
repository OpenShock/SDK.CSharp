using Microsoft.AspNetCore.SignalR.Client;

namespace OpenShock.SDK.CSharp.Hub;

public sealed class OpenShockRetryPolicy : IRetryPolicy
{
    private static readonly TimeSpan[] RetrySteps =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    ];
    
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        var retryStepLastIndex = RetrySteps.Length - 1;
        var retryStep = Math.Min(retryContext.PreviousRetryCount, retryStepLastIndex);
        return RetrySteps[retryStep];
    }
}