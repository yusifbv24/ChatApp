using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;

namespace ChatApp.Blazor.Client.Infrastructure.SignalR;

/// <summary>
/// Implementation of SignalR chat hub connection
/// </summary>
public class ChatHubConnection : IChatHubConnection, IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly HttpClient _httpClient;
    private readonly string _hubUrl;
    private string? _cachedToken;
    private Timer? _tokenRefreshTimer;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    // Connection lifecycle events
    public event Func<Exception?, Task>? Reconnecting;
    public event Func<string?, Task>? Reconnected;
    public event Func<Exception?, Task>? Closed;

    public ChatHubConnection(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        var apiBaseUrl = configuration["ApiBaseAddress"] ?? "http://localhost:7000";
        _hubUrl = $"{apiBaseUrl}/hubs/chat";
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null)
        {
            return;
        }

        // Fetch access token for SignalR (WebSockets don't automatically include cookies)
        _cachedToken = await GetAccessTokenAsync();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                // Provide access token for authentication
                options.AccessTokenProvider = () => Task.FromResult(_cachedToken);
            })
            // CRITICAL: Must match server timeout configuration
            // Server: KeepAliveInterval = 30s, ClientTimeoutInterval = 2min
            // Client must wait slightly longer to account for network delays
            .WithServerTimeout(TimeSpan.FromMinutes(2.5)) // 2min + 30s buffer
            // Aggressive reconnection strategy: keep trying for much longer
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,             // 1st retry: immediately
                TimeSpan.FromSeconds(2),   // 2nd retry: 2 sec
                TimeSpan.FromSeconds(5),   // 3rd retry: 5 sec
                TimeSpan.FromSeconds(10),  // 4th retry: 10 sec
                TimeSpan.FromSeconds(15),  // 5th retry: 15 sec
                TimeSpan.FromSeconds(20),  // 6th retry: 20 sec
                TimeSpan.FromSeconds(30),  // 7th retry: 30 sec
                TimeSpan.FromSeconds(30),  // 8th retry: 30 sec
                TimeSpan.FromSeconds(30),  // 9th retry: 30 sec
                TimeSpan.FromSeconds(60),  // 10th retry: 1 min
            })
            .Build();

        // Handle reconnection - refresh token when reconnecting
        _hubConnection.Reconnecting += async (error) =>
        {
            await _tokenLock.WaitAsync();
            try
            {
                _cachedToken = await GetAccessTokenAsync();
            }
            finally
            {
                _tokenLock.Release();
            }

            // Propagate event to subscribers
            if (Reconnecting != null)
                await Reconnecting(error);
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            // Propagate event to subscribers
            if (Reconnected != null)
                await Reconnected(connectionId);
        };

        _hubConnection.Closed += async (error) =>
        {
            // Propagate event to subscribers
            if (Closed != null)
                await Closed(error);
        };

        await _hubConnection.StartAsync();

        // Start token refresh timer for long sessions
        // JWT expires in 15 min, so refresh at 12 min to avoid 401 + extra refresh request
        // This ensures token is always fresh when reconnection happens
        _tokenRefreshTimer = new Timer(async _ =>
        {
            try
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
            catch
            {
                // Silently handle token refresh failures
            }
        }, null, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(12));
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
        // Dispose timer first to prevent race conditions during shutdown
        _tokenRefreshTimer?.Dispose();
        _tokenRefreshTimer = null;

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
        // Dispose token refresh timer
        _tokenRefreshTimer?.Dispose();

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        // Dispose synchronization primitive
        _tokenLock.Dispose();
    }
}