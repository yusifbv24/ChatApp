using ChatApp.Blazor.Client.Models.Enums;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace ChatApp.Blazor.Client.Infrastructure.SignalR;

/// <summary>
/// Implementation of SignalR chat hub connection
///
/// Production Features:
/// - Exponential Backoff with Jitter for reconnection
/// - Circuit Breaker pattern (AWS/Azure SDK style)
/// - Network Status API integration (online/offline detection)
/// - Connection health monitoring
/// - Automatic token refresh
/// - Thread-safe operations
/// </summary>
public class ChatHubConnection : IChatHubConnection, IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly string _hubUrl;
    private string? _cachedToken;
    private Timer? _tokenRefreshTimer;
    private Timer? _connectionCheckTimer;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly SemaphoreSlim _connectionLock = new(1, 1); // Prevent concurrent restart attempts
    private bool _isManuallyDisconnecting;
    private int _connectionAttempts;

    // Circuit Breaker state
    private CircuitState _circuitState = CircuitState.Closed;
    private int _consecutiveFailures;
    private DateTime _circuitOpenedAt;
    private const int CircuitBreakerThreshold = 5;           // Open circuit after 5 consecutive failures
    private const int CircuitBreakerResetTimeSeconds = 300;  // 5 minutes before trying again

    // Network & Visibility state
    private bool _isOnline = true;
    private DateTime _lastActiveTime = DateTime.UtcNow;
    private IJSObjectReference? _networkSubscription;
    private IJSObjectReference? _visibilitySubscription;
    private DotNetObjectReference<ChatHubConnection>? _dotNetRef;

    // Connection lifecycle events
    public event Func<Exception?, Task>? Reconnecting;
    public event Func<string?, Task>? Reconnected;
    public event Func<Exception?, Task>? Closed;

    public ChatHubConnection(
        HttpClient httpClient,
        IConfiguration configuration,
        IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
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
        _consecutiveFailures = 0;
        _circuitState = CircuitState.Closed;

        // Subscribe to browser Network Status API (online/offline detection)
        await SubscribeToBrowserEventsAsync();

        // Fetch access token for SignalR (WebSockets don't automatically include cookies)
        _cachedToken = await GetAccessTokenAsync();


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
            // Server timeout - should be > server's ClientTimeoutInterval (30s)
            .WithServerTimeout(TimeSpan.FromSeconds(45))
            // Production-grade retry with Exponential Backoff + Jitter
            .WithAutomaticReconnect(new ExponentialBackoffRetryPolicy())
            .Build();

        // Handle reconnection - refresh token when reconnecting
        _hubConnection.Reconnecting += async (error) =>
        {
            _connectionAttempts++;
            await RefreshTokenAsync();

            if (Reconnecting != null)
                await Reconnecting(error);
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            _connectionAttempts = 0; // Reset on successful connection

            if (Reconnected != null)
                await Reconnected(connectionId);
        };

        _hubConnection.Closed += async (error) =>
        {
            if (Closed != null)
                await Closed(error);

            // If connection was closed unexpectedly, try to restart
            if (!_isManuallyDisconnecting && _hubConnection != null)
            {
                await TryRestartConnectionAsync();
            }
        };

        try
        {
            await _hubConnection.StartAsync();
            ResetCircuitBreaker(); // Reset on successful connection
        }
        catch
        {
            RecordFailure(); // Circuit breaker failure tracking
            _ = ScheduleRestartAsync();
        }

        // Start token refresh timer for long sessions
        // JWT expires in 15 min, so refresh at 12 min to avoid 401
        _tokenRefreshTimer = new Timer(async _ =>
        {
            try
            {
                await RefreshTokenAsync();
            }
            catch
            {
                // Token refresh failed, will retry next interval
            }
        }, null, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(12));

        // Connection health check - verify connection is alive every 2 minutes
        // Chat app: Always runs regardless of tab visibility (messages must arrive 24/7)
        _connectionCheckTimer = new Timer(async _ =>
        {
            try
            {
                // Skip if offline or circuit is open
                if (!_isOnline || (_circuitState == CircuitState.Open && !ShouldAttemptReset()))
                    return;

                if (_hubConnection?.State == HubConnectionState.Disconnected)
                {
                    await TryRestartConnectionAsync();
                }
            }
            catch
            {
                // Health check error, will retry next interval
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
    /// Respects Circuit Breaker and Network Status
    /// </summary>
    private async Task ScheduleRestartAsync()
    {
        if (_isManuallyDisconnecting) return;

        // Circuit Breaker check - don't retry if circuit is open
        if (_circuitState == CircuitState.Open)
        {
            if (!ShouldAttemptReset())
                return;
            _circuitState = CircuitState.HalfOpen;
        }

        // Network Status check - don't retry if offline
        if (!_isOnline)
            return; // OnNetworkStatusChanged will trigger restart when online

        _connectionAttempts++;
        var delay = CalculateBackoffDelay(_connectionAttempts);

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
    /// Respects Circuit Breaker pattern.
    /// </summary>
    private async Task TryRestartConnectionAsync()
    {
        if (_isManuallyDisconnecting || _hubConnection == null || !_isOnline)
            return;

        // Use lock to prevent concurrent restart attempts
        if (!await _connectionLock.WaitAsync(0))
            return;

        try
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
                return;

            await RefreshTokenAsync();
            await _hubConnection.StartAsync();

            // Success! Reset everything
            _connectionAttempts = 0;
            ResetCircuitBreaker();
        }
        catch
        {
            RecordFailure();
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

    #region Circuit Breaker Methods

    /// <summary>
    /// Records a connection failure for Circuit Breaker pattern
    /// Opens circuit after threshold failures
    /// </summary>
    private void RecordFailure()
    {
        _consecutiveFailures++;

        if (_consecutiveFailures >= CircuitBreakerThreshold && _circuitState != CircuitState.Open)
        {
            _circuitState = CircuitState.Open;
            _circuitOpenedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Resets circuit breaker on successful connection
    /// </summary>
    private void ResetCircuitBreaker()
    {
        _circuitState = CircuitState.Closed;
        _consecutiveFailures = 0;
    }

    /// <summary>
    /// Checks if enough time has passed to attempt circuit reset
    /// </summary>
    private bool ShouldAttemptReset()
    {
        if (_circuitState != CircuitState.Open) return true;

        var elapsed = (DateTime.UtcNow - _circuitOpenedAt).TotalSeconds;
        return elapsed >= CircuitBreakerResetTimeSeconds;
    }

    #endregion

    #region Browser Events (Network Status & Page Visibility)

    /// <summary>
    /// Subscribes to browser Network Status and Page Visibility events
    /// Page Visibility is critical for sleep/wake detection - timers don't run during sleep
    /// </summary>
    private async Task SubscribeToBrowserEventsAsync()
    {
        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);

            // Network Status API - detect online/offline
            _networkSubscription = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "chatAppUtils.subscribeToNetworkChange", _dotNetRef);

            // Page Visibility API - detect sleep/wake (critical for reconnection)
            _visibilitySubscription = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "chatAppUtils.subscribeToVisibilityChange", _dotNetRef);

            _isOnline = await _jsRuntime.InvokeAsync<bool>("chatAppUtils.isOnline");
            _lastActiveTime = DateTime.UtcNow;
        }
        catch
        {
            _isOnline = true;
        }
    }

    /// <summary>
    /// Called by JavaScript when network status changes (online/offline)
    /// </summary>
    [JSInvokable]
    public async Task OnNetworkStatusChanged(bool isOnline)
    {
        var wasOffline = !_isOnline;
        _isOnline = isOnline;

        // Came back online - try to reconnect immediately
        if (isOnline && wasOffline && !_isManuallyDisconnecting)
        {
            if (_hubConnection?.State == HubConnectionState.Disconnected)
            {
                // Reset circuit breaker on network restore
                _circuitState = CircuitState.HalfOpen;
                _consecutiveFailures = 0;
                await TryRestartConnectionAsync();
            }
        }
    }

    /// <summary>
    /// Called by JavaScript when page visibility changes (tab visible/hidden or system wake/sleep)
    /// CRITICAL: This fires immediately when system wakes from sleep - timers don't
    /// </summary>
    [JSInvokable]
    public async Task OnVisibilityChanged(bool isVisible)
    {
        if (!isVisible || _isManuallyDisconnecting)
            return;

        // Page became visible - check if we were asleep
        var timeSinceLastActive = DateTime.UtcNow - _lastActiveTime;
        _lastActiveTime = DateTime.UtcNow;

        // If more than 30 seconds passed, we likely woke from sleep
        // Immediately check connection and refresh token
        if (timeSinceLastActive.TotalSeconds > 30)
        {
            // Refresh token first (may have expired during sleep)
            await RefreshTokenAsync();

            // Check connection state and reconnect if needed
            if (_hubConnection?.State == HubConnectionState.Disconnected && _isOnline)
            {
                _circuitState = CircuitState.HalfOpen;
                _consecutiveFailures = 0;
                await TryRestartConnectionAsync();
            }
        }
    }

    #endregion

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

        // Dispose JavaScript subscriptions
        try
        {
            if (_networkSubscription != null)
            {
                await _networkSubscription.InvokeVoidAsync("dispose");
                await _networkSubscription.DisposeAsync();
            }
            if (_visibilitySubscription != null)
            {
                await _visibilitySubscription.InvokeVoidAsync("dispose");
                await _visibilitySubscription.DisposeAsync();
            }
        }
        catch
        {
            // Ignore disposal errors
        }

        _dotNetRef?.Dispose();

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        // Dispose synchronization primitives
        _tokenLock.Dispose();
        _connectionLock.Dispose();
        GC.SuppressFinalize(this);
    }
}