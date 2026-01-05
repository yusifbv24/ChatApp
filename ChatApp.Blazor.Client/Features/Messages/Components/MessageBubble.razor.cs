using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ChatApp.Blazor.Client.Models.Messages;
using System.Text.RegularExpressions;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

/// <summary>
/// MessageBubble - Tək mesajı göstərən komponent.
///
/// Bu komponent aşağıdakı funksionallıqları təmin edir:
/// - Mesaj məzmununun göstərilməsi (text, link parsing)
/// - Reaction-lar və reaction picker
/// - Edit, Delete, Reply, Forward, Pin əməliyyatları
/// - Read status (DM və Channel üçün)
/// - Selection mode (multi-select)
/// - Deleted message view
/// - Reply preview
/// - Forwarded message indicator
///
/// Komponent partial class pattern istifadə edir:
/// - MessageBubble.razor: HTML template
/// - MessageBubble.razor.cs: C# code-behind (bu fayl)
/// </summary>
public partial class MessageBubble : IAsyncDisposable
{
    private bool _disposed = false;
    #region Injected Services

    [Inject] private IJSRuntime JS { get; set; } = default!;

    #endregion

    #region Parameters - Message Identity

    /// <summary>
    /// Mesajın unikal ID-si.
    /// </summary>
    [Parameter] public Guid MessageId { get; set; }

    /// <summary>
    /// Mesajın məzmunu.
    /// </summary>
    [Parameter] public string Content { get; set; } = "";

    /// <summary>
    /// Göndərənin adı.
    /// </summary>
    [Parameter] public string SenderName { get; set; } = "";

    /// <summary>
    /// Göndərənin avatar URL-i.
    /// </summary>
    [Parameter] public string? AvatarUrl { get; set; }

    /// <summary>
    /// Mesajın yaradılma tarixi (UTC).
    /// </summary>
    [Parameter] public DateTime CreatedAt { get; set; }

    #endregion

    #region Parameters - Message State

    /// <summary>
    /// Mesaj cari istifadəçiyə məxsusdurmu?
    /// </summary>
    [Parameter] public bool IsOwn { get; set; }

    /// <summary>
    /// Mesaj redaktə edilib?
    /// </summary>
    [Parameter] public bool IsEdited { get; set; }

    /// <summary>
    /// Mesaj silinib?
    /// </summary>
    [Parameter] public bool IsDeleted { get; set; }

    /// <summary>
    /// Mesaj oxunub? (DM üçün)
    /// </summary>
    [Parameter] public bool IsRead { get; set; }

    /// <summary>
    /// Mesajı oxuyan istifadəçi sayı (Channel üçün).
    /// </summary>
    [Parameter] public int ReadByCount { get; set; }

    /// <summary>
    /// Channel-da ümumi üzv sayı (sender xaric).
    /// </summary>
    [Parameter] public int TotalMemberCount { get; set; }

    /// <summary>
    /// Mesaj pinlənib?
    /// </summary>
    [Parameter] public bool IsPinned { get; set; }

    /// <summary>
    /// Mesaj favorite-ə əlavə edilib?
    /// </summary>
    [Parameter] public bool IsFavorite { get; set; }

    #endregion

    #region Parameters - Reactions

    /// <summary>
    /// Reaction sayı.
    /// </summary>
    [Parameter] public int ReactionCount { get; set; }

    /// <summary>
    /// Reaction-ların siyahısı.
    /// List of MessageReactionDto (DM) və ya List of ChannelMessageReactionDto (Channel).
    /// </summary>
    [Parameter] public object? Reactions { get; set; }

    /// <summary>
    /// Cari istifadəçinin ID-si (reaction ownership üçün).
    /// </summary>
    [Parameter] public Guid? CurrentUserId { get; set; }

    #endregion

    #region Parameters - Display Options

    /// <summary>
    /// Avatar göstərilsin?
    /// </summary>
    [Parameter] public bool ShowAvatar { get; set; }

