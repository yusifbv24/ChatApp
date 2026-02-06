using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

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

/// <summary>
/// Implementation of SignalR chat hub connection
///
/// Production Features:
/// - Exponential Backoff with Jitter for reconnection
/// - Proper logging via ILogger
/// - Connection health monitoring
/// - Automatic token refresh
/// - Thread-safe operations
/// </summary>
public class ChatHubConnection : IChatHubConnection, IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatHubConnection> _logger;
    private readonly string _hubUrl;
    private string? _cachedToken;
    private Timer? _tokenRefreshTimer;
    private Timer? _connectionCheckTimer;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly SemaphoreSlim _connectionLock = new(1, 1); // Prevent concurrent restart attempts
    private bool _isManuallyDisconnecting;
    private int _connectionAttempts;

    // Connection lifecycle events
    public event Func<Exception?, Task>? Reconnecting;
    public event Func<string?, Task>? Reconnected;
    public event Func<Exception?, Task>? Closed;

    public ChatHubConnection(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ChatHubConnection> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        var apiBaseUrl = configuration["ApiBaseAddress"] ?? "http://localhost:7000";
        _hubUrl = $"{apiBaseUrl}/hubs/chat";
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null)
        {
            return;
        }

        _isManuallyDisconnecting = false;
        _connectionAttempts = 0;

        // Fetch access token for SignalR (WebSockets don't automatically include cookies)
        _cachedToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(_cachedToken))
        {
            _logger.LogWarning("Failed to get access token for SignalR connection");
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                // CRITICAL: Use async token provider that always gets fresh token
                options.AccessTokenProvider = async () =>
                {
                    await _tokenLock.WaitAsync();
                    try
                    {
                        return _cachedToken;
                    }
                    finally
                    {
                        _tokenLock.Release();
                    }
                };
            })
            // Server timeout configuration
            .WithServerTimeout(TimeSpan.FromMinutes(2.5))
            // Production-grade retry with Exponential Backoff + Jitter
            .WithAutomaticReconnect(new ExponentialBackoffRetryPolicy())
            .Build();

        // Handle reconnection - refresh token when reconnecting
        _hubConnection.Reconnecting += async (error) =>
        {
            _connectionAttempts++;
            _logger.LogInformation("SignalR reconnecting (attempt {Attempt}). Error: {Error}",
                _connectionAttempts, error?.Message);

            await RefreshTokenAsync();

            if (Reconnecting != null)
                await Reconnecting(error);
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            _connectionAttempts = 0; // Reset on successful connection
            _logger.LogInformation("SignalR reconnected successfully. ConnectionId: {ConnectionId}", connectionId);

            if (Reconnected != null)
                await Reconnected(connectionId);
        };

        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("SignalR connection closed. Error: {Error}", error?.Message);

            if (Closed != null)
                await Closed(error);

            // If connection was closed unexpectedly, try to restart
            if (!_isManuallyDisconnecting && _hubConnection != null)
            {
                _logger.LogInformation("Attempting to restart connection after unexpected close...");
                await TryRestartConnectionAsync();
            }
        };

        try
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            // Schedule restart with exponential backoff
            _ = ScheduleRestartAsync();
        }

        // Start token refresh timer for long sessions
        // JWT expires in 15 min, so refresh at 12 min to avoid 401
        _tokenRefreshTimer = new Timer(async _ =>
        {
            try
            {
                await RefreshTokenAsync();
                _logger.LogDebug("Token refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token refresh failed");
            }
        }, null, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(12));

        // Connection health check - verify connection is alive every 2 minutes
        _connectionCheckTimer = new Timer(async _ =>
        {
            try
            {
                if (_hubConnection?.State == HubConnectionState.Disconnected)
                {
                    _logger.LogInformation("Connection health check: Disconnected, attempting restart...");
                    await TryRestartConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connection health check error");
            }
        }, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    /// <summary>
    /// Refreshes the cached token
    /// </summary>
    private async Task RefreshTokenAsync()
    {
        var newToken = await GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(newToken))
        {
            await _tokenLock.WaitAsync();
            try
            {
                _cachedToken = newToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }
    }

    /// <summary>
    /// Schedules a restart with exponential backoff
    /// </summary>
    private async Task ScheduleRestartAsync()
    {
        if (_isManuallyDisconnecting) return;

        _connectionAttempts++;
        var delay = CalculateBackoffDelay(_connectionAttempts);

        _logger.LogInformation("Scheduling connection restart in {Delay}s (attempt {Attempt})",
            delay.TotalSeconds, _connectionAttempts);

        await Task.Delay(delay);
        await TryRestartConnectionAsync();
    }

    /// <summary>
    /// Calculates backoff delay with jitter
    /// </summary>
    private static TimeSpan CalculateBackoffDelay(int attempt)
    {
        const int maxDelaySeconds = 60;
        const int baseDelaySeconds = 1;

        var exponentialDelay = Math.Min(
            maxDelaySeconds,
            baseDelaySeconds * Math.Pow(2, attempt)
        );

        // Add jitter: ±25%
        var jitter = 0.75 + (Random.Shared.NextDouble() * 0.5);
        return TimeSpan.FromSeconds(exponentialDelay * jitter);
    }

    /// <summary>
    /// Attempts to restart the connection if it's disconnected.
    /// Thread-safe - prevents concurrent restart attempts.
    /// </summary>
    private async Task TryRestartConnectionAsync()
    {
        if (_isManuallyDisconnecting) return;
        if (_hubConnection == null) return;

        // Use lock to prevent concurrent restart attempts
        if (!await _connectionLock.WaitAsync(0))
        {
            _logger.LogDebug("Restart already in progress, skipping");
            return;
        }

        try
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                return;
            }

            await RefreshTokenAsync();
            await _hubConnection.StartAsync();
            _connectionAttempts = 0; // Reset on success
            _logger.LogInformation("Connection restarted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restart connection (attempt {Attempt})", _connectionAttempts);
            // Schedule another attempt with backoff
            _ = ScheduleRestartAsync();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Fetches access token from the server for SignalR authentication
    /// </summary>
    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<SignalRTokenResponse>("/api/auth/signalr-token");
            return response?.Token;
        }
        catch
        {
            return null;
        }
    }

    private record SignalRTokenResponse(string? Token);

    public async Task StopAsync()
    {
        _isManuallyDisconnecting = true;

        // Dispose timers first to prevent race conditions during shutdown
        _tokenRefreshTimer?.Dispose();
        _tokenRefreshTimer = null;
        _connectionCheckTimer?.Dispose();
        _connectionCheckTimer = null;

        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
            _cachedToken = null;
        }

        Console.WriteLine("[SignalR] Connection stopped manually");
    }

    public Task<bool> IsConnectedAsync()
    {
        return Task.FromResult(_hubConnection?.State == HubConnectionState.Connected);
    }

    public async Task SendMessageAsync(string method, params object?[] args)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("Hub connection not started");
        }

        // Check if connection is active before sending
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            // Silently ignore if connection is not active (graceful degradation)
            return;
        }

        try
        {
            // Use SendCoreAsync to properly spread the arguments (fire-and-forget)
            await _hubConnection.SendCoreAsync(method, args);
        }
        catch
        {
            // Gracefully handle errors - don't crash the app
        }
    }

    public async Task<TResult> InvokeAsync<TResult>(string methodName, params object?[] args)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("Hub connection not started");
        }

        // Check if connection is active before invoking
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException($"SignalR connection is not active (State: {_hubConnection.State})");
        }

        try
        {
            // Use InvokeAsync to call server method and wait for result
            return await _hubConnection.InvokeCoreAsync<TResult>(methodName, args);
        }
        catch
        {
            throw;
        }
    }

    public IDisposable On<T>(string methodName, Action<T> handler)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("Hub connection not started");
        }

        return _hubConnection.On(methodName, handler);
    }

    public IDisposable On<T1, T2>(string methodName, Action<T1, T2> handler)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("Hub connection not started");
        }

        return _hubConnection.On(methodName, handler);
    }

    public IDisposable On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("Hub connection not started");
        }

        return _hubConnection.On(methodName, handler);
    }

    public IDisposable On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> handler)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("Hub connection not started");
        }

        return _hubConnection.On(methodName, handler);
    }

    public async ValueTask DisposeAsync()
    {
        _isManuallyDisconnecting = true;

        // Dispose timers
        _tokenRefreshTimer?.Dispose();
        _connectionCheckTimer?.Dispose();

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        // Dispose synchronization primitive
        _tokenLock.Dispose();
    }
}