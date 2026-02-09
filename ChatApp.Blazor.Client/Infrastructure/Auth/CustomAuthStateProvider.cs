using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ChatApp.Blazor.Client.Infrastructure.Auth;

/// <summary>
/// Custom authentication state provider using HttpOnly cookies (XSS-proof)
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly TokenRefreshService _tokenRefreshService;
    private readonly UserState _userState;
    private UserDetailDto? _cachedUser;

    public CustomAuthStateProvider(
        HttpClient httpClient, 
        TokenRefreshService tokenRefreshService, 
        UserState userState)
    {
        _httpClient = httpClient;
        _tokenRefreshService = tokenRefreshService;
        _userState = userState;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Return cached user if available (set during login)
            if (_cachedUser != null)
            {
                return CreateAuthenticatedState(_cachedUser);
            }

            // Call /api/users/me to get current user (cookie sent automatically)
            var user = await TryGetCurrentUserAsync();

            if (user != null)
            {
                _cachedUser = user;
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
    /// Tries to get current user, auto-refreshing token via session cookie
    /// </summary>
    private async Task<UserDetailDto?> TryGetCurrentUserAsync()
    {
        try
        {
            // First try to get user with current access token
            var response = await _httpClient.GetAsync("/api/users/me");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserDetailDto>();
            }

            // Access token expired or missing - try refresh via shared service
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await _tokenRefreshService.TryRefreshAsync();

                if (refreshed)
                {
                    var retryResponse = await _httpClient.GetAsync("/api/users/me");

                    if (retryResponse.IsSuccessStatusCode)
                    {
                        return await retryResponse.Content.ReadFromJsonAsync<UserDetailDto>();
                    }
                }
            }
        }
        catch
        {
            // Error occurred
        }

        return null;
    }

    /// <summary>
    /// Creates authenticated state from user DTO
    /// </summary>
    private static AuthenticationState CreateAuthenticatedState(UserDetailDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
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
    /// Marks user as authenticated with already-fetched user data (avoids extra API call)
    /// </summary>
    public void MarkUserAsAuthenticated(UserDetailDto user)
    {
        _cachedUser = user;
        var authState = Task.FromResult(CreateAuthenticatedState(user));
        NotifyAuthenticationStateChanged(authState);
    }

    /// <summary>
    /// Marks user as logged out (cookies are cleared by backend)
    /// </summary>
    public void MarkUserAsLoggedOut()
    {
        _cachedUser = null;

        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    /// <summary>
    /// Gets the current user information from the API (cookie-based)
    /// </summary>
    public async Task<UserDetailDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDetailDto>("/api/users/me");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if user has a specific permission (uses UserState which stays in sync)
    /// </summary>
    public Task<bool> HasPermissionAsync(string permission)
    {
        // Delegate to UserState which is the single source of truth
        // and stays updated when user data changes
        return Task.FromResult(_userState.HasPermission(permission));
    }
}