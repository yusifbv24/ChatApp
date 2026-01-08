using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using ChatApp.Blazor.Client.State;
using ChatApp.Blazor.Client.Models.Files;
using MudBlazor;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class MessageInput : IDisposable
{
    #region Injected Services

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private UserState UserState { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    #endregion

    #region Parameters - Basic

    /// <summary>
    /// Input placeholder texti.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Mesaj göndərilir?
    /// </summary>
    [Parameter] public bool IsSending { get; set; }

    /// <summary>
    /// Conversation ID-si (draft tracking üçün).
    /// </summary>
    [Parameter] public Guid? ConversationId { get; set; }

    #endregion

    #region Parameters - Edit Mode

    /// <summary>
    /// Edit modunda?
    /// </summary>
    [Parameter] public bool IsEditing { get; set; }

    /// <summary>
    /// Redaktə edilən mesajın məzmunu.
    /// </summary>
    [Parameter] public string? EditingContent { get; set; }

    #endregion

    #region Parameters - Reply Mode

    /// <summary>
    /// Reply modunda?
    /// </summary>
    [Parameter] public bool IsReplying { get; set; }

    /// <summary>
    /// Reply edilən mesajın göndərəninin adı.
    /// </summary>
    [Parameter] public string? ReplyToSenderName { get; set; }

    /// <summary>
    /// Reply edilən mesajın məzmunu.
    /// </summary>
    [Parameter] public string? ReplyToContent { get; set; }

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

    #endregion

    #region Parameters - Draft Support

    /// <summary>
    /// İlkin draft məzmunu.
    /// </summary>
    [Parameter] public string? InitialDraft { get; set; }

    #endregion

    #region Parameters - Event Callbacks

    /// <summary>
    /// Mesaj göndərmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<string> OnSend { get; set; }

    /// <summary>
    /// Mesaj redaktə callback-i.
    /// </summary>
    [Parameter] public EventCallback<string> OnEdit { get; set; }

    /// <summary>
    /// Edit ləğv etmə callback-i.
    /// </summary>
    [Parameter] public EventCallback OnCancelEdit { get; set; }

    /// <summary>
    /// Reply ləğv etmə callback-i.
    /// </summary>
    [Parameter] public EventCallback OnCancelReply { get; set; }

    /// <summary>
    /// Typing indicator callback-i.
    /// </summary>
    [Parameter] public EventCallback<bool> OnTyping { get; set; }

    /// <summary>
    /// File attach callback-i (legacy - deprecated).
    /// </summary>
    [Parameter] public EventCallback OnAttach { get; set; }

    /// <summary>
    /// Fayllarla mesaj göndərmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<(List<SelectedFile> Files, string Message)> OnSendWithFiles { get; set; }

    /// <summary>
    /// Draft dəyişikliyi callback-i.
    /// </summary>
    [Parameter] public EventCallback<string> OnDraftChanged { get; set; }

    #endregion

    #region Private Fields - Constants

    /// <summary>
    /// Maksimum simvol sayı.
    /// </summary>
    private const int MaxLength = 4000;

    /// <summary>
    /// Ümumi emoji-lər.
    /// </summary>
    private readonly string[] CommonEmojis = {
        "😀", "😃", "😄", "😁", "😅", "😂", "🤣", "😊",
        "😇", "🙂", "🙃", "😉", "😌", "😍", "🥰", "😘",
        "😗", "😙", "😚", "😋", "😛", "😜", "🤪", "😝",
        "🤑", "🤗", "🤭", "🤫", "🤔", "🤐", "🤨", "😐",
        "👍", "👎", "👌", "✌️", "🤞", "🤟", "🤘", "🤙",
        "👏", "🙌", "👐", "🤲", "🤝", "🙏", "❤️", "🧡",
        "💛", "💚", "💙", "💜", "🖤", "💔", "💕", "💞",
        "🎉", "🎊", "🎁", "🔥", "⭐", "✨", "💯", "💪"
    };

    #endregion

    #region Private Fields - Element References

    /// <summary>
    /// Textarea DOM reference.
    /// </summary>
    private ElementReference textAreaRef;

    /// <summary>
    /// File input reference.
    /// </summary>
    private InputFile fileInputRef = default!;

    #endregion

    #region Private Fields - UI State

    /// <summary>
    /// Mesaj mətni.
    /// </summary>
    private string MessageText { get; set; } = string.Empty;

    /// <summary>
    /// Emoji picker görünürmü?
    /// </summary>
    private bool showEmojiPicker = false;

    /// <summary>
    /// Typing indicator göndərilib?
    /// </summary>
    private bool isTyping = false;

    /// <summary>
    /// Textarea-ya focus lazımdır?
    /// </summary>
    private bool shouldFocus = false;

    /// <summary>
    /// File selection panel görünürmü?
    /// </summary>
    private bool showFileSelectionPanel = false;

    /// <summary>
    /// Seçilmiş fayllar.
    /// </summary>
    private List<SelectedFile> selectedFiles = new();

    #endregion

    #region Private Fields - Tracking

    /// <summary>
    /// Typing timer (2 saniyə sonra typing stop).
    /// </summary>
    private System.Timers.Timer? typingTimer;

    /// <summary>
    /// Əvvəlki conversation ID.
    /// </summary>
    private Guid? previousConversationId;

    /// <summary>
    /// Əvvəl edit modunda idi?
    /// </summary>
    private bool wasEditing = false;

    /// <summary>
    /// Əvvəl reply modunda idi?
    /// </summary>
    private bool wasReplying = false;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Mesaj göndərmək mümkündür?
    /// </summary>
    private bool CanSend =>
        !string.IsNullOrWhiteSpace(MessageText) &&
        !IsSending &&
        MessageText.Length <= MaxLength;

    /// <summary>
    /// Send button disabled?
    /// </summary>
    private bool SendButtonDisabled => !CanSend || IsSending;

    /// <summary>
    /// Limit yaxınlaşır? (3500+)
    /// </summary>
    private bool IsNearLimit => MessageText.Length >= 3500 && MessageText.Length < MaxLength;

    /// <summary>
    /// Limitdədir?
    /// </summary>
    private bool IsAtLimit => MessageText.Length >= MaxLength;

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Komponentin ilk yüklənməsi.
    /// </summary>
    protected override void OnInitialized()
    {
        typingTimer = new System.Timers.Timer(2000);
        typingTimer.Elapsed += async (s, e) => await StopTyping();
        typingTimer.AutoReset = false;
    }

    /// <summary>
    /// Parameter dəyişiklikləri.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        // Edit mode başladıqda content-i set et
        if (IsEditing && !wasEditing && !string.IsNullOrEmpty(EditingContent))
        {
            MessageText = EditingContent;
            wasEditing = true;
            shouldFocus = true;
        }

        else if (wasEditing && !IsEditing)
        {
            // Edit ləğv edildi/tamamlandı
            MessageText = string.Empty;
            wasEditing = false;
            await ResetTextareaHeight();
        }

        // Reply mode başladıqda focus et
        if (IsReplying && !wasReplying)
        {
            shouldFocus = true;
            wasReplying = true;
        }

        else if (!IsReplying && wasReplying)
        {
            wasReplying = false;
        }

        // Conversation dəyişdikdə draft-ı yüklə
        if (ConversationId != previousConversationId)
        {
            previousConversationId = ConversationId;
            shouldFocus = true;
            MessageText = InitialDraft ?? string.Empty;
        }
    }

    /// <summary>
    /// Render-dən sonra focus.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (shouldFocus || firstRender)
        {
            shouldFocus = false;
            try
            {
                await textAreaRef.FocusAsync();
            }
            catch
            {
                // Element hazır olmaya bilər
            }
        }
    }

    #endregion

    #region Input Handlers

    /// <summary>
    /// Input dəyişikliyi handler.
    /// </summary>
    private async Task HandleInput(ChangeEventArgs e)
    {
        var newValue = e.Value?.ToString() ?? "";

        // Limit enforce et
        if (newValue.Length > MaxLength)
        {
            newValue = newValue.Substring(0, MaxLength);
        }

        MessageText = newValue;

        // Yazarkən emoji picker bağla
        if (showEmojiPicker)
        {
            showEmojiPicker = false;
        }

        // Typing indicator göndər
        await StartTyping();

        // Textarea auto-resize
        await JS.InvokeVoidAsync("chatAppUtils.autoResizeTextarea", textAreaRef);

        // Draft dəyişikliyini parent-ə bildir
        await OnDraftChanged.InvokeAsync(newValue);
    }

    /// <summary>
    /// Key down handler.
    /// </summary>
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            if (showEmojiPicker) showEmojiPicker = false;
            await SendMessage();
        }
        else if (e.Key == "Escape" && showEmojiPicker)
        {
            showEmojiPicker = false;
        }
    }

    /// <summary>
    /// Blur handler.
    /// </summary>
    private async Task HandleBlur()
    {
        await StopTyping();
    }

    #endregion

    #region Typing Indicator

    /// <summary>
    /// Typing başladır.
    /// </summary>
    private async Task StartTyping()
    {
        if (!isTyping)
        {
            isTyping = true;
            await OnTyping.InvokeAsync(true);
        }
        typingTimer?.Stop();
        typingTimer?.Start();
    }

    /// <summary>
    /// Typing dayandırır.
    /// </summary>
    private async Task StopTyping()
    {
        if (isTyping)
        {
            isTyping = false;
            await InvokeAsync(() => OnTyping.InvokeAsync(false));
        }
    }

    #endregion

    #region Send/Edit Methods

    /// <summary>
    /// Mesaj göndərir və ya redaktəni saxlayır.
    /// </summary>
    private async Task SendMessage()
    {
        if (!CanSend) return;

        if (showEmojiPicker) showEmojiPicker = false;

        var message = MessageText.Trim();
        MessageText = string.Empty;
        await StopTyping();

        // Draft-ı təmizlə
        await OnDraftChanged.InvokeAsync(string.Empty);

        // Textarea height reset
        await JS.InvokeVoidAsync("chatAppUtils.resetTextareaHeight", textAreaRef);


        if (IsEditing)
        {
            await OnEdit.InvokeAsync(message);
        }
        else
        {
            await OnSend.InvokeAsync(message);
        }

        shouldFocus = true;
        StateHasChanged();
    }

    /// <summary>
    /// Edit-i ləğv edir.
    /// </summary>
    private async Task CancelEdit()
    {
        MessageText = string.Empty;
        await OnCancelEdit.InvokeAsync();
        await FocusAsync();
    }

    /// <summary>
    /// Reply-ı ləğv edir.
    /// </summary>
    private async Task CancelReply()
    {
        await OnCancelReply.InvokeAsync();
        await FocusAsync();
    }

    #endregion

    #region Emoji Picker

    /// <summary>
    /// Emoji picker toggle.
    /// </summary>
    private async Task ToggleEmojiPicker()
    {
        showEmojiPicker = !showEmojiPicker;

        if (showEmojiPicker)
        {
            shouldFocus = true;
            StateHasChanged();
            await Task.Delay(10);
            await FocusAsync();
        }
    }

    /// <summary>
    /// Emoji picker bağlama.
    /// </summary>
    private void CloseEmojiPicker()
    {
        showEmojiPicker = false;
    }

    /// <summary>
    /// Emoji əlavə etmə.
    /// </summary>
    private async Task InsertEmoji(string emoji)
    {
        if (MessageText.Length + emoji.Length <= MaxLength)
        {
            MessageText += emoji;
        }
        await FocusAsync();
    }

    #endregion

    #region Attachment

    /// <summary>
    /// Attach click handler - triggers file input.
    /// </summary>
    private async Task OnAttachClick()
    {
        try
        {
            // Trigger hidden file input
            await JS.InvokeVoidAsync("chatAppUtils.clickFileInput", fileInputRef.Element);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error opening file picker: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// File selection handler with validation and preview generation.
    /// </summary>
    private async Task HandleFileSelection(InputFileChangeEventArgs e)
    {
        const long maxFileSize = 100 * 1024 * 1024; // 100MB
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

        selectedFiles.Clear();

        try
        {
            foreach (var browserFile in e.GetMultipleFiles(20)) // Max 20 files
            {
                // Validate file size
                if (browserFile.Size > maxFileSize)
                {
                    Snackbar.Add($"{browserFile.Name} exceeds 100MB limit", Severity.Warning);
                    continue;
                }

                // Create SelectedFile model
                var extension = Path.GetExtension(browserFile.Name).ToLowerInvariant();
                var isImage = imageExtensions.Contains(extension);

                var selectedFile = new SelectedFile
                {
                    BrowserFile = browserFile,
                    FileName = browserFile.Name,
                    Extension = extension,
                    SizeInBytes = browserFile.Size,
                    ContentType = browserFile.ContentType,
                    IsImage = isImage,
                    State = UploadState.Pending
                };

                // Generate preview for images
                if (isImage)
                {
                    try
                    {
                        // Resize image for preview (max 400x400)
                        var resizedImage = await browserFile.RequestImageFileAsync(browserFile.ContentType, 400, 400);

                        // Read as data URL
                        using var stream = resizedImage.OpenReadStream(maxFileSize);
                        using var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        var bytes = memoryStream.ToArray();
                        var base64 = Convert.ToBase64String(bytes);
                        selectedFile.PreviewDataUrl = $"data:{browserFile.ContentType};base64,{base64}";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error generating preview for {browserFile.Name}: {ex.Message}");
                    }
                }

                selectedFiles.Add(selectedFile);
            }

            // Show file selection panel if files were selected
            if (selectedFiles.Count > 0)
            {
                showFileSelectionPanel = true;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error selecting files: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Close file selection panel.
    /// </summary>
    private void HandleCloseFilePanel()
    {
        showFileSelectionPanel = false;
        selectedFiles.Clear();
        StateHasChanged();
    }

    /// <summary>
    /// Send message with files.
    /// </summary>
    private async Task HandleSendWithFiles((List<SelectedFile> Files, string Message) data)
    {
        try
        {
            // Invoke parent callback with files and message
            await OnSendWithFiles.InvokeAsync(data);

            // Close panel and clear files
            showFileSelectionPanel = false;
            selectedFiles.Clear();
            MessageText = string.Empty;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error sending files: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Retry failed file upload.
    /// </summary>
    private async Task HandleRetryFile(SelectedFile file)
    {
        const long maxFileSize = 100 * 1024 * 1024; // 100MB

        try
        {
            file.State = UploadState.Uploading;
            file.UploadProgress = 0;
            file.ErrorMessage = null;
            StateHasChanged();

            // Regenerate preview for images if needed
            if (file.IsImage && string.IsNullOrEmpty(file.PreviewDataUrl))
            {
                try
                {
                    var resizedImage = await file.BrowserFile.RequestImageFileAsync(file.ContentType, 400, 400);
                    using var stream = resizedImage.OpenReadStream(maxFileSize);
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var bytes = memoryStream.ToArray();
                    var base64 = Convert.ToBase64String(bytes);
                    file.PreviewDataUrl = $"data:{file.ContentType};base64,{base64}";
                }
                catch
                {
                    // Preview generation is optional
                }
            }

            file.State = UploadState.Pending;
            StateHasChanged();
            Snackbar.Add($"Retry ready: {file.FileName}", Severity.Info);
        }
        catch (Exception ex)
        {
            file.State = UploadState.Failed;
            file.ErrorMessage = ex.Message;
            StateHasChanged();
            Snackbar.Add($"Error preparing retry: {ex.Message}", Severity.Error);
        }
    }

    #endregion

    #region Helper Methods


    /// <summary>
    /// Textarea height-ını reset edir.
    /// </summary>
    private async Task ResetTextareaHeight()
    {
        try
        {
            await JS.InvokeVoidAsync("chatAppUtils.resetTextareaHeight", textAreaRef);
        }
        catch
        {
            // JS interop fail ola bilər
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Textarea-ya focus edir.
    /// Parent komponentlər üçün public method.
    /// </summary>
    public async Task FocusAsync()
    {
        try
        {
            await textAreaRef.FocusAsync();
        }
        catch
        {
            // Element hazır olmaya bilər
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Fayl tipi üçün Material icon qaytarır.
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
    private string GetFileIconClass(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
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

    #endregion

    #region IDisposable

    /// <summary>
    /// Resurları təmizləyir.
    /// </summary>
    public void Dispose()
    {
        typingTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}