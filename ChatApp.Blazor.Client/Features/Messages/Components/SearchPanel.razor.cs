using Microsoft.AspNetCore.Components;
using ChatApp.Blazor.Client.Models.Search;
using System.Globalization;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class SearchPanel : IAsyncDisposable
{
    #region Parameters

    /// <summary>
    /// Direct Conversation ID-si.
    /// </summary>
    [Parameter] public Guid? ConversationId { get; set; }

    /// <summary>
    /// Channel ID-si.
    /// </summary>
    [Parameter] public Guid? ChannelId { get; set; }

    /// <summary>
    /// Direct Message-dır? (false = Channel)
    /// </summary>
    [Parameter] public bool IsDirectMessage { get; set; }

    /// <summary>
    /// Panel bağlama callback-i.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// Mesaja naviqasiya callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnNavigateToMessage { get; set; }

    /// <summary>
    /// Axtarış funksiyası.
    /// Parameters: targetId, query, page, pageSize
    /// </summary>
    [Parameter] public Func<Guid, string, int, int, Task<SearchResultsDto?>>? SearchFunc { get; set; }

    #endregion

    #region Private Fields

    private bool _disposed = false;

    /// <summary>
    /// Axtarış sorğusu.
    /// </summary>
    private string searchQuery = string.Empty;

    /// <summary>
    /// Axtarış davam edir?
    /// </summary>
    private bool isSearching = false;

    /// <summary>
    /// Axtarış nəticələri.
    /// </summary>
    private SearchResultsDto? searchResults;

    /// <summary>
    /// Debouncing üçün CancellationToken.
    /// </summary>
    private CancellationTokenSource? _searchCts;

    /// <summary>
    /// Cache-lənmiş qruplanmış nəticələr.
    /// </summary>
    private List<IGrouping<DateTime, SearchResultDto>>? _cachedGroupedResults;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Nəticələri tarixə görə qruplaşdırır.
    /// Cache-lənmiş - yalnız searchResults dəyişdikdə yenidən hesablanır.
    /// </summary>
    private List<IGrouping<DateTime, SearchResultDto>> GroupedResults
    {
        get
        {
            if (_cachedGroupedResults == null && searchResults?.Results != null)
            {
                _cachedGroupedResults = searchResults.Results
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .GroupBy(r => r.CreatedAtUtc.Date)
                    .ToList();
            }
            return _cachedGroupedResults ?? [];
        }
    }

    #endregion

    #region Search Methods

    /// <summary>
    /// Input dəyişikliyi handler (debounced).
    /// </summary>
    private async Task HandleSearchInput()
    {
        // Əvvəlki axtarışı ləğv et
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        // Minimum 3 simvol lazımdır
        if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 3)
        {
            searchResults = null;
            isSearching = false;
            StateHasChanged();
            return;
        }

        // Debounce - 300ms gözlə
        try
        {
            await Task.Delay(300, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await PerformSearch(token);
    }

    /// <summary>
    /// Axtarışı icra edir.
    /// </summary>
    private async Task PerformSearch(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        isSearching = true;
        StateHasChanged();

        try
        {
            if (SearchFunc != null)
            {
                var targetId = IsDirectMessage ? ConversationId!.Value : ChannelId!.Value;
                searchResults = await SearchFunc(targetId, searchQuery, 1, 30);
                _cachedGroupedResults = null; // Cache invalidate
            }
        }
        catch
        {
            searchResults = null;
            _cachedGroupedResults = null; // Cache invalidate
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                isSearching = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Axtarışı təmizləyir.
    /// </summary>
    private void ClearSearch()
    {
        searchQuery = string.Empty;
        searchResults = null;
        _cachedGroupedResults = null; // Cache invalidate
        _searchCts?.Cancel();
        isSearching = false;
    }

    /// <summary>
    /// Mesaja naviqasiya edir.
    /// </summary>
    private async Task NavigateToMessage(Guid messageId)
    {
        await OnNavigateToMessage.InvokeAsync(messageId);
    }

    #endregion

    #region Formatting

    /// <summary>
    /// Tarixi formatlanır.
    /// "Today", "Yesterday", "December 23", "December 23, 2024"
    /// </summary>
    private static string FormatDate(DateTime date)
    {
        var today = DateTime.Today;
        if (date == today)
            return "Today";
        if (date == today.AddDays(-1))
            return "Yesterday";
        if (date.Year == today.Year)
            return date.ToString("MMMM d", CultureInfo.InvariantCulture);
        return date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
    }

    #endregion

    #region IAsyncDisposable

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;

        _cachedGroupedResults = null;
        searchResults = null;
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }

    #endregion
}