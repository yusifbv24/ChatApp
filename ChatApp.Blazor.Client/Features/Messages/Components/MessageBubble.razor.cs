using ChatApp.Blazor.Client.Models.Messages;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ChatApp.Shared.Kernel;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class MessageBubble : IAsyncDisposable
{
    #region Injected Services

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;

    #endregion

    #region Image Lightbox State

    private bool showImageLightbox = false;

    #endregion

    #region Link Preview State

    private LinkPreviewData? _linkPreview;
    private bool _linkPreviewLoaded;
    private string? _previousContent;

    private record LinkPreviewData(string? Url, string? Title, string? Description, string? ImageUrl, string? Domain);

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
    /// Statik fayl URL-i (API call əvəzinə birbaşa file server-dən).
    /// DTO-dan gəlir: "/uploads/files/userId/filename.jpg"
    /// </summary>
    [Parameter] public string? FileUrl { get; set; }

    /// <summary>
    /// Thumbnail URL-i (şəkillər üçün).
    /// DTO-dan gəlir: "/uploads/files/userId/thumb_filename.jpg"
    /// </summary>
    [Parameter] public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Göndərənin adı.
    /// </summary>
    [Parameter] public string SenderName { get; set; } = string.Empty;


    [Parameter] public Guid SenderId { get; set; }

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
    /// İstifadəçinin mesaj redaktə etmə icazəsi var?
    /// </summary>
    [Parameter] public bool CanEditMessage { get; set; }

    /// <summary>
    /// İstifadəçinin mesaj silmə icazəsi var?
    /// </summary>
    [Parameter] public bool CanDeleteMessage { get; set; }

    /// <summary>
    /// İstifadəçinin fayl yükləmə (download) icazəsi var?
    /// </summary>
    [Parameter] public bool CanDownloadFile { get; set; }

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

    /// <summary>
    /// Mesajın statusu (Optimistic UI üçün).
    /// Pending: Göndərilir, Sent: Göndərildi, Delivered: Çatdırıldı, Read: Oxundu, Failed: Uğursuz
    /// </summary>
    [Parameter] public MessageStatus Status { get; set; } = MessageStatus.Sent;

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

    #region Parameters - Mentions

    /// <summary>
    /// Mesajda mention edilən istifadəçilər.
    /// </summary>
    [Parameter] public object? Mentions { get; set; } // List<MessageMentionDto> or List<ChannelMessageMentionDto>

    /// <summary>
    /// Mention-a klik edildikdə trigger edilən callback (userId ötürülür).
    /// </summary>
    [Parameter] public EventCallback<Guid> OnMentionClick { get; set; }

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

    /// <summary>
    /// Cancel upload callback-i (file upload ləğv ediləndə).
    /// </summary>
    [Parameter] public EventCallback<Guid> OnCancelUpload { get; set; }

    #endregion

    #region Parameters - File Upload State

    /// <summary>
    /// Fayl upload state-i (Pending, Uploading, Completed, Failed, Cancelled).
    /// </summary>
    [Parameter] public Models.Files.UploadState? FileUploadState { get; set; }

    /// <summary>
    /// Fayl upload progress (0-100).
    /// </summary>
    [Parameter] public int FileUploadProgress { get; set; }

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

    /// <summary>
    /// DotNetObjectReference for message menu outside click detection.
    /// </summary>
    private DotNetObjectReference<MessageBubble>? _messageMenuRef;

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

    /// <summary>
    /// Content-dən ilk URL-i çıxar. Edit zamanı URL dəyişikliyini aşkarlamaq üçün.
    /// </summary>
    private static string? ExtractFirstUrl(string? content)
    {
        if (string.IsNullOrEmpty(content)) return null;
        var match = UrlRegex().Match(content);
        return match.Success ? match.Value : null;
    }

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
    /// Fayl download URL-i.
    /// Əgər FileUrl (statik URL) varsa onu istifadə edir (PERFORMANS).
    /// Əks halda fallback olaraq API endpoint istifadə edir.
    /// </summary>
    private string FileDownloadUrl
    {
        get
        {
            // Əvvəlcə statik URL-i yoxla (yeni performans yanaşması)
            if (!string.IsNullOrEmpty(FileUrl))
            {
                return GetFullUrl(FileUrl);
            }

            // Fallback: köhnə API endpoint (legacy support)
            if (string.IsNullOrEmpty(FileId))
                return string.Empty;

            var baseAddress = Configuration["ApiBaseAddress"] ?? "http://localhost:7000";
            return $"{baseAddress}/api/files/{FileId}/download";
        }
    }

    /// <summary>
    /// Download üçün API endpoint URL-i.
    /// Statik URL deyil - CORS + Content-Disposition: attachment dəstəyi üçün.
    /// </summary>
    private string ApiDownloadUrl
    {
        get
        {
            if (string.IsNullOrEmpty(FileId))
                return string.Empty;

            var baseAddress = Configuration["ApiBaseAddress"] ?? "http://localhost:7000";
            return $"{baseAddress}/api/files/{FileId}/download";
        }
    }

    /// <summary>
    /// JS interop ilə faylı download edir.
    /// </summary>
    private async Task DownloadFileAsync()
    {
        var url = ApiDownloadUrl;
        if (!string.IsNullOrEmpty(url))
        {
            await JS.InvokeVoidAsync("chatAppUtils.triggerFileDownload", url, FileName);
        }
    }

    /// <summary>
    /// Relative URL-i full URL-ə çevirir (API base address ilə).
    /// </summary>
    private string GetFullUrl(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return string.Empty;

        var apiBaseAddress = Configuration["ApiBaseAddress"] ?? "http://localhost:7000";
        return $"{apiBaseAddress}{relativeUrl}";
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
    /// Mətn içindəki URL-ləri və mention-ları parse edir.
    /// XSS qorunması üçün əvvəlcə HTML encode edilir.
    /// @ simvolu olmadan yalnız ad ilə mention-ları rəngli göstərir.
    /// </summary>
    private string ParseLinks(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // XSS qorunması: əvvəlcə HTML encode
        var encoded = WebUtility.HtmlEncode(text);

        // Mentions field-indən mention edilmiş user adlarını al
        var mentionNames = new Dictionary<string, Guid>(); // FullName -> UserId

        // DirectMessage və ChannelMessage fərqli mention type-ları var
        if (Mentions != null)
        {
            // Try parse as DirectMessage mentions
            if (Mentions is List<MessageMentionDto> dmMentions && dmMentions.Count > 0)
            {
                foreach (var m in dmMentions)
                {
                    mentionNames[m.UserFullName] = m.UserId;
                }
            }

            // Try parse as ChannelMessage mentions
            if (Mentions is List<ChannelMessageMentionDto> channelMentions && channelMentions.Count > 0)
            {
                foreach (var m in channelMentions)
                {
                    if (m.UserId.HasValue)
                    {
                        mentionNames[m.UserFullName] = m.UserId.Value;
                    }
                    else
                    {
                        // @All mention (UserId = null)
                        // Guid.Empty istifadə edirik ki, render olunsun, lakin klik disabled olsun
                        mentionNames[m.UserFullName] = Guid.Empty;
                    }
                }
            }
        }

        // Mention-ları parse et (@ simvolu OLMADAN, yalnız ad rəngli və clickable)
        foreach (var mention in mentionNames)
        {
            // Exact word match - case insensitive
            var pattern = $@"\b({Regex.Escape(mention.Key)})\b";

            // @All üçün xüsusi stil (cursor default, klik disabled)
            var cursorStyle = mention.Value == Guid.Empty ? "default" : "pointer";
            var clickableClass = mention.Value == Guid.Empty ? "message-mention mention-all" : "message-mention";

            encoded = Regex.Replace(
                encoded,
                pattern,
                match => $"<span class=\"{clickableClass}\" data-userid=\"{mention.Value}\" data-fullname=\"{mention.Key}\" style=\"cursor: {cursorStyle};\">{mention.Key}</span>",
                RegexOptions.IgnoreCase);
        }

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
        showReactionPickerCts?.Dispose();
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
        showReactionPickerCts?.Dispose();
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
        showReactionPickerCts?.Dispose();
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
        hideReactionPanelCts?.Dispose();
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

            // Setup outside click detection when opening menu
            // JS will automatically close all other open menus
            try
            {
                if (_messageMenuRef == null)
                {
                    _messageMenuRef = DotNetObjectReference.Create(this);
                }
                await JS.InvokeVoidAsync("setupMessageMenuOutsideClickHandler", MessageId, _messageMenuRef);
            }
            catch
            {
                // Silently handle JS interop errors
            }
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
    /// Scroll version - scroll detect olunduqda dəyişir.
    /// </summary>
    [Parameter] public int ScrollVersion { get; set; }

    private int _previousScrollVersion = 0;

    /// <summary>
    /// OnScrollDetected callback handler - scroll edərkən menu bağlamaq üçün.
    /// </summary>
    protected override void OnParametersSet()
    {
        // FIX: Close menu when scroll is detected (ScrollVersion changed)
        if (ScrollVersion != _previousScrollVersion)
        {
            _previousScrollVersion = ScrollVersion;
            if (showMoreMenu)
            {
                showMoreMenu = false;
                showMoreSubmenu = false;
            }
        }

        // Content dəyişdikdə (edit) link preview-u yenidən yoxla
        if (_previousContent != null && _previousContent != Content)
        {
            var oldUrl = ExtractFirstUrl(_previousContent);
            var newUrl = ExtractFirstUrl(Content);

            if (oldUrl != newUrl)
            {
                // URL dəyişdi və ya silindi — link preview-u ləğv et və yenidən yüklə
                _linkPreview = null;
                _linkPreviewLoaded = false;
            }
        }
        _previousContent = Content;
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
    /// Cancel upload click handler (Bitrix24 style cancel button).
    /// </summary>
    private async Task HandleCancelUpload()
    {
        if (OnCancelUpload.HasDelegate)
        {
            await OnCancelUpload.InvokeAsync(MessageId);
        }
    }

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

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        hideReactionPanelCts?.Cancel();
        hideReactionPanelCts?.Dispose();
        hideReactionPanelCts = null;

        showReactionPickerCts?.Cancel();
        showReactionPickerCts?.Dispose();
        showReactionPickerCts = null;

        // FIX: Always dispose DotNetObjectReference even if JS call fails
        if (_dotNetHelper != null)
        {
            try
            {
                await JS.InvokeVoidAsync("window.disposeMentionClickHandlers");
            }
            catch
            {
                // Ignore JS disposal errors
            }
            finally
            {
                _dotNetHelper.Dispose();
                _dotNetHelper = null;
            }
        }

        // Dispose message menu outside click handler
        // FIX: Always dispose DotNetObjectReference even if JS call fails
        if (_messageMenuRef != null)
        {
            try
            {
                await JS.InvokeVoidAsync("disposeMessageMenuOutsideClickHandler", MessageId);
            }
            catch
            {
                // Ignore JS disposal errors
            }
            finally
            {
                _messageMenuRef.Dispose();
                _messageMenuRef = null;
            }
        }

        GC.SuppressFinalize(this);
    }

    #endregion

    #region Lifecycle Methods

    private DotNetObjectReference<MessageBubble>? _dotNetHelper;

    /// <summary>
    /// Component render olduqdan sonra mention-lara click event listener əlavə edir.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _dotNetHelper = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("window.initializeMentionClickHandlers", _dotNetHelper);
            }
            catch
            {
                // Silently handle initialization errors
            }
        }

        // Link preview yüklə (firstRender və ya edit sonrası _linkPreviewLoaded reset olduqda)
        if (!_linkPreviewLoaded)
        {
            await LoadLinkPreviewAsync();
        }
    }

    private async Task LoadLinkPreviewAsync()
    {
        if (_linkPreviewLoaded || IsDeleted || string.IsNullOrEmpty(Content))
            return;

        _linkPreviewLoaded = true;

        var match = UrlRegex().Match(Content);
        if (!match.Success)
            return;

        // Frontend URL validasiyası — etibarsız host-ları backend-ə göndərmə
        var url = match.Value;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https") ||
            !uri.Host.Contains('.') || uri.Host.Length < 4 ||
            uri.Host.EndsWith('.'))
            return;

        // TLD minimum 2 simvol olmalıdır (example.com yox, 166.a yox)
        var lastDot = uri.Host.LastIndexOf('.');
        if (lastDot >= 0 && uri.Host.Length - lastDot - 1 < 2)
            return;

        try
        {
            var response = await Http.GetAsync($"api/files/link-preview?url={Uri.EscapeDataString(url)}");
            if (response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                _linkPreview = await response.Content.ReadFromJsonAsync<LinkPreviewData>();
                StateHasChanged();

                // Link preview yükləndikdə, yalnız istifadəçi aşağıdadırsa scroll et
                try { await JS.InvokeVoidAsync("chatAppUtils.scrollToBottomIfNear", "chat-messages"); }
                catch { /* non-critical */ }
            }
        }
        catch
        {
            // Link preview is non-critical
        }
    }

    /// <summary>
    /// JS-dən çağrılan metod - mention-a klik edildikdə.
    /// @All mention (Guid.Empty) ignore edilir.
    /// </summary>
    [JSInvokable]
    public async Task HandleMentionClickFromJS(string userIdStr)
    {
        if (Guid.TryParse(userIdStr, out var userId))
        {
            // @All mention-u ignore et (Guid.Empty)
            if (userId == Guid.Empty)
                return;

            await OnMentionClick.InvokeAsync(userId);
        }
    }

    /// <summary>
    /// JS callback - called when clicking outside message more menu.
    /// </summary>
    [JSInvokable]
    public void OnMessageMenuOutsideClick()
    {
        if (showMoreMenu)
        {
            CloseMoreMenu();
            StateHasChanged();
        }
    }

    #endregion

    #region Render Optimization

    /// <summary>
    /// Render optimization - yalnız dəyişiklik olanda render et.
    /// Reactions və ReadBy list dəyişiklikləri istisna (her zaman render).
    /// </summary>
    protected override bool ShouldRender()
    {
        // Disposed olubsa render etmə
        if (_disposed) return false;

        // Default: render et (Blazor öz məntiqini işlətsin)
        return true;
    }

    #endregion
}