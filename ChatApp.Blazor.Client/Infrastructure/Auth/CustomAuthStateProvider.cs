using ChatApp.Blazor.Client.Models.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ChatApp.Blazor.Client.Infrastructure.Auth;

/// <summary>
/// Custom authentication state provider using HttpOnly cookies (XSS-proof)
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    public CustomAuthStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Call /api/auth/me to get current user (cookie sent automatically)
            var user = await _httpClient.GetFromJsonAsync<UserDto>("/api/auth/me");

            if (user != null)
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
        }
        catch
        {
            // Not authenticated or error occurred
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
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
    public Task MarkUserAsLoggedOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
        return Task.CompletedTask;
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