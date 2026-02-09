namespace ChatApp.Blazor.Client.Infrastructure.Http;

/// <summary>
/// Centralized token refresh service that prevents concurrent refresh attempts.
/// All handlers share this service so only one refresh call happens at a time.
/// When multiple 401s occur simultaneously, only ONE refresh request is made.
/// Refresh is session-based (cookie) — no localStorage dependency.
/// </summary>
public class TokenRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Task<bool>? _activeRefreshTask;
    private DateTime _lastRefreshUtc = DateTime.MinValue;
    private const int RefreshWindowSeconds = 5; // Increased from 2 to handle slow networks

    public TokenRefreshService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Attempts to refresh the token. Concurrent callers will await the same refresh operation.
    /// Returns true if refresh succeeded, false otherwise.
    /// </summary>
    public async Task<bool> TryRefreshAsync()
    {
        // Fast path: If a refresh succeeded very recently, skip — token is fresh
        if ((DateTime.UtcNow - _lastRefreshUtc).TotalSeconds < RefreshWindowSeconds)
            return true;

        // If there's an active refresh in progress, wait for it instead of starting a new one
        var activeTask = _activeRefreshTask;
        if (activeTask != null)
        {
            try
            {
                return await activeTask;
            }
            catch
            {
                // If the active task failed, we'll try again below
            }
        }

        await _refreshLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock (another thread might have refreshed)
            if ((DateTime.UtcNow - _lastRefreshUtc).TotalSeconds < RefreshWindowSeconds)
                return true;

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
}