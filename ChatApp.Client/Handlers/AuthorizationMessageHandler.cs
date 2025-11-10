using ChatApp.Client.Services.Authentication;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;

namespace ChatApp.Client.Handlers
{
    public class AuthorizationMessageHandler : DelegatingHandler
    {
        private readonly ITokenService _tokenService;
        private readonly NavigationManager _navigationManager;

        public AuthorizationMessageHandler(
            ITokenService tokenService,
            NavigationManager navigationManager)
        {
            _tokenService = tokenService;
            _navigationManager = navigationManager;
        }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Get the current access token from storage
            var token = await _tokenService.GetAccessTokenAsync();

            // Only add Authorization header if we have a token
            // If user is not logged in, token will be null, and that's OK
            // Some endpoints (like /api/auth/login) don't require authentication
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Add the Authorization header with Bearer scheme
                // This is the standard format for JWT authentication
                // "Bearer" is a keyword defined by the OAuth 2.0 spec
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Send the request with the Authorization header
            // base.SendAsync() passes the request to the next handler in the pipeline
            var response = await base.SendAsync(request, cancellationToken);

            // Handle 401 Unauthorized responses
            // This means the token is invalid or expired
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // The token might be expired. Let's check if we can refresh it.
                // But ONLY if this isn't already a refresh or login request
                // (to avoid infinite loops)
                var isAuthEndpoint = request.RequestUri?.AbsolutePath.Contains("/api/auth") ?? false;

                if (!isAuthEndpoint)
                {
                    // Try to refresh the token
                    // If we have a valid refresh token, this will get us a new access token
                    // We can't inject AuthenticationService here (circular dependency)
                    // so we'd need to implement refresh logic differently
                    // For now, we'll redirect to login if we get 401

                    // Redirect to login page
                    // The user will need to log in again
                    _navigationManager.NavigateTo("/login", forceLoad: true);
                }
            }

            return response;
        }
    }
}