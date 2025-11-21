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
    /// Authenticates user - POST /api/auth/login
    /// </summary>
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var result = await _apiClient.PostAsync<LoginResponse>("/api/auth/login", request);

        if (result.IsSuccess && result.Value != null)
        {
            await _authStateProvider.MarkUserAsAuthenticated(result.Value);

            // Load current user info
            var user = await _authStateProvider.GetCurrentUserAsync();
            _userState.CurrentUser = user;
        }

        return result;
    }

    /// <summary>
    /// Refreshes access token - POST /api/auth/refresh
    /// </summary>
    public async Task<Result<LoginResponse>> RefreshTokenAsync()
    {
        var refreshToken = await _authStateProvider.GetRefreshTokenAsync();

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result.Failure<LoginResponse>("No refresh token available");
        }

        var request = new RefreshTokenRequest(refreshToken);
        var result = await _apiClient.PostAsync<LoginResponse>("/api/auth/refresh", request);

        if (result.IsSuccess && result.Value != null)
        {
            await _authStateProvider.MarkUserAsAuthenticated(result.Value);

            // Reload current user info
            var user = await _authStateProvider.GetCurrentUserAsync();
            _userState.CurrentUser = user;
        }

        return result;
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
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        return await _authStateProvider.GetCurrentUserAsync();
    }
}
