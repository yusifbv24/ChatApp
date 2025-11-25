using System.Net;

namespace ChatApp.Blazor.Client.Infrastructure.Http;

/// <summary>
/// HTTP message handler that automatically refreshes access token on 401 responses
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private bool _refreshing = false;

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

        // If 401 and not already refreshing, try to refresh token
        if (response.StatusCode == HttpStatusCode.Unauthorized && !_refreshing)
        {
            _refreshing = true;
            try
            {
                // Get auth service from DI
                using var scope = _serviceProvider.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<Features.Auth.Services.IAuthService>();

                // Try to refresh token
                var refreshResult = await authService.RefreshTokenAsync();

                if (refreshResult.IsSuccess)
                {
                    // Retry the original request with new access token
                    var retryResponse = await base.SendAsync(request, cancellationToken);
                    return retryResponse;
                }
            }
            finally
            {
                _refreshing = false;
            }
        }

        return response;
    }
}