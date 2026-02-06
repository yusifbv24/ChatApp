namespace ChatApp.Blazor.Client.Models.Enums
{
    /// <summary>
    /// Circuit Breaker States
    /// PATTERN USED BY: AWS SDK, Azure SDK, Netflix Hystrix
    ///
    /// States:
    /// - Closed: Normal operation, requests pass through
    /// - Open: Circuit is broken, requests fail fast (server is down)
    /// - HalfOpen: Testing if server recovered
    /// </summary>
    public enum CircuitState
    {
        Closed,     // Normal - allow connections
        Open,       // Broken - fail fast, don't try to connect
        HalfOpen    // Testing - try one connection to see if recovered
    }
}