using ChatApp.Blazor.Client.Models.Files;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Shared.Kernel;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region File Upload State - Aktiv upload-lar

    /// <summary>
    /// Aktiv upload-ların tracking dictionary-si.
    /// Key: TempId (mesaj), Value: CancellationTokenSource
    /// </summary>
    private readonly Dictionary<Guid, CancellationTokenSource> _activeUploads = new();

    #endregion

    #region File Upload - Fayl yükləmə

    /// <summary>
    /// Fayllarla mesaj göndərir - Bitrix24 style optimistic UI.
    /// 1. Əvvəlcə optimistic mesaj UI-da göstərilir (upload state ilə)
    /// 2. Sonra fayl upload başlayır
    /// 3. Upload bitdikdə mesaj backend-ə göndərilir
    /// </summary>
    private async Task HandleSendWithFiles((List<SelectedFile> Files, string Message) data)
    {
        if (data.Files == null || data.Files.Count == 0) return;

        try
        {
            // PENDING CONVERSATION - əvvəlcə yarat
            if (isPendingConversation && pendingUser != null)
            {
                var createResult = await ConversationService.StartConversationAsync(pendingUser.Id);
                if (!createResult.IsSuccess)
                {
                    ShowError(createResult.Error ?? "Failed to create conversation");
                    return;
                }

                selectedConversationId = createResult.Value;
                isPendingConversation = false;
                pendingUser = null;

                await SignalRService.JoinConversationAsync(selectedConversationId.Value);
                await LoadConversationsAndChannels();
            }

            // Hər fayl üçün ayrı optimistic mesaj yarat və upload başlat
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                await SendDirectMessagesWithFilesOptimistic(data.Files, data.Message);
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                await SendChannelMessagesWithFilesOptimistic(data.Files, data.Message);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to send files: {ex.Message}");
        }
    }

    /// <summary>
    /// Direct message - Optimistic UI ilə fayl göndərmə.
    /// </summary>
    private async Task SendDirectMessagesWithFilesOptimistic(List<SelectedFile> files, string messageText)
    {
        if (!selectedConversationId.HasValue) return;

        var conversationId = selectedConversationId.Value;
        var messageTime = DateTime.UtcNow;

        // Hər fayl üçün optimistic mesaj yarat
        var uploadTasks = new List<Task>();

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var tempId = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            var content = (i == 0 && !string.IsNullOrWhiteSpace(messageText)) ? messageText : string.Empty;

            // Track active upload
            _activeUploads[tempId] = cts;
            file.CancellationTokenSource = cts;
            file.AssociatedMessageId = tempId;

            // OPTIMISTIC UI - Mesajı dərhal göstər (uploading state ilə)
            var optimisticMessage = new DirectMessageDto(
                tempId,                                         // Temporary ID
                conversationId,
                currentUserId,
                UserState.CurrentUser?.Email ?? "",
                UserState.CurrentUser?.FullName ?? "",
                UserState.CurrentUser?.AvatarUrl,
                recipientUserId,
                content,
                null,                                           // FileId - hələ yoxdur
                file.FileName,                                  // FileName
                file.ContentType,                               // FileContentType
                file.SizeInBytes,                               // FileSizeInBytes
                false,                                          // IsEdited
                false,                                          // IsDeleted
                false,                                          // IsRead
                false,                                          // IsPinned
                0,                                              // ReactionCount
                messageTime.AddMilliseconds(i),                 // CreatedAtUtc (ordered)
                null,                                           // EditedAtUtc
                null,                                           // PinnedAtUtc
                null, null, null, null, null, null,             // Reply fields
                false,                                          // IsForwarded
                null,                                           // Reactions
                null,                                           // Mentions
                MessageStatus.Pending,                          // Status - Pending (uploading)
                tempId,                                         // TempId
                UploadState.Uploading,                          // FileUploadState
                0,                                              // FileUploadProgress
                cts);                                           // FileUploadCts

            directMessages.Add(optimisticMessage);

            // CRITICAL: SignalR handler-ın dublikatı tanıması üçün pending dictionary-ə əlavə et
            // SignalR handler SenderId + Content ilə match edir
            pendingDirectMessages[tempId] = optimisticMessage;

            // Conversation list-i yenilə (sonuncu fayl üçün)
            if (i == files.Count - 1)
            {
                var preview = file.ContentType?.StartsWith("image/") == true ? "[Image]" : "[File]";
                if (!string.IsNullOrWhiteSpace(content)) preview += $" {content}";
                UpdateConversationLocally(conversationId, preview, messageTime);
            }

            // Upload task-ı başlat (paralel)
            var fileIndex = i;
            var fileContent = content;
            uploadTasks.Add(UploadAndSendDirectMessage(file, tempId, conversationId, fileContent, cts.Token));
        }

        StateHasChanged();

        // Bütün upload-ları paralel işlət
        await Task.WhenAll(uploadTasks);
    }

    /// <summary>
    /// Tək fayl upload edib mesaj göndərir (Direct Message).
    /// </summary>
    private async Task UploadAndSendDirectMessage(
        SelectedFile file,
        Guid tempId,
        Guid conversationId,
        string content,
        CancellationToken cancellationToken)
    {
        try
        {
            // Upload progress callback
            var progressCallback = new Progress<int>(progress =>
            {
                InvokeAsync(() =>
                {
                    var message = directMessages.FirstOrDefault(m => m.TempId == tempId);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        directMessages[index] = message with { FileUploadProgress = progress };
                        InvalidateMessageCache();
                        StateHasChanged();
                    }
                });
            });

            // Upload file
            var uploadResult = await FileService.UploadFileWithProgressAsync(
                file.BrowserFile,
                selectedConversationId,
                selectedChannelId,
                progressCallback,
                cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                // Cancelled - mesajı sil
                await InvokeAsync(() =>
                {
                    directMessages.RemoveAll(m => m.TempId == tempId);
                    _activeUploads.Remove(tempId);
                    InvalidateMessageCache();
                    StateHasChanged();
                });
                return;
            }

            if (!uploadResult.IsSuccess || uploadResult.Value == null)
            {
                // Upload failed - mesajı failed state-ə keçir
                await InvokeAsync(() =>
                {
                    var message = directMessages.FirstOrDefault(m => m.TempId == tempId);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        directMessages[index] = message with
                        {
                            Status = MessageStatus.Failed,
                            FileUploadState = UploadState.Failed
                        };
                        InvalidateMessageCache();
                    }
                    _activeUploads.Remove(tempId);
                    StateHasChanged();
                });
                return;
            }

            var fileId = uploadResult.Value.FileId.ToString();

            // Upload completed - update state
            await InvokeAsync(() =>
            {
                var message = directMessages.FirstOrDefault(m => m.TempId == tempId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with
                    {
                        FileUploadState = UploadState.Completed,
                        FileUploadProgress = 100
                    };
                    InvalidateMessageCache();
                    StateHasChanged();
                }
            });

            // Send message to backend
            var result = await ConversationService.SendMessageAsync(
                conversationId,
                content,
                fileId: fileId,
                replyToMessageId: null,
                isForwarded: false);

            await InvokeAsync(() =>
            {
                var message = directMessages.FirstOrDefault(m => m.TempId == tempId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);

                    if (result.IsSuccess)
                    {
                        // Success - update with real ID
                        directMessages[index] = message with
                        {
                            Id = result.Value,
                            FileId = fileId,
                            Status = MessageStatus.Sent,
                            FileUploadState = null,  // Clear upload state
                            FileUploadCts = null
                        };

                        // Conversation list status-u yenilə
                        UpdateListItemWhere(
                            ref directConversations,
                            c => c.Id == conversationId && c.LastMessageSenderId == currentUserId && c.LastMessageStatus == "Pending",
                            c => c with { LastMessageStatus = "Sent", LastMessageId = result.Value }
                        );
                    }
                    else
                    {
                        // Failed
                        directMessages[index] = message with
                        {
                            Status = MessageStatus.Failed,
                            FileUploadState = UploadState.Failed
                        };

                        // Conversation list status-u yenilə
                        UpdateListItemWhere(
                            ref directConversations,
                            c => c.Id == conversationId && c.LastMessageSenderId == currentUserId && c.LastMessageStatus == "Pending",
                            c => c with { LastMessageStatus = "Failed" }
                        );
                    }

                    InvalidateMessageCache();
                }

                _activeUploads.Remove(tempId);
                StateHasChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // Cancelled
            await InvokeAsync(() =>
            {
                directMessages.RemoveAll(m => m.TempId == tempId);
                _activeUploads.Remove(tempId);
                InvalidateMessageCache();
                StateHasChanged();
            });
        }
        catch (Exception)
        {
            // Error
            await InvokeAsync(() =>
            {
                var message = directMessages.FirstOrDefault(m => m.TempId == tempId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with
                    {
                        Status = MessageStatus.Failed,
                        FileUploadState = UploadState.Failed
                    };
                    InvalidateMessageCache();
                }
                _activeUploads.Remove(tempId);
                StateHasChanged();
            });
        }
    }

    /// <summary>
    /// Channel message - Optimistic UI ilə fayl göndərmə.
    /// </summary>
    private async Task SendChannelMessagesWithFilesOptimistic(List<SelectedFile> files, string messageText)
    {
        if (!selectedChannelId.HasValue) return;

        var channelId = selectedChannelId.Value;
        var messageTime = DateTime.UtcNow;
        var totalMembers = Math.Max(0, selectedChannelMemberCount - 1);

        // Hər fayl üçün optimistic mesaj yarat
        var uploadTasks = new List<Task>();

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var tempId = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            var content = (i == 0 && !string.IsNullOrWhiteSpace(messageText)) ? messageText : string.Empty;

            // Track active upload
            _activeUploads[tempId] = cts;
            file.CancellationTokenSource = cts;
            file.AssociatedMessageId = tempId;

            // OPTIMISTIC UI - Mesajı dərhal göstər (uploading state ilə)
            var optimisticMessage = new ChannelMessageDto(
                tempId,                                         // Temporary ID
                channelId,
                currentUserId,
                UserState.CurrentUser?.Email ?? "",
                UserState.CurrentUser?.FullName ?? "",
                UserState.CurrentUser?.AvatarUrl,
                content,
                null,                                           // FileId - hələ yoxdur
                file.FileName,                                  // FileName
                file.ContentType,                               // FileContentType
                file.SizeInBytes,                               // FileSizeInBytes
                false,                                          // IsEdited
                false,                                          // IsDeleted
                false,                                          // IsPinned
                0,                                              // ReactionCount
                messageTime.AddMilliseconds(i),                 // CreatedAtUtc (ordered)
                null,                                           // EditedAtUtc
                null,                                           // PinnedAtUtc
                null, null, null, null, null, null,             // Reply fields
                false,                                          // IsForwarded
                0,                                              // ReadByCount
                totalMembers,                                   // TotalMemberCount
                [],                                             // ReadBy
                [],                                             // Reactions
                [],                                             // Mentions
                MessageStatus.Pending,                          // Status - Pending (uploading)
                tempId,                                         // TempId
                UploadState.Uploading,                          // FileUploadState
                0,                                              // FileUploadProgress
                cts);                                           // FileUploadCts

            channelMessages.Add(optimisticMessage);

            // CRITICAL: SignalR handler-ın dublikatı tanıması üçün pending dictionary-ə əlavə et
            // SignalR handler SenderId + Content ilə match edir
            pendingChannelMessages[tempId] = optimisticMessage;

            // Channel list-i yenilə (sonuncu fayl üçün)
            if (i == files.Count - 1)
            {
                var preview = file.ContentType?.StartsWith("image/") == true ? "[Image]" : "[File]";
                if (!string.IsNullOrWhiteSpace(content)) preview += $" {content}";
                UpdateChannelLocally(channelId, preview, messageTime, UserState.CurrentUser?.FullName);
            }

            // Upload task-ı başlat (paralel)
            var fileIndex = i;
            var fileContent = content;
            uploadTasks.Add(UploadAndSendChannelMessage(file, tempId, channelId, fileContent, totalMembers, cts.Token));
        }

        StateHasChanged();

        // Bütün upload-ları paralel işlət
        await Task.WhenAll(uploadTasks);
    }

    /// <summary>
    /// Tək fayl upload edib mesaj göndərir (Channel Message).
    /// </summary>
    private async Task UploadAndSendChannelMessage(
        SelectedFile file,
        Guid tempId,
        Guid channelId,
        string content,
        int totalMembers,
        CancellationToken cancellationToken)
    {
        try
        {
            // Upload progress callback
            var progressCallback = new Progress<int>(progress =>
            {
                InvokeAsync(() =>
                {
                    var message = channelMessages.FirstOrDefault(m => m.TempId == tempId);
                    if (message != null)
                    {
                        message.FileUploadProgress = progress;
                        InvalidateMessageCache();
                        StateHasChanged();
                    }
                });
            });

            // Upload file
            var uploadResult = await FileService.UploadFileWithProgressAsync(
                file.BrowserFile,
                selectedConversationId,
                selectedChannelId,
                progressCallback,
                cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                // Cancelled - mesajı sil
                await InvokeAsync(() =>
                {
                    channelMessages.RemoveAll(m => m.TempId == tempId);
                    _activeUploads.Remove(tempId);
                    InvalidateMessageCache();
                    StateHasChanged();
                });
                return;
            }

            if (!uploadResult.IsSuccess || uploadResult.Value == null)
            {
                // Upload failed
                await InvokeAsync(() =>
                {
                    var message = channelMessages.FirstOrDefault(m => m.TempId == tempId);
                    if (message != null)
                    {
                        message.Status = MessageStatus.Failed;
                        message.FileUploadState = UploadState.Failed;
                        InvalidateMessageCache();
                    }
                    _activeUploads.Remove(tempId);
                    StateHasChanged();
                });
                return;
            }

            var fileId = uploadResult.Value.FileId.ToString();

            // Upload completed - update state
            await InvokeAsync(() =>
            {
                var message = channelMessages.FirstOrDefault(m => m.TempId == tempId);
                if (message != null)
                {
                    message.FileUploadState = UploadState.Completed;
                    message.FileUploadProgress = 100;
                    InvalidateMessageCache();
                    StateHasChanged();
                }
            });

            // Send message to backend
            var result = await ChannelService.SendMessageAsync(
                channelId,
                content,
                fileId: fileId,
                replyToMessageId: null,
                isForwarded: false);

            await InvokeAsync(() =>
            {
                var message = channelMessages.FirstOrDefault(m => m.TempId == tempId);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);

                    if (result.IsSuccess)
                    {
                        // Success - update with real ID
                        channelMessages[index] = message with
                        {
                            Id = result.Value,
                            FileId = fileId,
                            Status = MessageStatus.Sent,
                            FileUploadState = null,  // Clear upload state
                            FileUploadCts = null
                        };

                        // Channel list status-u yenilə
                        UpdateListItemWhere(
                            ref channelConversations,
                            c => c.Id == channelId && c.LastMessageSenderId == currentUserId && c.LastMessageStatus == "Pending",
                            c => c with { LastMessageStatus = "Sent", LastMessageId = result.Value }
                        );
                    }
                    else
                    {
                        // Failed
                        message.Status = MessageStatus.Failed;
                        message.FileUploadState = UploadState.Failed;

                        // Channel list status-u yenilə
                        UpdateListItemWhere(
                            ref channelConversations,
                            c => c.Id == channelId && c.LastMessageSenderId == currentUserId && c.LastMessageStatus == "Pending",
                            c => c with { LastMessageStatus = "Failed" }
                        );
                    }

                    InvalidateMessageCache();
                }

                _activeUploads.Remove(tempId);
                StateHasChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // Cancelled
            await InvokeAsync(() =>
            {
                channelMessages.RemoveAll(m => m.TempId == tempId);
                _activeUploads.Remove(tempId);
                InvalidateMessageCache();
                StateHasChanged();
            });
        }
        catch (Exception)
        {
            // Error
            await InvokeAsync(() =>
            {
                var message = channelMessages.FirstOrDefault(m => m.TempId == tempId);
                if (message != null)
                {
                    message.Status = MessageStatus.Failed;
                    message.FileUploadState = UploadState.Failed;
                    InvalidateMessageCache();
                }
                _activeUploads.Remove(tempId);
                StateHasChanged();
            });
        }
    }

    /// <summary>
    /// Fayl upload-ını ləğv et.
    /// UI-dan çağrılır (cancel button).
    /// </summary>
    public void CancelFileUpload(Guid tempId)
    {
        if (_activeUploads.TryGetValue(tempId, out var cts))
        {
            cts.Cancel();
            // Mesaj UploadAndSend metodunda silinəcək
        }
    }

    #endregion
}
