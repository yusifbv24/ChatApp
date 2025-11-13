namespace ChatApp.Blazor.Client.Infrastructure.SignalR;

/// <summary>
/// Interface for SignalR chat hub connection
/// </summary>
public interface IChatHubConnection
{
    Task StartAsync();
    Task StopAsync();
    Task<bool> IsConnectedAsync();
    Task SendMessageAsync(string method, params object[] args);
    IDisposable On<T>(string methodName, Action<T> handler);
    IDisposable On<T1, T2>(string methodName, Action<T1, T2> handler);
    IDisposable On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler);
}
