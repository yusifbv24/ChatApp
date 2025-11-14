using ChatApp.Blazor.Client.Infrastructure.Storage;
using System.Net.Http.Headers;

namespace ChatApp.Blazor.Client.Infrastructure.Http
{
    /// <summary>
    /// HTTP message handler that automatically adds JWT token to requests
    /// </summary>
    public class AuthenticationDelegatingHandler:DelegatingHandler
    {
        private readonly IStorageService _storageService;
        private const string AccessTokenKey = "accessToken";

        public AuthenticationDelegatingHandler(IStorageService storageService)
        {
            _storageService=storageService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // Get access token from storage
            var accessToken = await _storageService.GetItemAsync<string>(AccessTokenKey);

            // Add token to request if available
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}