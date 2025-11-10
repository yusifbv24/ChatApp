using System.Security.Claims;

namespace ChatApp.Client.Services.Authentication
{
    public interface ITokenService
    {
        /// <summary>
        /// Stores both tokens after successful login or refresh
        /// </summary>
        Task SetTokensAsync(string accessToken, string refreshToken, int expiresInSeconds);

        /// <summary>
        /// Gets the current access token for API authentication
        /// </summary>
        Task<string?> GetAccessTokenAsync();

        /// <summary>
        /// Gets the refresh token for obtaining a new access token
        /// </summary>
        Task<string?> GetRefreshTokenAsync();

        /// <summary>
        /// Checks if the current access token is still valid (not expired)
        /// </summary>
        Task<bool> IsTokenValidAsync();

        /// <summary>
        /// Extracts the user's ID from the access token
        /// This is stored in the token as ClaimTypes.NameIdentifier
        /// </summary>
        Task<Guid?> GetUserIdFromTokenAsync();

        /// <summary>
        /// Extracts all claims from the access token
        /// Claims include: user ID, username, permissions, expiration time, etc.
        /// </summary>
        Task<IEnumerable<Claim>> GetClaimsFromTokenAsync();

        /// <summary>
        /// Removes all tokens (called on logout)
        /// </summary>
        Task ClearTokensAsync();
    }
}