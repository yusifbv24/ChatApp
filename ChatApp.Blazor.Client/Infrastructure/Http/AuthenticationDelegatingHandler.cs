using System.Net;

namespace ChatApp.Blazor.Client.Infrastructure.Http;

/// <summary>
/// HTTP message handler that automatically refreshes access token on 401 responses.
/// Uses TokenRefreshService to prevent concurrent refresh race conditions.
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
        // Skip token refresh ONLY for refresh endpoint to avoid infinite loops
        // Other auth endpoints (like signalr-token) should still get automatic refresh
        var path = request.RequestUri?.AbsolutePath;
        if (path?.Contains("/api/auth/refresh") == true || path?.Contains("/api/auth/login") == true)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Clone request before sending (HttpRequestMessage can only be sent once)
        var clonedRequest = await CloneRequestAsync(request);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refreshService = _serviceProvider.GetRequiredService<TokenRefreshService>();
            var refreshed = await refreshService.TryRefreshAsync();

            if (refreshed)
            {
                // Retry with the cloned request (original request is already consumed)
                response.Dispose();
                return await base.SendAsync(clonedRequest, cancellationToken);
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);

            // Copy content headers
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Copy options (.NET 5+)
        foreach (var option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }
}