    /// <summary>
    /// Sender adı göstərilsin?
    /// </summary>
    [Parameter] public bool ShowSenderName { get; set; }

    /// <summary>
    /// Direct Message-dir? (false = Channel)
    /// </summary>
    [Parameter] public bool IsDirectMessage { get; set; }

    #endregion

    #region Parameters - Reply & Forward

    /// <summary>
    /// Reply edilən mesajın ID-si.
    /// </summary>
    [Parameter] public Guid? ReplyToMessageId { get; set; }

    /// <summary>
    /// Reply edilən mesajın məzmunu.
    /// </summary>
    [Parameter] public string? ReplyToContent { get; set; }

    /// <summary>
    /// Reply edilən mesajın göndərəninin adı.
    /// </summary>
    [Parameter] public string? ReplyToSenderName { get; set; }

    /// <summary>
    /// Mesaj forward edilib?
    /// </summary>
    [Parameter] public bool IsForwarded { get; set; }

    #endregion

    #region Parameters - Read Later

    /// <summary>
    /// "Read Later" işarəli son mesajın ID-si.
    /// </summary>
    [Parameter] public Guid? LastReadLaterMessageId { get; set; }

    #endregion

    #region Parameters - Selection Mode

    /// <summary>
    /// Selection modunda?
    /// </summary>
    [Parameter] public bool IsSelectMode { get; set; }

    /// <summary>
    /// Bu mesaj seçilib?
    /// </summary>
    [Parameter] public bool IsSelected { get; set; }

    #endregion

    #region Parameters - Event Callbacks

    /// <summary>
    /// Edit callback-i.
    /// </summary>
    [Parameter] public EventCallback OnEdit { get; set; }

    /// <summary>
    /// Delete callback-i.
    /// </summary>
    [Parameter] public EventCallback OnDelete { get; set; }

    /// <summary>
    /// Reaction əlavə/silmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<string> OnReaction { get; set; }

    /// <summary>
    /// Reply callback-i.
    /// </summary>
    [Parameter] public EventCallback OnReply { get; set; }

    /// <summary>
    /// Pin/Unpin callback-i.
    /// </summary>
    [Parameter] public EventCallback OnPin { get; set; }

    /// <summary>
    /// Forward callback-i.
    /// </summary>
    [Parameter] public EventCallback OnForward { get; set; }

    /// <summary>
    /// Reply preview click callback-i (mesaja scroll).
    /// </summary>
    [Parameter] public EventCallback<Guid> OnReplyClick { get; set; }

    /// <summary>
    /// Action tamamlandı callback-i (refocus üçün).
    /// </summary>
    [Parameter] public EventCallback OnActionCompleted { get; set; }

    /// <summary>
    /// Scroll to bottom callback-i.
    /// </summary>
    [Parameter] public EventCallback ScrollToBottom { get; set; }

    /// <summary>
    /// "Mark as Later" callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnMarkAsLater { get; set; }

    /// <summary>
    /// Selection toggle callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnSelectToggle { get; set; }

    /// <summary>
    /// Favorite toggle callback-i.
    /// </summary>
    [Parameter] public EventCallback OnToggleFavorite { get; set; }

    #endregion

    #region Private Fields - Element References

    /// <summary>
    /// Mesaj wrapper-ın DOM reference-i.
    /// Menu position hesablaması üçün.
    /// </summary>
    private ElementReference messageWrapperRef;

    /// <summary>
    /// Chevron wrapper-ın DOM reference-i.
    /// Menu position hesablaması üçün (menu chevron-a nisbətən açılır).
    /// </summary>
    private ElementReference chevronWrapperRef;

    #endregion

    #region Private Fields - UI State

    /// <summary>
    /// Reaction picker görünürmü?
    /// </summary>
    private bool showReactionPicker = false;

    /// <summary>
    /// More menu görünürmü?
    /// </summary>
    private bool showMoreMenu = false;

