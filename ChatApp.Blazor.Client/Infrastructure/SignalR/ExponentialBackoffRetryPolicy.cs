using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApp.Blazor.Client.Infrastructure.SignalR;

/// <summary>
/// Production-grade retry policy with Exponential Backoff and Jitter.
///
/// PATTERN USED BY: WhatsApp, Slack, Discord, AWS SDK, Azure SDK
///
/// Benefits:
/// 1. Exponential Backoff: Delays increase exponentially (1s -> 2s -> 4s -> 8s...)
/// 2. Jitter: Random variance prevents "thundering herd" problem
/// 3. Max Delay Cap: Never waits more than 60 seconds
/// 4. Infinite Retry: Never gives up on reconnection
/// </summary>
public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private const int MaxRetryDelaySeconds = 60; // Max 1 minute between retries
    private const int BaseDelaySeconds = 1;      // Start with 1 second
    private static readonly Random Jitter = new();

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        // Exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s, 60s (capped)
        var exponentialDelay = Math.Min(
            MaxRetryDelaySeconds,
            BaseDelaySeconds * Math.Pow(2, retryContext.PreviousRetryCount)
        );

        // Add jitter: ±25% randomness to prevent thundering herd
        // Example: 8s delay becomes random between 6s-10s
        var jitterFactor = 0.75 + (Jitter.NextDouble() * 0.5); // 0.75 to 1.25
        var finalDelay = exponentialDelay * jitterFactor;

        return TimeSpan.FromSeconds(finalDelay);
    }
}