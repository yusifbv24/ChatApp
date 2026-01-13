using Microsoft.AspNetCore.Components;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.Helpers;
using System.Globalization;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

/// <summary>
/// Sidebar - Chat haqqında məlumat və əlavə funksionallıqlar paneli.
///
/// Bu komponent aşağıdakı funksionallıqları təmin edir:
/// - Conversation/Channel haqqında məlumat (avatar, ad, üzv sayı)
/// - Sound toggle (mute/unmute)
/// - Favorite messages panel
/// - Links panel (placeholder)
/// - Files and media panel (placeholder)
/// - Context menu (pin, view profile, hide, leave və s.)
///
/// Komponent partial class pattern istifadə edir:
/// - Sidebar.razor: HTML template
/// - Sidebar.razor.cs: C# code-behind (bu fayl)
/// </summary>
public partial class Sidebar
{
    #region Parameters - Chat Type

    /// <summary>
    /// Direct Message-dır? (false = Channel)
    /// </summary>
    [Parameter] public bool IsDirectMessage { get; set; }

    #endregion

    #region Parameters - Direct Message Info

    /// <summary>
    /// DM-də qarşı tərəfin adı.
    /// </summary>
    [Parameter] public string RecipientName { get; set; } = string.Empty;

    /// <summary>
    /// DM-də qarşı tərəfin avatar URL-i.
    /// </summary>
    [Parameter] public string? RecipientAvatarUrl { get; set; }

    /// <summary>
    /// Notes conversation-du? (self-conversation)
    /// </summary>
    [Parameter] public bool IsNotesConversation { get; set; }

    #endregion

    #region Parameters - Channel Info

    /// <summary>
    /// Channel adı.
    /// </summary>
    [Parameter] public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Channel üzv sayı.
    /// </summary>
    [Parameter] public int MemberCount { get; set; }

    #endregion

    #region Parameters - Tracking IDs

    /// <summary>
    /// Conversation ID-si (sub-panel reset üçün).
    /// </summary>
    [Parameter] public Guid? ConversationId { get; set; }

    /// <summary>
    /// Channel ID-si (sub-panel reset üçün).
    /// </summary>
    [Parameter] public Guid? ChannelId { get; set; }

    #endregion

    #region Parameters - Counts

    /// <summary>
    /// Favorite mesaj sayı.
    /// </summary>
    [Parameter] public int FavoriteCount { get; set; }

    /// <summary>
    /// Fayl sayı.
    /// </summary>
    [Parameter] public int FileCount { get; set; }

    /// <summary>
    /// Link sayı.
    /// </summary>
    [Parameter] public int LinkCount { get; set; }

    #endregion

    #region Parameters - Sound/Mute

    /// <summary>
    /// Mute edilib?
    /// </summary>
    [Parameter] public bool IsMuted { get; set; }

    /// <summary>
    /// Mute toggle callback-i.
    /// </summary>
    [Parameter] public EventCallback<bool> OnMuteToggle { get; set; }

    #endregion

    #region Parameters - Favorites Data

    /// <summary>
    /// DM favorite mesajları.
    /// </summary>
    [Parameter] public List<FavoriteDirectMessageDto>? FavoriteDirectMessages { get; set; }

    /// <summary>
    /// Channel favorite mesajları.
    /// </summary>
    [Parameter] public List<FavoriteChannelMessageDto>? FavoriteChannelMessages { get; set; }

    /// <summary>
    /// Favorite-ları yükləmə callback-i.
    /// </summary>
    [Parameter] public EventCallback OnLoadFavorites { get; set; }

    /// <summary>
    /// Mesaja naviqasiya callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnNavigateToMessage { get; set; }

    /// <summary>
    /// Favorite-dən silmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnRemoveFromFavorites { get; set; }

    #endregion

    #region Parameters - Event Callbacks

    /// <summary>
    /// Sidebar bağlama callback-i.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// View profile callback-i (user profile panel açmaq üçün).
    /// </summary>
    [Parameter] public EventCallback OnViewProfile { get; set; }

    #endregion

    #region Private Fields - UI State

    /// <summary>
    /// Context menu görünürmü?
    /// </summary>
    private bool showContextMenu;

    /// <summary>
    /// Favorites panel görünürmü?
    /// </summary>
    private bool showFavoritesPanel;

    /// <summary>
    /// Links panel görünürmü?
    /// </summary>
    private bool showLinksPanel;

    /// <summary>
    /// Files panel görünürmü?
    /// </summary>
    private bool showFilesPanel;

    /// <summary>
    /// Favorites yüklənir?
    /// </summary>
    private bool isLoadingFavorites;

    /// <summary>
    /// Hover edilən mesajın ID-si.
    /// </summary>
    private Guid? hoveredMessageId;

    /// <summary>
    /// Açıq menulu mesajın ID-si.
    /// </summary>
    private Guid? activeMenuMessageId;

    #endregion

    #region Private Fields - Tracking

    /// <summary>
    /// Əvvəlki conversation ID (dəyişiklik detect üçün).
    /// </summary>
    private Guid? _previousConversationId;

