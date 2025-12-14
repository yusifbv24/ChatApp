namespace ChatApp.Blazor.Client.Infrastructure.SignalR;

/// <summary>
/// Interface for SignalR chat hub connection
/// </summary>
public interface IChatHubConnection
{
    // Connection lifecycle events
    event Func<Exception?, Task>? Reconnecting;
    event Func<string?, Task>? Reconnected;
    event Func<Exception?, Task>? Closed;

    Task StartAsync();
    Task StopAsync();
    Task<bool> IsConnectedAsync();
    Task SendMessageAsync(string method, params object[] args);
    Task<TResult> InvokeAsync<TResult>(string methodName, params object[] args);
    IDisposable On<T>(string methodName, Action<T> handler);
    IDisposable On<T1, T2>(string methodName, Action<T1, T2> handler);
    IDisposable On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler);
    IDisposable On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> handler);
}