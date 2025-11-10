using ChatApp.Client.Services.Storage;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatApp.Client.Services.Authentication
{
    public class TokenService : ITokenService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        // localStorage keys - centralized to avoid typos
        private const string ACCESS_TOKEN_KEY = "accessToken";
        private const string REFRESH_TOKEN_KEY = "refreshToken";
        private const string TOKEN_EXPIRATION_KEY = "tokenExpiration";

        public TokenService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            _tokenHandler = new JwtSecurityTokenHandler();
        }
        public async Task SetTokensAsync(string accessToken, string refreshToken, int expiresInSeconds)
        {
            // Store both tokens
            await _localStorage.SetItemAsync(ACCESS_TOKEN_KEY, accessToken);
            await _localStorage.SetItemAsync(REFRESH_TOKEN_KEY, refreshToken);

            // Calculate and store when the token expires
            // We use UTC to avoid timezone confusion
            var expirationTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);
            await _localStorage.SetItemAsync(TOKEN_EXPIRATION_KEY, expirationTime);
        }

        /// <summary>
        /// Retrieves the access token for use in API calls
        /// The AuthorizationMessageHandler calls this before every API request
        /// </summary>
        public async Task<string?> GetAccessTokenAsync()
        {
            return await _localStorage.GetItemAsync<string>(ACCESS_TOKEN_KEY);
        }

        /// <summary>
        /// Retrieves the refresh token for getting a new access token
        /// Called when the access token expires or is about to expire
        /// </summary>
        public async Task<string?> GetRefreshTokenAsync()
        {
            return await _localStorage.GetItemAsync<string>(REFRESH_TOKEN_KEY);
        }

        public async Task<bool> IsTokenValidAsync()
        {
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(accessToken))
                return false;

            var expiration = await _localStorage.GetItemAsync<DateTime?>(TOKEN_EXPIRATION_KEY);
            if (!expiration.HasValue)
                return false;

            // Token is valid if it hasn't expired yet
            // We subtract a buffer time (5 minutes) to refresh before actual expiration
            var bufferMinutes = 5; // This should come from configuration
            return DateTime.UtcNow < expiration.Value.AddMinutes(-bufferMinutes);
        }

        public async Task<Guid?> GetUserIdFromTokenAsync()
        {
            var claims = await GetClaimsFromTokenAsync();
            var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
                return null;

            if (Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }
        public async Task<IEnumerable<Claim>> GetClaimsFromTokenAsync()
        {
            var accessToken = await GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(accessToken))
                return Enumerable.Empty<Claim>();

            try
            {
                // Parse the JWT token
                // This reads the payload (middle section) and deserializes the JSON into claims
                var token = _tokenHandler.ReadJwtToken(accessToken);

                // Return all claims from the token
                // These will be used to build the ClaimsPrincipal in AuthStateProvider
                return token.Claims;
            }
            catch (Exception)
            {
                // If token parsing fails (corrupted token, invalid format, etc.),
                // return empty claims rather than crashing the app
                // The AuthStateProvider will treat this as "not authenticated"
                return Enumerable.Empty<Claim>();
            }
        }

        public async Task ClearTokensAsync()
        {
            await _localStorage.RemoveItemAsync(ACCESS_TOKEN_KEY);
            await _localStorage.RemoveItemAsync(REFRESH_TOKEN_KEY);
            await _localStorage.RemoveItemAsync(TOKEN_EXPIRATION_KEY);
        }
    }
}