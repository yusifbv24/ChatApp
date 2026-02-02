using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Infrastructure.Http;

/// <summary>
/// Centralized token refresh service that prevents concurrent refresh attempts.
/// All handlers share this service so only one refresh call happens at a time.
/// </summary>
public class TokenRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Task<bool>? _activeRefreshTask;
    private DateTime _lastRefreshUtc = DateTime.MinValue;

    public TokenRefreshService(IHttpClientFactory httpClientFactory, IJSRuntime jsRuntime)
    {
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Attempts to refresh the token. Concurrent callers will await the same refresh operation.
    /// Returns true if refresh succeeded, false otherwise.
    /// </summary>
    public async Task<bool> TryRefreshAsync()
    {
        // If a refresh succeeded very recently (within 2 seconds), skip — token is fresh
        if ((DateTime.UtcNow - _lastRefreshUtc).TotalSeconds < 2)
            return true;

        await _refreshLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if ((DateTime.UtcNow - _lastRefreshUtc).TotalSeconds < 2)
                return true;

            // Check RememberMe preference
            if (!await GetRememberMePreference())
                return false;

            _activeRefreshTask = ExecuteRefreshAsync();
            return await _activeRefreshTask;
        }
        finally
        {
            _activeRefreshTask = null;
            _refreshLock.Release();
        }
    }

    private async Task<bool> ExecuteRefreshAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("Default");
            var response = await httpClient.PostAsync("/api/auth/refresh", null);

            if (response.IsSuccessStatusCode)
            {
                _lastRefreshUtc = DateTime.UtcNow;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> GetRememberMePreference()
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "rememberMe");
            return value == "true";
        }
        catch
        {
            return false;
        }
    }
}