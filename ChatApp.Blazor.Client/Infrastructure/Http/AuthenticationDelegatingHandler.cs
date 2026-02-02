using System.Net;

namespace ChatApp.Blazor.Client.Infrastructure.Http;

/// <summary>
/// HTTP message handler that automatically refreshes access token on 401 responses
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;

    public AuthenticationDelegatingHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Skip token refresh for auth endpoints to avoid infinite loops
        if (request.RequestUri?.AbsolutePath.Contains("/api/auth/") == true)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            var refreshService = _serviceProvider.GetRequiredService<TokenRefreshService>();
            var refreshed = await refreshService.TryRefreshAsync();

            if (refreshed)
            {
                // Retry the original request with new access token
                var retryResponse = await base.SendAsync(request, cancellationToken);
                return retryResponse;
            }
        }

        return response;
    }
}