    /// <summary>
    /// Əvvəlki channel ID (dəyişiklik detect üçün).
    /// </summary>
    private Guid? _previousChannelId;

    #endregion

    #region Private Fields - Cache

    /// <summary>
    /// Cache-lənmiş qruplanmış DM favorites.
    /// </summary>
    private List<IGrouping<DateTime, FavoriteDirectMessageDto>>? _cachedGroupedDirectFavorites;

    /// <summary>
    /// Cache-lənmiş qruplanmış Channel favorites.
    /// </summary>
    private List<IGrouping<DateTime, FavoriteChannelMessageDto>>? _cachedGroupedChannelFavorites;

    /// <summary>
    /// Əvvəlki FavoriteDirectMessages reference.
    /// </summary>
    private List<FavoriteDirectMessageDto>? _previousFavoriteDirectMessages;

    /// <summary>
    /// Əvvəlki FavoriteChannelMessages reference.
    /// </summary>
    private List<FavoriteChannelMessageDto>? _previousFavoriteChannelMessages;

    #endregion

    #region Computed Properties - Cached

    /// <summary>
    /// Qruplanmış DM favorites - cache-lənmiş.
    /// </summary>
    private List<IGrouping<DateTime, FavoriteDirectMessageDto>> GroupedDirectFavorites
    {
        get
        {
            if (_cachedGroupedDirectFavorites == null && FavoriteDirectMessages != null)
            {
                _cachedGroupedDirectFavorites = FavoriteDirectMessages
                    .GroupBy(m => m.FavoritedAtUtc.Date)
                    .OrderByDescending(g => g.Key)
                    .ToList();
            }
            return _cachedGroupedDirectFavorites ?? new List<IGrouping<DateTime, FavoriteDirectMessageDto>>();
        }
    }

    /// <summary>
    /// Qruplanmış Channel favorites - cache-lənmiş.
    /// </summary>
    private List<IGrouping<DateTime, FavoriteChannelMessageDto>> GroupedChannelFavorites
    {
        get
        {
            if (_cachedGroupedChannelFavorites == null && FavoriteChannelMessages != null)
            {
                _cachedGroupedChannelFavorites = FavoriteChannelMessages
                    .GroupBy(m => m.FavoritedAtUtc.Date)
                    .OrderByDescending(g => g.Key)
                    .ToList();
            }
            return _cachedGroupedChannelFavorites ?? new List<IGrouping<DateTime, FavoriteChannelMessageDto>>();
        }
    }

    /// <summary>
    /// Qrup içindəki mesajları sıralayır - cache olunmuş data ilə.
    /// </summary>
    private static IEnumerable<FavoriteDirectMessageDto> GetOrderedDirectMessages(IGrouping<DateTime, FavoriteDirectMessageDto> group)
        => group.OrderByDescending(m => m.FavoritedAtUtc);

    /// <summary>
    /// Qrup içindəki mesajları sıralayır - cache olunmuş data ilə.
    /// </summary>
    private static IEnumerable<FavoriteChannelMessageDto> GetOrderedChannelMessages(IGrouping<DateTime, FavoriteChannelMessageDto> group)
        => group.OrderByDescending(m => m.FavoritedAtUtc);

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Parameter dəyişiklikləri olduqda sub-panelləri sıfırlayır.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (_previousConversationId != ConversationId || _previousChannelId != ChannelId)
        {
            ResetAllPanels();
            _previousConversationId = ConversationId;
            _previousChannelId = ChannelId;
        }

        // Favorites cache invalidation
        if (!ReferenceEquals(FavoriteDirectMessages, _previousFavoriteDirectMessages))
        {
            _cachedGroupedDirectFavorites = null;
            _previousFavoriteDirectMessages = FavoriteDirectMessages;
        }

