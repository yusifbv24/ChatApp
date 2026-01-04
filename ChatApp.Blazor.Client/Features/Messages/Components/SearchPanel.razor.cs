using Microsoft.AspNetCore.Components;
using ChatApp.Blazor.Client.Models.Search;
using ChatApp.Blazor.Client.Helpers;
using System.Globalization;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

/// <summary>
/// SearchPanel - Mesaj axtarışı paneli.
///
/// Bu komponent aşağıdakı funksionallıqları təmin edir:
/// - Conversation/Channel daxilində mesaj axtarışı
/// - Debounced search (300ms)
/// - Axtarış nəticələrinin tarixə görə qruplaşdırılması
/// - Highlighted content göstərilməsi
/// - Mesaja naviqasiya
///
/// Komponent partial class pattern istifadə edir:
/// - SearchPanel.razor: HTML template
/// - SearchPanel.razor.cs: C# code-behind (bu fayl)
/// </summary>
public partial class SearchPanel
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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Nəticələri tarixə görə qruplaşdırır.
    /// </summary>
    private IEnumerable<IGrouping<DateTime, SearchResultDto>> GroupedResults =>
        searchResults?.Results
            .OrderByDescending(r => r.CreatedAtUtc)
            .GroupBy(r => r.CreatedAtUtc.Date) ?? Enumerable.Empty<IGrouping<DateTime, SearchResultDto>>();

    #endregion

    #region Search Methods

    /// <summary>
    /// Input dəyişikliyi handler (debounced).
    /// </summary>
    private async Task HandleSearchInput()
    {
        // Əvvəlki axtarışı ləğv et
        _searchCts?.Cancel();
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
                searchResults = await SearchFunc(targetId, searchQuery, 1, 50);
            }
        }
        catch
        {
            searchResults = null;
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
    private string FormatDate(DateTime date)
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
}
