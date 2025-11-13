using Blazored.LocalStorage;

namespace ChatApp.Blazor.Client.Infrastructure.Storage;

/// <summary>
/// Implementation of storage service using Blazored.LocalStorage
/// </summary>
public class StorageService : IStorageService
{
    private readonly ILocalStorageService _localStorage;

    public StorageService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            return await _localStorage.GetItemAsync<T>(key);
        }
        catch
        {
            return default;
        }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        await _localStorage.SetItemAsync(key, value);
    }

    public async Task RemoveItemAsync(string key)
    {
        await _localStorage.RemoveItemAsync(key);
    }

    public async Task ClearAsync()
    {
        await _localStorage.ClearAsync();
    }

    public async Task<bool> ContainKeyAsync(string key)
    {
        return await _localStorage.ContainKeyAsync(key);
    }
}
