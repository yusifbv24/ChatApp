using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ChatApp.Client.Services.Authentication
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly ITokenService _tokenService;

        // Cache the authentication state to avoid repeatedly parsing JWT
        // This is safe because we call NotifyAuthenticationStateChanged() whenever
        // the authentication state changes (login, logout, token refresh)
        private AuthenticationState? _cachedAuthState;

        public AuthStateProvider(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // If we have a cached state and it's still valid, return it
            // This prevents unnecessary JWT parsing
            if (_cachedAuthState != null)
            {
                // But first verify the token is still valid
                // We don't want to return authenticated state with an expired token
                var isValid = await _tokenService.IsTokenValidAsync();
                if (isValid)
                {
                    return _cachedAuthState;
                }

                // Token expired - clear cache and fall through to build new state
                _cachedAuthState = null;
            }

            // Try to get claims from the JWT token
            var claims = await _tokenService.GetClaimsFromTokenAsync();

            // If we got claims, user is authenticated
            if (claims.Any())
            {
                // Build the ClaimsIdentity
                // "jwt" is the authentication type - this is what makes
                // Identity.IsAuthenticated return true
                var identity = new ClaimsIdentity(claims, "jwt");

                // Build the ClaimsPrincipal
                var principal = new ClaimsPrincipal(identity);

                // Cache it for performance
                _cachedAuthState = new AuthenticationState(principal);

                return _cachedAuthState;
            }

            // No valid token found - user is not authenticated
            // Return an anonymous (not authenticated) state
            var anonymousIdentity = new ClaimsIdentity(); // No authentication type = not authenticated
            var anonymousPrincipal = new ClaimsPrincipal(anonymousIdentity);

            _cachedAuthState = new AuthenticationState(anonymousPrincipal);
            return _cachedAuthState;
        }
        public void NotifyAuthenticationStateChanged()
        {
            // Clear the cache so next call to GetAuthenticationStateAsync
            // will rebuild the auth state from current token
            _cachedAuthState = null;

            // Call the base class method which fires the event
            // that Blazor listens to
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}