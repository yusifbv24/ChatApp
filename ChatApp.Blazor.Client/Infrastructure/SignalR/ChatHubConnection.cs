using ChatApp.Blazor.Client.Infrastructure.Auth;
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
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
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

        // Use SendCoreAsync to properly spread the arguments (fire-and-forget)
        await _hubConnection.SendCoreAsync(method, args);
    }

    public async Task<TResult> InvokeAsync<TResult>(string methodName, params object?[] args)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("Hub connection not started");
        }

        // Use InvokeAsync to call server method and wait for result
        return await _hubConnection.InvokeCoreAsync<TResult>(methodName, args);
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