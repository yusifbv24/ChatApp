using ChatApp.Blazor.Client.Infrastructure.Storage;
using ChatApp.Blazor.Client.Models.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatApp.Blazor.Client.Infrastructure.Auth
{
    /// <summary>
    /// Custom authentication state provider for JWT-based authentication
    /// </summary>
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IStorageService _storageService;
        private readonly JwtSecurityTokenHandler _jwtHandler;

        private const string AccessTokenKey = "accessToken";
        private const string RefreshTokenKey = "refreshToken";

        public CustomAuthStateProvider(
            IStorageService storageService)
        {
            _storageService = storageService;
            _jwtHandler = new JwtSecurityTokenHandler();
        }


        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var accessToken = await _storageService.GetItemAsync<string>(AccessTokenKey);

            if (string.IsNullOrEmpty(accessToken))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            try
            {
                var claims = ParseClaimsFromJwt(accessToken);
                var identity = new ClaimsIdentity(claims,"jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                // Token is invalid,clear it 
                await MarkUserAsLoggedOut();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }


        /// <summary>
        /// Marks user as authenticated and stores tokens
        /// </summary>
        public async Task MarkUserAsAuthenticated(LoginResponse loginResponse)
        {
            await _storageService.SetItemAsync(AccessTokenKey, loginResponse.AccessToken);
            await _storageService.SetItemAsync(RefreshTokenKey, loginResponse.RefreshToken);

            var claims = ParseClaimsFromJwt(loginResponse.AccessToken);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);
        }


        /// <summary>
        /// Marks user as logged out clears tokens
        /// </summary>
        public async Task MarkUserAsLoggedOut()
        {
            await _storageService.RemoveItemAsync(AccessTokenKey);
            await _storageService.RemoveItemAsync(RefreshTokenKey);

            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }



        /// <summary>
        /// Gets the current user information
        /// </summary>
        public async Task<UserDto?> GetCurrentUserAsync()
        {
            var authState = await GetAuthenticationStateAsync();
            var user = authState.User;

            if(!user.Identity?.IsAuthenticated?? true)
            {
                return null;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = user.FindFirst(ClaimTypes.Name)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var displayName = user.FindFirst("DisplayName")?.Value;
            var avatarUrl = user.FindFirst("AvatarUrl")?.Value;
            var isAdmin = user.FindFirst("IsAdmin")?.Value == "True";

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return new UserDto(
                Guid.Parse(userId),
                username ?? "",
                email ?? "",
                displayName ?? username ?? "",
                avatarUrl,
                null,
                Guid.Empty,
                true,
                isAdmin,
                DateTime.UtcNow,
                []);
        }


        /// <summary>
        /// Checks if user has a specific permission
        /// </summary>
        public async Task<bool> HasPermissionAsync(string permission)
        {
            var authState = await GetAuthenticationStateAsync();
            var user = authState.User;

            if(!user.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // Check if user is admin (admins have all permissions)
            var isAdmin = user.FindFirst("IsAdmin")?.Value == "True";
            if (isAdmin)
            {
                return true;
            }

            // Check specific permission
            return user.HasClaim("Permission", permission);
        }


        /// <summary>
        /// Gets access token from storage
        /// </summary>
        public async Task<string?> GetAccessTokenAsync()
        {
            return await _storageService.GetItemAsync<string>(AccessTokenKey);
        }


        /// <summary>
        /// Gets refresh token from storage
        /// </summary>
        public async Task<string?> GetRefreshTokenAsync()
        {
            return await _storageService.GetItemAsync<string>(RefreshTokenKey);
        }


        /// <summary>
        /// Parses claims from JWT
        /// </summary>
        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var token = _jwtHandler.ReadJwtToken(jwt);
            return token.Claims;
        }
    }
}