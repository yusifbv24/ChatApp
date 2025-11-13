namespace ChatApp.Blazor.Client.Infrastructure.Storage;

/// <summary>
/// Interface for browser storage operations (Local and Session storage)
/// </summary>
public interface IStorageService
{
    Task<T?> GetItemAsync<T>(string key);
    Task SetItemAsync<T>(string key, T value);
    Task RemoveItemAsync(string key);
    Task ClearAsync();
    Task<bool> ContainKeyAsync(string key);
}