    /// <summary>
    /// Hover actions görünürmü? (chevron, reaction icon)
    /// </summary>
    private bool showHoverActions = false;

    /// <summary>
    /// Menu yuxarıda açılsın? (ekranda yer olmadıqda)
    /// </summary>
    private bool menuPositionAbove = false;

    /// <summary>
    /// More submenu görünürmü?
    /// </summary>
    private bool showMoreSubmenu = false;

    /// <summary>
    /// Hovered reaction-ın index-i (user panel üçün).
    /// </summary>
    private int? hoveredReactionIndex = null;

    #endregion

    #region Private Fields - Cancellation Tokens

    /// <summary>
    /// Reaction panel hide delay üçün CancellationToken.
    /// </summary>
    private CancellationTokenSource? hideReactionPanelCts;

    /// <summary>
    /// Reaction picker show delay üçün CancellationToken.
    /// </summary>
    private CancellationTokenSource? showReactionPickerCts;

    #endregion

    #region Private Fields - Constants

    /// <summary>
    /// Ən çox istifadə edilən reaction-lar.
    /// </summary>
    private readonly string[] CommonReactions = { "👍", "❤️", "😂", "😮", "😢", "🎉" };

    /// <summary>
    /// URL regex pattern - link parsing üçün.
    /// </summary>
    private static readonly Regex UrlRegex = new(
        @"(https?://[^\s<>""']+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #endregion

    #region Computed Properties

    /// <summary>
    /// Bu mesaj "Read Later" işarəlidir?
    /// </summary>
    private bool IsMarkedAsLater =>
        LastReadLaterMessageId.HasValue && LastReadLaterMessageId.Value == MessageId;

    #endregion

    #region Formatting Methods

    /// <summary>
    /// Tarixi saat:dəqiqə formatına çevirir.
    /// </summary>
    private string FormatTime(DateTime dateTime)
    {
        return dateTime.ToLocalTime().ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Mətni qısaldır (ellipsis ilə).
    /// </summary>
    private string TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Mətn içindəki URL-ləri klikləbilən linklərə çevirir.
    /// XSS hücumlarından qorunmaq üçün əvvəlcə HTML encode edilir.
    /// </summary>
    private string ParseLinks(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // XSS qorunması: əvvəlcə HTML encode
        var encoded = System.Net.WebUtility.HtmlEncode(text);

        // URL-ləri anchor tag-larla əvəz et
        return UrlRegex.Replace(encoded, match =>
        {
            var url = match.Value;
            return $"<a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"message-link\">{url}</a>";
        });
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// Reaction siyahısını qaytarır.
    /// Həm DM (MessageReactionDto) həm də Channel (ChannelMessageReactionDto) üçün işləyir.
    /// </summary>
    private List<dynamic>? GetReactionsList()
    {
        if (Reactions == null) return null;

        if (Reactions is List<MessageReactionDto> directReactions)
            return directReactions.Cast<dynamic>().ToList();

        if (Reactions is List<ChannelMessageReactionDto> channelReactions)
            return channelReactions.Cast<dynamic>().ToList();

        return null;
    }

    /// <summary>
    /// Reaction tooltip text-ini qaytarır.
    /// </summary>
    private string GetReactionTooltip(dynamic reaction)
    {
        string emoji = reaction.Emoji;
        int count = reaction.Count;

        return count == 1 ? $"{emoji} 1 person" : $"{emoji} {count} people";
    }

    /// <summary>
    /// Emoji reaction seçir/toggle edir.
    /// </summary>
    private async Task SelectReaction(string emoji)
    {
        showReactionPickerCts?.Cancel();
        showReactionPicker = false;
        await OnReaction.InvokeAsync(emoji);
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Like reaction toggle edir.
    /// </summary>
    private async Task ToggleLikeReaction()
    {
        showReactionPickerCts?.Cancel();
        showReactionPicker = false;
        await OnReaction.InvokeAsync("👍");
        await OnActionCompleted.InvokeAsync();
    }

    #endregion

    #region Reaction Hover Methods

    /// <summary>
    /// Reaction icon-a hover olduqda picker-i açır (delay ilə).
    /// </summary>
    private async Task HandleReactionIconHover()
    {
        showReactionPickerCts?.Cancel();
        showReactionPickerCts = new CancellationTokenSource();
        var token = showReactionPickerCts.Token;

        try
        {
            await Task.Delay(250, token);

            if (!token.IsCancellationRequested)
            {
                showReactionPicker = true;
                showMoreMenu = false;
                StateHasChanged();
            }
        }
        catch (TaskCanceledException)
        {
            // Hover-dan tez çıxdıqda expected
        }
    }

    /// <summary>
    /// Reaction icon-dan çıxdıqda picker-i bağlayır (delay ilə).
    /// </summary>
    private async Task CancelReactionIconHover()
    {
        showReactionPickerCts?.Cancel();
        showReactionPickerCts = new CancellationTokenSource();
        var token = showReactionPickerCts.Token;

        try
        {
            await Task.Delay(200, token);

            if (!token.IsCancellationRequested)
            {
                showReactionPicker = false;
                StateHasChanged();
            }
        }
        catch (TaskCanceledException) { }
    }

    /// <summary>
    /// Reaction picker açıq saxlayır (picker üzərinə hover olduqda).
    /// </summary>
    private Task KeepReactionPickerOpen()
    {
        showReactionPickerCts?.Cancel();
        showReactionPickerCts = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reaction picker-dan çıxdıqda bağlayır.
    /// </summary>
    private async Task HandleReactionPickerLeave()
    {
        showReactionPickerCts?.Cancel();
        showReactionPickerCts = new CancellationTokenSource();
        var token = showReactionPickerCts.Token;

        try
        {
            await Task.Delay(200, token);

            if (!token.IsCancellationRequested)
            {
                showReactionPicker = false;
                StateHasChanged();
            }
        }
        catch (TaskCanceledException) { }
    }

    #endregion

    #region Reaction User Panel Methods (Channel)

    /// <summary>
    /// Reaction user panel-i göstərir.
    /// </summary>
    private Task ShowReactionUsers(int index)
    {
        hideReactionPanelCts?.Cancel();
        hideReactionPanelCts = null;
        hoveredReactionIndex = index;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reaction user panel-i gizlətməyi planlaşdırır (delay ilə).
    /// </summary>
    private async Task ScheduleHideReactionUsers()
    {
        hideReactionPanelCts?.Cancel();
        hideReactionPanelCts = new CancellationTokenSource();
        var token = hideReactionPanelCts.Token;

        try
        {
            await Task.Delay(300, token);

            if (!token.IsCancellationRequested)
            {
                hoveredReactionIndex = null;
                StateHasChanged();
            }
        }
        catch (TaskCanceledException)
        {
            // Panel üzərinə hover olduqda expected
        }
    }

    /// <summary>
    /// Reaction user panel gizlətməsini ləğv edir.
    /// </summary>
    private Task CancelHideReactionUsers()
    {
        hideReactionPanelCts?.Cancel();
        hideReactionPanelCts = null;
        return Task.CompletedTask;
    }

    #endregion

    #region More Menu Methods

    /// <summary>
    /// More menu-nu toggle edir.
    /// </summary>
    private async Task ToggleMoreMenu()
    {
        if (!showMoreMenu)
        {
            await CheckMenuPosition();
        }
        showMoreMenu = !showMoreMenu;
        showReactionPicker = false;
    }

    /// <summary>
    /// Menu-nun yuxarıda və ya aşağıda açılmasını müəyyən edir.
    /// </summary>
    private async Task CheckMenuPosition()
    {
        try
        {
            var position = await JS.InvokeAsync<MenuPositionInfo>("chatAppUtils.getElementPosition", chevronWrapperRef);
            if (position == null)
            {
                menuPositionAbove = false;
                return;
            }

            const int menuHeight = 420; // 9 items × 42px + padding

            // Open above if more space above, otherwise below
            menuPositionAbove = position.ActualSpaceBelow < menuHeight
                && position.ActualSpaceAbove > position.ActualSpaceBelow;
        }
        catch
        {
            menuPositionAbove = false;
        }
    }

    /// <summary>
    /// More menu-nu bağlayır.
    /// </summary>
    private void CloseMoreMenu()
    {
        showMoreMenu = false;
        showMoreSubmenu = false;
    }

    /// <summary>
    /// More submenu-nu göstərir.
    /// </summary>
    private void ShowMoreSubmenu() => showMoreSubmenu = true;

    /// <summary>
    /// More submenu-nu gizlədir.
    /// </summary>
    private void HideMoreSubmenu() => showMoreSubmenu = false;

    #endregion

    #region Action Handlers

    /// <summary>
    /// Edit click handler.
    /// </summary>
    private async Task OnEditClick()
    {
        CloseMoreMenu();
        await OnEdit.InvokeAsync();
        await OnActionCompleted.InvokeAsync();
        await ScrollToBottom.InvokeAsync();
    }

    /// <summary>
    /// Delete click handler.
    /// </summary>
    private async Task OnDeleteClick()
    {
        CloseMoreMenu();
        await OnDelete.InvokeAsync();
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Reply button click handler (menu-dan).
    /// </summary>
    private async Task HandleReplyButtonClick()
    {
        CloseMoreMenu();
        await OnReply.InvokeAsync();
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Copy click handler.
    /// </summary>
    private async Task OnCopyClick()
    {
        CloseMoreMenu();
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", Content);
        }
        catch
        {
            // Clipboard errors - ignore
        }
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Forward click handler.
    /// </summary>
    private async Task OnForwardClick()
    {
        CloseMoreMenu();
        await OnForward.InvokeAsync();
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Pin/Unpin click handler.
    /// </summary>
    private async Task OnPinClick()
    {
        CloseMoreMenu();
        await OnPin.InvokeAsync();
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Reply preview click handler - replied mesaja scroll edir.
    /// </summary>
    private async Task HandleReplyClick()
    {
        if (ReplyToMessageId.HasValue)
        {
            await OnReplyClick.InvokeAsync(ReplyToMessageId.Value);
        }
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Favorite toggle click handler.
    /// </summary>
    private async Task HandleToggleFavoriteClick()
    {
        CloseMoreMenu();
        await OnToggleFavorite.InvokeAsync();
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Mark as Later click handler.
    /// </summary>
    private async Task HandleMarkAsLaterClick()
    {
        CloseMoreMenu();
        await OnMarkAsLater.InvokeAsync(MessageId);
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Select click handler (selection mode-a keçid).
    /// </summary>
    private async Task HandleSelectClick()
    {
        CloseMoreMenu();
        await OnSelectToggle.InvokeAsync(MessageId);
        await OnActionCompleted.InvokeAsync();
    }

    /// <summary>
    /// Bubble click handler (selection mode-da toggle).
    /// </summary>
    private async Task HandleBubbleClick()
    {
        if (IsSelectMode && !IsDeleted)
        {
            await OnSelectToggle.InvokeAsync(MessageId);
        }
    }

    #endregion

    #region Helper Types

    /// <summary>
    /// Menu position hesablaması üçün JS-dən gələn məlumat.
    /// </summary>
    private record MenuPositionInfo
    {
        public double ActualSpaceBelow { get; set; }
        public double ActualSpaceAbove { get; set; }
    }

    #endregion

    #region IAsyncDisposable

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        hideReactionPanelCts?.Cancel();
        hideReactionPanelCts?.Dispose();
        hideReactionPanelCts = null;

        showReactionPickerCts?.Cancel();
        showReactionPickerCts?.Dispose();
        showReactionPickerCts = null;

        return ValueTask.CompletedTask;
    }

    #endregion
}
