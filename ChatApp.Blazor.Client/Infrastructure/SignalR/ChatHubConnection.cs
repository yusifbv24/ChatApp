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
            Console.WriteLine($"SignalR reconnecting: {error?.Message}");
            _cachedToken = await GetAccessTokenAsync();

            // Propagate event to subscribers
            if (Reconnecting != null)
                await Reconnecting(error);
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            Console.WriteLine($"SignalR reconnected with ID: {connectionId}");

            // Propagate event to subscribers
            if (Reconnected != null)
                await Reconnected(connectionId);
        };

        _hubConnection.Closed += async (error) =>
        {
            Console.WriteLine($"SignalR connection closed: {error?.Message}");

            // Propagate event to subscribers
            if (Closed != null)
                await Closed(error);
        };

        await _hubConnection.StartAsync();
        Console.WriteLine("SignalR connection started successfully");
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
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get SignalR token: {ex.Message}");
            return null;
        }
    }

    private record SignalRTokenResponse(string? Token);

    public async Task StopAsync()
    {
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
            Console.WriteLine($"SignalR connection is {_hubConnection.State}. Cannot send message: {method}");
            // Silently ignore if connection is not active (graceful degradation)
            return;
        }

        try
        {
            // Use SendCoreAsync to properly spread the arguments (fire-and-forget)
            await _hubConnection.SendCoreAsync(method, args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending SignalR message '{method}': {ex.Message}");
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
            Console.WriteLine($"SignalR connection is {_hubConnection.State}. Cannot invoke method: {methodName}");
            throw new InvalidOperationException($"SignalR connection is not active (State: {_hubConnection.State})");
        }

        try
        {
            // Use InvokeAsync to call server method and wait for result
            return await _hubConnection.InvokeCoreAsync<TResult>(methodName, args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error invoking SignalR method '{methodName}': {ex.Message}");
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
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}