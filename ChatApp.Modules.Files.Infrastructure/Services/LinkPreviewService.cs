using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ChatApp.Modules.Files.Application.DTOs.Responses;
using ChatApp.Modules.Files.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Files.Infrastructure.Services;

public partial class LinkPreviewService : ILinkPreviewService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LinkPreviewService> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    public LinkPreviewService(IHttpClientFactory httpClientFactory, ILogger<LinkPreviewService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<LinkPreviewDto?> GetPreviewAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme != "http" && uri.Scheme != "https")
            return null;

        // Etibarsız host-ları erkən filtrə et (166.a, localhost, IP kimi)
        if (!uri.Host.Contains('.') || uri.Host.Length < 4 ||
            uri.Host.EndsWith('.') || uri.HostNameType == UriHostNameType.IPv4 ||
            uri.HostNameType == UriHostNameType.IPv6)
            return null;

        // TLD minimum 2 simvol olmalıdır (example.com, not example.a)
        var lastDotIndex = uri.Host.LastIndexOf('.');
        if (lastDotIndex >= 0 && uri.Host.Length - lastDotIndex - 1 < 2)
            return null;

        // Check cache
        if (_cache.TryGetValue(url, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Preview;

        try
        {
            using var client = _httpClientFactory.CreateClient("LinkPreview");
            client.Timeout = RequestTimeout;
            client.DefaultRequestHeaders.Add("User-Agent", "ChatApp LinkPreview/1.0");

            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                return null;

            // Limit response size to 256KB
            var html = await ReadLimitedAsync(response.Content, 256 * 1024, cancellationToken);
            if (string.IsNullOrEmpty(html))
                return null;

            var preview = ParseOgTags(html, uri);
            _cache[url] = new CacheEntry(preview, DateTime.UtcNow.Add(CacheDuration));
            return preview;
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException or OperationCanceledException)
        {
            // Etibarsız və ya əlçatmaz URL-lər üçün debug log yetərlidir, warning lazım deyil
            _logger.LogDebug("Link preview fetch failed for {Url}: {Message}", url, ex.Message);
            return null;
        }
    }

    private static LinkPreviewDto? ParseOgTags(string html, Uri uri)
    {
        var title = ExtractMetaContent(html, "og:title")
                    ?? ExtractTag(html, "title");
        var description = ExtractMetaContent(html, "og:description")
                          ?? ExtractMetaContent(html, "description");
        var imageUrl = ExtractMetaContent(html, "og:image");

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
            return null;

        // Resolve relative image URL
        if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            imageUrl = new Uri(uri, imageUrl).AbsoluteUri;
        }

        return new LinkPreviewDto(
            Url: uri.AbsoluteUri,
            Title: title?.Trim(),
            Description: description?.Trim().Length > 200
                ? description.Trim()[..200] + "..."
                : description?.Trim(),
            ImageUrl: imageUrl,
            Domain: uri.Host
        );
    }

    private static string? ExtractMetaContent(string html, string property)
    {
        // Match both property="og:title" and name="description" patterns
        var match = Regex.Match(html,
            $"""<meta[^>]*(?:property|name)=["']{Regex.Escape(property)}["'][^>]*content=["']([^"']*)["']""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (match.Success)
            return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);

        // Try reversed attribute order: content before property
        match = Regex.Match(html,
            $"""<meta[^>]*content=["']([^"']*)["'][^>]*(?:property|name)=["']{Regex.Escape(property)}["']""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    private static string? ExtractTag(string html, string tagName)
    {
        var match = Regex.Match(html, $"<{tagName}[^>]*>([^<]*)</{tagName}>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    private static async Task<string> ReadLimitedAsync(HttpContent content, int maxBytes, CancellationToken ct)
    {
        using var stream = await content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        var buffer = new char[maxBytes];
        var read = await reader.ReadBlockAsync(buffer, 0, maxBytes);
        return new string(buffer, 0, read);
    }

    private record CacheEntry(LinkPreviewDto? Preview, DateTime ExpiresAt);
}