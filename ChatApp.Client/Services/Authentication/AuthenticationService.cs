using ChatApp.Client.Models.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace ChatApp.Client.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly AuthenticationStateProvider _authStateProvider;

        // We'll implement automatic token refresh with a timer
        private Timer? _tokenRefreshTimer;

        public AuthenticationService(
            IHttpClientFactory httpClientFactory,
            ITokenService tokenService,
            AuthenticationStateProvider authStateProvider)
        {
            // Use the named HttpClient we configured in Program.cs
            // This client is configured with the API base URL
            _httpClient = httpClientFactory.CreateClient("ChatApp.Api");
            _tokenService = tokenService;
            _authStateProvider = authStateProvider;
        }
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

                // Check if the request was successful (2xx status code)
                if (!response.IsSuccessStatusCode)
                {
                    // Login failed - wrong credentials, account locked, etc.
                    // The specific error message is in the response body
                    return null;
                }

                // Deserialize the response body into our LoginResponse model
                // This gives us the JWT tokens
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (loginResponse == null)
                    return null;

                // Store the tokens using TokenService
                // This saves them to localStorage so they survive page refresh
                await _tokenService.SetTokensAsync(
                    loginResponse.AccessToken,
                    loginResponse.RefreshToken,
                    loginResponse.ExpiresIn);

                // CRITICAL: Notify Blazor that authentication state has changed
                // This triggers AuthStateProvider to re-evaluate the user's authentication status
                // Without this, [Authorize] attributes won't know the user is logged in!
                if (_authStateProvider is AuthStateProvider customProvider)
                {
                    customProvider.NotifyAuthenticationStateChanged();
                }

                // Set up automatic token refresh
                // We refresh 5 minutes before expiration to prevent sudden logout
                var refreshTime = TimeSpan.FromSeconds(loginResponse.ExpiresIn - 300); // 300s = 5 minutes
                _tokenRefreshTimer?.Dispose(); // Dispose old timer if it exists
                _tokenRefreshTimer = new Timer(
                    async _ => await RefreshTokenAsync(),
                    null,
                    refreshTime,
                    Timeout.InfiniteTimeSpan); // Don't repeat, we'll set new timer after refresh

                return loginResponse;
            }
            catch (HttpRequestException)
            {
                // Network error, server unreachable, etc.
                return null;
            }
            catch (Exception)
            {
                // Unexpected error during login
                return null;
            }
        }
        public async Task LogoutAsync()
        {
            try
            {
                // Get user ID to send to logout endpoint
                var userId = await _tokenService.GetUserIdFromTokenAsync();

                if (userId.HasValue)
                {
                    // Tell backend to invalidate the refresh token
                    // Even if this fails, we still clear local tokens (fail-safe)
                    try
                    {
                        await _httpClient.PostAsync("/api/auth/logout", null);
                    }
                    catch
                    {
                        // If backend is unreachable, still logout locally
                        // This ensures user can always logout even if server is down
                    }
                }

                // Stop the token refresh timer
                _tokenRefreshTimer?.Dispose();
                _tokenRefreshTimer = null;

                // Clear all tokens from localStorage
                await _tokenService.ClearTokensAsync();

                // Notify Blazor that user is now logged out
                // This triggers re-render of components checking authentication
                if (_authStateProvider is AuthStateProvider customProvider)
                {
                    customProvider.NotifyAuthenticationStateChanged();
                }
            }
            catch (Exception)
            {
                // Even if something goes wrong, ensure tokens are cleared
                await _tokenService.ClearTokensAsync();

                if (_authStateProvider is AuthStateProvider customProvider)
                {
                    customProvider.NotifyAuthenticationStateChanged();
                }
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                // Get the current refresh token
                var refreshToken = await _tokenService.GetRefreshTokenAsync();

                if (string.IsNullOrWhiteSpace(refreshToken))
                    return false;

                // Send refresh token to backend
                var response = await _httpClient.PostAsJsonAsync(
                    "/api/auth/refresh",
                    new { RefreshToken = refreshToken });

                if (!response.IsSuccessStatusCode)
                {
                    // Refresh failed - token invalid, expired, or revoked
                    // User needs to log in again
                    await LogoutAsync();
                    return false;
                }

                // Get new tokens from response
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (loginResponse == null)
                {
                    await LogoutAsync();
                    return false;
                }

                // Store new tokens
                await _tokenService.SetTokensAsync(
                    loginResponse.AccessToken,
                    loginResponse.RefreshToken,
                    loginResponse.ExpiresIn);

                // Set up next automatic refresh
                var refreshTime = TimeSpan.FromSeconds(loginResponse.ExpiresIn - 300);
                _tokenRefreshTimer?.Dispose();
                _tokenRefreshTimer = new Timer(
                    async _ => await RefreshTokenAsync(),
                    null,
                    refreshTime,
                    Timeout.InfiniteTimeSpan);

                // No need to notify auth state - user is still logged in with same identity,
                // just with a new token. The token is used transparently by the API client.

                return true;
            }
            catch (Exception)
            {
                // If refresh fails, log user out for safety
                await LogoutAsync();
                return false;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            return await _tokenService.IsTokenValidAsync();
        }
    }
}