        if (!ReferenceEquals(FavoriteChannelMessages, _previousFavoriteChannelMessages))
        {
            _cachedGroupedChannelFavorites = null;
            _previousFavoriteChannelMessages = FavoriteChannelMessages;
        }
    }

    #endregion

    #region Panel Management

    /// <summary>
    /// Bütün sub-panelləri sıfırlayır.
    /// </summary>
    private void ResetAllPanels()
    {
        showFavoritesPanel = false;
        showLinksPanel = false;
        showFilesPanel = false;
        showContextMenu = false;
        activeMenuMessageId = null;
        hoveredMessageId = null;
    }

    /// <summary>
    /// Bütün menuları bağlayır.
    /// </summary>
    private void CloseAllMenus()
    {
        if (showContextMenu) showContextMenu = false;
        if (activeMenuMessageId.HasValue)
        {
            activeMenuMessageId = null;
            hoveredMessageId = null;
        }
    }

    #endregion

    #region Context Menu

    /// <summary>
    /// Context menu-nu toggle edir.
    /// </summary>
    private void ToggleContextMenu()
    {
        showContextMenu = !showContextMenu;
    }

    /// <summary>
    /// Mesaj context menu-sunu toggle edir.
    /// </summary>
    private void ToggleMessageMenu(Guid messageId)
    {
        activeMenuMessageId = activeMenuMessageId == messageId ? null : messageId;
    }

    #endregion

    #region Favorites Panel

    /// <summary>
    /// Favorites panelini açır.
    /// </summary>
    private async Task OpenFavoritesPanel()
    {
        isLoadingFavorites = true;
        showFavoritesPanel = true;
        StateHasChanged();

        await OnLoadFavorites.InvokeAsync();

        isLoadingFavorites = false;
        StateHasChanged();
    }

    /// <summary>
    /// Favorites panelini bağlayır.
    /// </summary>
    private void CloseFavoritesPanel()
    {
        showFavoritesPanel = false;
        activeMenuMessageId = null;
        hoveredMessageId = null;
    }

    /// <summary>
    /// Mesaja naviqasiya edir.
    /// </summary>
    private async Task NavigateToMessage(Guid messageId)
    {
        await OnNavigateToMessage.InvokeAsync(messageId);
    }

    /// <summary>
    /// View context click handler.
    /// </summary>
    private async Task HandleViewContext(Guid messageId)
    {
        activeMenuMessageId = null;
        await OnNavigateToMessage.InvokeAsync(messageId);
    }

    /// <summary>
    /// Favorite-dən silmə handler.
    /// </summary>
    private async Task HandleRemoveFromFavorites(Guid messageId)
    {
        activeMenuMessageId = null;
        await OnRemoveFromFavorites.InvokeAsync(messageId);
    }

    #endregion

    #region Links & Files Panels

    /// <summary>
    /// Links panelini açır.
    /// </summary>
    private void OpenLinksPanel()
    {
        showLinksPanel = true;
    }

    /// <summary>
    /// Links panelini bağlayır.
    /// </summary>
    private void CloseLinksPanel()
    {
        showLinksPanel = false;
    }

    /// <summary>
    /// Files panelini açır.
    /// </summary>
    private void OpenFilesPanel()
    {
        showFilesPanel = true;
    }

    /// <summary>
    /// Files panelini bağlayır.
    /// </summary>
    private void CloseFilesPanel()
    {
        showFilesPanel = false;
    }

    #endregion

    #region Formatting

    /// <summary>
    /// Favorite tarixini formatlanır.
    /// "Today", "Yesterday", "January 5, 2024"
    /// </summary>
    private static string FormatFavoriteDate(DateTime date)
    {
        var today = DateTime.UtcNow.Date;
        if (date == today)
            return "Today";
        if (date == today.AddDays(-1))
            return "Yesterday";
        return date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Fayl attachment-i olan mesajlar üçün preview text qaytarır.
    /// [File] prefix-i əlavə edir.
    /// </summary>
    private static string GetFilePreview(FavoriteDirectMessageDto message)
    {
        if (!string.IsNullOrEmpty(message.FileId))
        {
            // Has file attachment
            return string.IsNullOrWhiteSpace(message.Content) ? "[File]" : $"[File] {message.Content}";
        }
        return message.Content;
    }

    /// <summary>
    /// Fayl attachment-i olan mesajlar üçün preview text qaytarır.
    /// [File] prefix-i əlavə edir.
    /// </summary>
    private static string GetFilePreview(FavoriteChannelMessageDto message)
    {
        if (!string.IsNullOrEmpty(message.FileId))
        {
            // Has file attachment
            return string.IsNullOrWhiteSpace(message.Content) ? "[File]" : $"[File] {message.Content}";
        }
        return message.Content;
    }

    #endregion

    #region Action Handlers

    /// <summary>
    /// Sound toggle handler.
    /// </summary>
    private async Task HandleSoundToggle(ChangeEventArgs e)
    {
        var isEnabled = (bool)(e.Value ?? false);
        await OnMuteToggle.InvokeAsync(!isEnabled);
    }

    /// <summary>
    /// Pin handler (placeholder).
    /// </summary>
    private void HandlePin()
    {
        showContextMenu = false;
        // TODO: Implement pin functionality
    }

    /// <summary>
    /// View profile handler - profile panel açır.
    /// </summary>
    private async Task HandleViewProfile()
    {
        showContextMenu = false;
        await OnViewProfile.InvokeAsync();
    }

    /// <summary>
    /// Find chat handler (placeholder).
    /// </summary>
    private void HandleFindChat()
    {
        showContextMenu = false;
        // TODO: Implement find chat functionality
    }

    /// <summary>
    /// Add members handler (placeholder).
    /// </summary>
    private void HandleAddMembers()
    {
        showContextMenu = false;
        // TODO: Implement add members functionality
    }

    /// <summary>
    /// Hide handler (placeholder).
    /// </summary>
    private void HandleHide()
    {
        showContextMenu = false;
        // TODO: Implement hide functionality
    }

    /// <summary>
    /// Leave handler (placeholder).
    /// </summary>
    private void HandleLeave()
    {
        showContextMenu = false;
        // TODO: Implement leave functionality
    }

    #endregion
}
