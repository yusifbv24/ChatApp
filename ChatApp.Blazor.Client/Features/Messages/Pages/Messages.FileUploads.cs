using ChatApp.Blazor.Client.Models.Files;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region File Upload - Fayl yükləmə

    /// <summary>
    /// Fayllarla mesaj göndərir.
    /// Bütün fayllar paralel olaraq upload edilir, sonra hər fayl üçün ayrı mesaj göndərilir.
    /// </summary>
    private async Task HandleSendWithFiles((List<SelectedFile> Files, string Message) data)
    {
        if (data.Files == null || data.Files.Count == 0) return;

        isSendingMessage = true;
        StateHasChanged();

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

            // Upload faylları paralel
            var uploadTasks = data.Files.Select(file => UploadSingleFile(file));
            var uploadResults = await Task.WhenAll(uploadTasks);

            // Uğurlu upload-ları fil filtr et
            var successfulUploads = uploadResults
                .Where(r => r.IsSuccess && !string.IsNullOrEmpty(r.FileId))
                .ToList();

            if (successfulUploads.Count == 0)
            {
                ShowError("All file uploads failed");
                return;
            }

            // Hər fayl üçün ayrı mesaj göndər
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                await SendDirectMessagesWithFiles(successfulUploads, data.Message);
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                await SendChannelMessagesWithFiles(successfulUploads, data.Message);
            }

            // Draft cleared by MessageInput after send
        }
        catch (Exception ex)
        {
            ShowError($"Failed to send files: {ex.Message}");
        }
        finally
        {
            isSendingMessage = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Tək fayl upload edir və SelectedFile state-ini yeniləyir.
    /// </summary>
    private async Task<(bool IsSuccess, string? FileId, SelectedFile File)> UploadSingleFile(SelectedFile file)
    {
        try
        {
            file.State = UploadState.Uploading;
            file.UploadProgress = 0;
            StateHasChanged();

            // Pass conversation/channel context for proper file organization
            var uploadResult = await FileService.UploadFileAsync(
                file.BrowserFile,
                selectedConversationId,
                selectedChannelId);

            if (uploadResult.IsSuccess && uploadResult.Value != null)
            {
                file.State = UploadState.Completed;
                file.UploadProgress = 100;
                file.UploadedFileId = uploadResult.Value.FileId.ToString();
                StateHasChanged();

                return (true, uploadResult.Value.FileId.ToString(), file);
            }
            else
            {
                file.State = UploadState.Failed;
                file.ErrorMessage = uploadResult.Error ?? "Upload failed";
                StateHasChanged();

                return (false, null, file);
            }
        }
        catch (Exception ex)
        {
            file.State = UploadState.Failed;
            file.ErrorMessage = ex.Message;
            StateHasChanged();

            return (false, null, file);
        }
    }

    /// <summary>
    /// PERFORMANCE: Direct message-lər üçün fayllarla mesaj göndərir.
    /// Batch API istifadə edərək bütün mesajları bir request-də göndərir (N request → 1 request).
    /// </summary>
    private async Task SendDirectMessagesWithFiles(
        List<(bool IsSuccess, string? FileId, SelectedFile File)> uploadResults,
        string messageText)
    {
        if (!selectedConversationId.HasValue) return;

        // Build batch request
        var batchMessages = uploadResults
            .Where(r => !string.IsNullOrEmpty(r.FileId))
            .Select((r, index) => new BatchMessageItem
            {
                Content = (index == 0 && !string.IsNullOrWhiteSpace(messageText)) ? messageText : string.Empty,
                FileId = r.FileId
            })
            .ToList();

        if (batchMessages.Count == 0) return;

        var batchRequest = new BatchSendMessagesRequest
        {
            Messages = batchMessages,
            ReplyToMessageId = null,
            IsForwarded = false,
            Mentions = []
        };

        // PERFORMANCE: Single API call instead of N calls
        var result = await ConversationService.SendBatchMessagesAsync(selectedConversationId.Value, batchRequest);

        if (result.IsSuccess && result.Value != null)
        {
            var messageIds = result.Value;
            var messageTime = DateTime.UtcNow;

            // Create optimistic UI messages for each
            for (int i = 0; i < messageIds.Count && i < uploadResults.Count; i++)
            {
                var (_, fileId, file) = uploadResults[i];
                var messageId = messageIds[i];
                var content = (i == 0 && !string.IsNullOrWhiteSpace(messageText)) ? messageText : string.Empty;

                // Pending read receipt yoxla
                bool hasReadReceipt = pendingReadReceipts.TryGetValue(messageId, out _);

                // OPTİMİSTİC UI
                var newMessage = new DirectMessageDto(
                    messageId,
                    selectedConversationId.Value,
                    currentUserId,
                    UserState.CurrentUser?.Username ?? "",
                    UserState.CurrentUser?.DisplayName ?? "",
                    UserState.CurrentUser?.AvatarUrl,
                    recipientUserId,
                    content,
                    fileId,                                         // FileId
                    file.FileName,                                  // FileName
                    file.ContentType,                               // FileContentType
                    file.SizeInBytes,                               // FileSizeInBytes
                    false,                                          // IsEdited
                    false,                                          // IsDeleted
                    hasReadReceipt,                                 // IsRead
                    false,                                          // IsPinned
                    0,                                              // ReactionCount
                    messageTime,                                    // CreatedAtUtc
                    null,                                           // EditedAtUtc
                    null,                                           // PinnedAtUtc
                    null,                                           // ReplyToMessageId
                    null,                                           // ReplyToContent
                    null,                                           // ReplyToSenderName
                    null,                                           // ReplyToFileId
                    null,                                           // ReplyToFileName
                    null,                                           // ReplyToFileContentType
                    false,                                          // IsForwarded
                    null);                                          // Reactions

                // Dublikat yoxla
                if (!directMessages.Any(m => m.Id == messageId))
                {
                    directMessages.Add(newMessage);
                }

                // Pending receipt istifadə olundusa, sil
                if (hasReadReceipt)
                {
                    pendingReadReceipts.Remove(messageId);
                }
            }

            // Conversation list-i yenilə (sonuncu mesaj ilə)
            if (uploadResults.Count > 0 && messageIds.Count > 0)
            {
                var lastIndex = uploadResults.Count - 1;
                var (_, lastFileId, lastFile) = uploadResults[lastIndex];
                var lastContent = !string.IsNullOrWhiteSpace(messageText) ? messageText : string.Empty;

                var lastMessage = new DirectMessageDto(
                    messageIds[messageIds.Count - 1],
                    selectedConversationId.Value,
                    currentUserId,
                    UserState.CurrentUser?.Username ?? "",
                    UserState.CurrentUser?.DisplayName ?? "",
                    UserState.CurrentUser?.AvatarUrl,
                    recipientUserId,
                    lastContent,
                    lastFileId,
                    lastFile.FileName,
                    lastFile.ContentType,
                    lastFile.SizeInBytes,
                    false, false, false, false, 0,
                    messageTime,
                    null, null, null, null, null, null, null, null, false, null);

                var preview = GetFilePreview(lastMessage);
                UpdateConversationLocally(selectedConversationId.Value, preview, messageTime);
            }
        }
        else
        {
            ShowError($"Failed to send batch messages: {result.Error}");
        }

        StateHasChanged();
    }

    /// <summary>
    /// Channel mesajları üçün fayllarla mesaj göndərir.
    /// Hər fayl üçün ayrı mesaj yaradılır.
    /// </summary>
    private async Task SendChannelMessagesWithFiles(
        List<(bool IsSuccess, string? FileId, SelectedFile File)> uploadResults,
        string messageText)
    {
        if (!selectedChannelId.HasValue) return;

        // İlk mesajda mətn var (əgər user yazdısa), qalanları boş content ilə göndər
        for (int i = 0; i < uploadResults.Count; i++)
        {
            var (_, fileId, file) = uploadResults[i];
            if (fileId == null) continue;

            // Yalnız ilk mesajda user-in yazdığı mətn göndər, qalanları boş göndər
            var content = (i == 0 && !string.IsNullOrWhiteSpace(messageText)) ? messageText : string.Empty;

            var result = await ChannelService.SendMessageAsync(
                selectedChannelId.Value,
                content,
                fileId: fileId,
                replyToMessageId: null,
                isForwarded: false);

            if (result.IsSuccess)
            {
                var messageTime = DateTime.UtcNow;
                var messageId = result.Value;

                // Race condition yoxla
                if (pendingMessageAdds.Contains(messageId))
                {
                    // SignalR artıq əlavə edir
                }
                else if (channelMessages.Any(m => m.Id == messageId))
                {
                    // SignalR artıq əlavə edib
                }
                else
                {
                    // Pending marker qoy
                    pendingMessageAdds.Add(messageId);

                    var totalMembers = Math.Max(0, selectedChannelMemberCount - 1);

                    // OPTİMİSTİK UI
                    var newMessage = new ChannelMessageDto(
                        messageId,
                        selectedChannelId.Value,
                        currentUserId,
                        UserState.CurrentUser?.Username ?? "",
                        UserState.CurrentUser?.DisplayName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        content,
                        fileId,                                     // FileId
                        file.FileName,                              // FileName
                        file.ContentType,                           // FileContentType
                        file.SizeInBytes,                           // FileSizeInBytes
                        false,                                      // IsEdited
                        false,                                      // IsDeleted
                        false,                                      // IsPinned
                        0,                                          // ReactionCount
                        messageTime,                                // CreatedAtUtc
                        null,                                       // EditedAtUtc
                        null,                                       // PinnedAtUtc
                        null,                                       // ReplyToMessageId
                        null,                                       // ReplyToContent
                        null,                                       // ReplyToSenderName
                        null,                                       // ReplyToFileId
                        null,                                       // ReplyToFileName
                        null,                                       // ReplyToFileContentType
                        false,                                      // IsForwarded
                        0,                                          // ReadByCount
                        totalMembers,                               // TotalMemberCount
                        [],                                         // ReadBy
                        []);                                        // Reactions

                    channelMessages.Add(newMessage);

                    // Pending marker sil
                    pendingMessageAdds.Remove(messageId);
                }

                // Channel list-i yenilə (yalnız sonuncu mesaj üçün)
                if (i == uploadResults.Count - 1)
                {
                    // File preview hesabla (conversation list üçün sadə format)
                    string preview;
                    if (fileId != null)
                    {
                        if (file.ContentType != null && file.ContentType.StartsWith("image/"))
                        {
                            preview = string.IsNullOrWhiteSpace(content) ? "[Image]" : $"[Image] {content}";
                        }
                        else
                        {
                            preview = string.IsNullOrWhiteSpace(content) ? "[File]" : $"[File] {content}";
                        }
                    }
                    else
                    {
                        preview = content;
                    }
                    UpdateChannelLocally(selectedChannelId.Value, preview, messageTime, UserState.CurrentUser?.DisplayName);
                }
            }
            else
            {
                ShowError($"Failed to send message with file {file.FileName}: {result.Error}");
            }
        }

        StateHasChanged();
    }

    #endregion
}
