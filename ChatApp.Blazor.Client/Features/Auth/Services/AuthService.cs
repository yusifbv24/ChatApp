using ChatApp.Blazor.Client.Infrastructure.Auth;
using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.State;

namespace ChatApp.Blazor.Client.Features.Auth.Services;

/// <summary>
/// Implementation of authentication service
/// Handles: POST /api/auth/login, POST /api/auth/refresh, POST /api/auth/logout
/// </summary>
public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly UserState _userState;

    public AuthService(
        IApiClient apiClient,
        CustomAuthStateProvider authStateProvider,
        UserState userState)
    {
        _apiClient = apiClient;
        _authStateProvider = authStateProvider;
        _userState = userState;
    }

    /// <summary>
    /// Authenticates user - POST /api/auth/login (cookies set by backend)
    /// </summary>
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var result = await _apiClient.PostAsync<object>("/api/auth/login", request);

        if (result.IsSuccess)
        {
            // Notify auth state changed (cookies are already set by backend)
            await _authStateProvider.MarkUserAsAuthenticated();

            // Load current user info from /api/auth/me
            var user = await _authStateProvider.GetCurrentUserAsync();
            _userState.CurrentUser = user;

            // Return success (no tokens in response for security)
            return Result.Success(new LoginResponse("", "", 0));
        }

        return Result.Failure<LoginResponse>(result.Error ?? "Login failed");
    }

    /// <summary>
    /// Refreshes access token - POST /api/auth/refresh (cookies rotated by backend)
    /// </summary>
    public async Task<Result<LoginResponse>> RefreshTokenAsync()
    {
        var result = await _apiClient.PostAsync<object>("/api/auth/refresh");

        if (result.IsSuccess)
        {
            // Notify auth state changed (new cookies are already set by backend)
            await _authStateProvider.MarkUserAsAuthenticated();

            // Reload current user info from /api/auth/me
            var user = await _authStateProvider.GetCurrentUserAsync();
            _userState.CurrentUser = user;

            // Return success (no tokens in response for security)
            return Result.Success(new LoginResponse("", "", 0));
        }

        return Result.Failure<LoginResponse>(result.Error ?? "Token refresh failed");
    }

    /// <summary>
    /// Logs out current user - POST /api/auth/logout
    /// </summary>
    public async Task<Result> LogoutAsync()
    {
        var result = await _apiClient.PostAsync("/api/auth/logout");

        await _authStateProvider.MarkUserAsLoggedOut();
        _userState.CurrentUser = null;

        return result;
    }

    /// <summary>
    /// Gets current authenticated user information
    /// </summary>
    public async Task<UserDetailDto?> GetCurrentUserAsync()
    {
        return await _authStateProvider.GetCurrentUserAsync();
    }
}
