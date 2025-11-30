using ChatApp.Blazor.Client.Models.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ChatApp.Blazor.Client.Infrastructure.Auth;

/// <summary>
/// Custom authentication state provider using HttpOnly cookies (XSS-proof)
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    public CustomAuthStateProvider(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Call /api/auth/me to get current user (cookie sent automatically)
            var user = await TryGetCurrentUserAsync();

            if (user != null)
            {
                return CreateAuthenticatedState(user);
            }
        }
        catch
        {
            // Not authenticated or error occurred
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Tries to get current user, auto-refreshing token if RememberMe was set
    /// </summary>
    private async Task<UserDto?> TryGetCurrentUserAsync()
    {
        try
        {
            // First try to get user with current access token
            var response = await _httpClient.GetAsync("/api/auth/me");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserDto>();
            }

            // Access token expired or missing - check if RememberMe was set
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var rememberMe = await GetRememberMePreference();

                if (rememberMe)
                {
                    // Try to refresh the access token using refresh token
                    var refreshResponse = await _httpClient.PostAsync("/api/auth/refresh", null);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        // Refresh succeeded - retry getting user info
                        var retryResponse = await _httpClient.GetAsync("/api/auth/me");

                        if (retryResponse.IsSuccessStatusCode)
                        {
                            return await retryResponse.Content.ReadFromJsonAsync<UserDto>();
                        }
                    }
                }
                // RememberMe not set or refresh failed - user must re-login
            }
        }
        catch
        {
            // Error occurred
        }

        return null;
    }

    /// <summary>
    /// Gets RememberMe preference from localStorage
    /// </summary>
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

    /// <summary>
    /// Creates authenticated state from user DTO
    /// </summary>
    private AuthenticationState CreateAuthenticatedState(UserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("DisplayName", user.DisplayName ?? user.Username),
            new("IsAdmin", user.IsAdmin.ToString())
        };

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            claims.Add(new("AvatarUrl", user.AvatarUrl));
        }

        var identity = new ClaimsIdentity(claims, "cookie");
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationState(principal);
    }

    /// <summary>
    /// Marks user as authenticated (cookies are set by backend)
    /// </summary>
    public async Task MarkUserAsAuthenticated()
    {
        // Get updated authentication state from /api/auth/me
        var authState = GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(authState);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Marks user as logged out (cookies are cleared by backend)
    /// </summary>
    public async Task MarkUserAsLoggedOut()
    {
        // Clear RememberMe preference on logout
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "rememberMe");
        }
        catch { }

        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    /// <summary>
    /// Gets the current user information from the API (cookie-based)
    /// </summary>
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDto>("/api/auth/me");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if user has a specific permission
    /// </summary>
    public async Task<bool> HasPermissionAsync(string permission)
    {
        var user = await GetCurrentUserAsync();

        if (user == null)
        {
            return false;
        }

        // Check if user is admin (admins have all permissions)
        if (user.IsAdmin)
        {
            return true;
        }

        // Check specific permission in user's roles
        return user.Roles.Any(r => r.Permissions.Any(p => p.Name == permission));
    }
}