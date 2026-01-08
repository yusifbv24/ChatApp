using ChatApp.Blazor.Client.Models.Messages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MudBlazor;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class MessageBubble : IAsyncDisposable
{
    #region Injected Services

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    #endregion

    #region Image Lightbox State

    /// <summary>
    /// Şəkil lightbox açıqdır?
    /// </summary>
    private bool showImageLightbox = false;

    #endregion

    #region Parameters - Message Identity

    /// <summary>
    /// Mesajın unikal ID-si.
    /// </summary>
    [Parameter] public Guid MessageId { get; set; }

    /// <summary>
    /// Mesajın məzmunu.
    /// </summary>
    [Parameter] public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Mesaja əlavə edilmiş fayl ID-si (varsa).
    /// </summary>
    [Parameter] public string? FileId { get; set; }

    /// <summary>
    /// Fayl adı.
    /// </summary>
    [Parameter] public string? FileName { get; set; }

    /// <summary>
    /// Fayl content type (MIME type).
    /// </summary>
    [Parameter] public string? FileContentType { get; set; }

    /// <summary>
    /// Fayl ölçüsü (bytes).
    /// </summary>
    [Parameter] public long? FileSizeInBytes { get; set; }

    /// <summary>
    /// Göndərənin adı.
    /// </summary>
    [Parameter] public string SenderName { get; set; } = string.Empty;

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
    /// Reply edilən mesajın fayl ID-si.
    /// </summary>
    [Parameter] public string? ReplyToFileId { get; set; }

    /// <summary>
    /// Reply edilən mesajın fayl adı.
    /// </summary>
    [Parameter] public string? ReplyToFileName { get; set; }

    /// <summary>
    /// Reply edilən mesajın fayl content type-ı.
    /// </summary>
    [Parameter] public string? ReplyToFileContentType { get; set; }

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
    /// Chevron wrapper-ın DOM reference-i.
    /// Menu position hesablaması üçün (menu chevron-a nisbətən açılır).
    /// </summary>
    private ElementReference chevronWrapperRef;

    #endregion

    #region Private Fields - UI State

    private bool _disposed = false;

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
    /// Source-generated for better performance.
    /// </summary>
    [GeneratedRegex(@"(https?://[^\s<>""']+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    #endregion

    #region Private Fields - File State

    /// <summary>
    /// Fayl şəkildir? (ContentType-dan müəyyən olunur)
    /// </summary>
    private bool IsFileImage => !string.IsNullOrEmpty(FileContentType) && FileContentType.StartsWith("image/");

    /// <summary>
    /// Mesaj sadəcə fayldan ibarətdir? (content yoxdur)
    /// File-only mesajlar edit edilə bilməz.
    /// </summary>
    private bool HasFileOnly() => !string.IsNullOrEmpty(FileId) && string.IsNullOrWhiteSpace(Content);

    #endregion

    #region Computed Properties

    /// <summary>
    /// Bu mesaj "Read Later" işarəlidir?
    /// </summary>
    private bool IsMarkedAsLater =>
        LastReadLaterMessageId.HasValue && LastReadLaterMessageId.Value == MessageId;

    /// <summary>
    /// Fayl download URL-i (API base address ilə)
    /// </summary>
    private string FileDownloadUrl
    {
        get
        {
            if (string.IsNullOrEmpty(FileId))
                return string.Empty;

            var apiBaseAddress = Configuration["ApiBaseAddress"] ?? "http://localhost:7000";
            return $"{apiBaseAddress}/api/files/{FileId}/download";
        }
    }

    #endregion

    #region Formatting Methods

    /// <summary>
    /// Tarixi saat:dəqiqə formatına çevirir.
    /// </summary>
    private static string FormatTime(DateTime dateTime)
    {
        return dateTime.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);
    }


    /// <summary>
    /// Fayl icon-unu extension-a görə qaytarır.
    /// </summary>
    private string GetFileIcon()
    {
        return GetFileIcon(FileName);
    }

    /// <summary>
    /// Fayl tipi üçün Material icon qaytarır (parametrli overload).
    /// </summary>
    private string GetFileIcon(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return Icons.Material.Filled.InsertDriveFile;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => Icons.Material.Filled.PictureAsPdf,
            ".doc" or ".docx" => Icons.Material.Filled.Description,
            ".xls" or ".xlsx" => Icons.Material.Filled.TableChart,
            ".ppt" or ".pptx" => Icons.Material.Filled.Slideshow,
            ".zip" or ".rar" or ".7z" => Icons.Material.Filled.FolderZip,
            ".mp4" or ".avi" or ".mov" or ".mkv" => Icons.Material.Filled.VideoFile,
            ".mp3" or ".wav" or ".flac" => Icons.Material.Filled.AudioFile,
            ".txt" => Icons.Material.Filled.TextSnippet,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => Icons.Material.Filled.Image,
            _ => Icons.Material.Filled.InsertDriveFile
        };
    }

    /// <summary>
    /// Fayl type-ına görə CSS class qaytarır (icon rəngi üçün).
    /// </summary>
    private string GetFileIconClass()
    {
        if (string.IsNullOrEmpty(FileName))
            return string.Empty;

        var extension = Path.GetExtension(FileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "pdf",
            ".doc" or ".docx" => "word",
            ".xls" or ".xlsx" => "excel",
            ".ppt" or ".pptx" => "powerpoint",
            ".zip" or ".rar" or ".7z" => "archive",
            ".mp4" or ".avi" or ".mov" or ".mkv" => "video",
            ".mp3" or ".wav" or ".flac" => "audio",
            ".txt" => "text",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Fayl ölçüsünü formatlaşdırır (B, KB, MB, GB).
    /// </summary>
    private string FormatFileSize()
    {
        if (!FileSizeInBytes.HasValue)
            return "Unknown size";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = FileSizeInBytes.Value;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Fayl adını qısaldır (40 simvoldan uzun olarsa).
    /// Məsələn: "very-long-file-name-that-takes-space.pdf" → "very-long-file-name-that-...pdf"
    /// </summary>
    private string TruncateFileName(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "Unknown file";

        const int maxLength = 40;
        if (fileName.Length <= maxLength)
            return fileName;

        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Extension-u çıxarıb qalan yer hesablayırıq
        var availableLength = maxLength - extension.Length - 3; // 3 = "..." uzunluğu

        if (availableLength < 10)
            availableLength = 10; // Minimum 10 simvol göstər

        return $"{nameWithoutExtension.Substring(0, availableLength)}...{extension}";
    }

    /// <summary>
    /// Mətn içindəki URL-ləri klikləbilən linklərə çevirir.
    /// XSS hücumlarından qorunmaq üçün əvvəlcə HTML encode edilir və daha sonra özümüz html code yaradaraq digər səhifədə açılmasını təmin edirik.
    /// Noopener yazmazsaq açılan səhifə bizim səhifəyə geri müdaxilə edə bilər.
    /// </summary>
    private static string ParseLinks(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // XSS qorunması: əvvəlcə HTML encode
        var encoded = WebUtility.HtmlEncode(text);

        // URL-ləri anchor tag-larla əvəz et
        return UrlRegex().Replace(encoded, match =>
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

            // Calculate menu height dynamically based on visible items
            int itemCount = 0;
            itemCount++; // Reply - always visible
            itemCount++; // Copy - always visible
            if (IsOwn && !IsForwarded) itemCount++; // Edit - conditional
            itemCount++; // Forward - always visible
            if (!string.IsNullOrEmpty(FileId)) itemCount++; // Download - conditional (only with files)
            itemCount++; // More submenu - always visible
            if (IsOwn) itemCount++; // Delete - conditional
            itemCount++; // Select - always visible

            const int itemHeight = 42;
            int menuHeight = itemCount * itemHeight;

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

    /// <summary>
    /// Şəkil üzərinə klik edəndə lightbox aç.
    /// </summary>
    private void OpenImageLightbox()
    {
        showImageLightbox = true;
    }

    /// <summary>
    /// Lightbox-u bağla.
    /// </summary>
    private void CloseImageLightbox()
    {
        showImageLightbox = false;
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

        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }

    #endregion
}