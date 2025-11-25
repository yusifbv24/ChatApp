using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace ChatApp.Blazor.Client.Infrastructure.Http;

/// <summary>
/// Custom HTTP message handler that ensures credentials (cookies) are included in all requests
/// This is necessary for Blazor WebAssembly to send cookies to the API
/// </summary>
public class CookieHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Set the browser's Fetch API to include credentials (cookies) with the request
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return base.SendAsync(request, cancellationToken);
    }
}