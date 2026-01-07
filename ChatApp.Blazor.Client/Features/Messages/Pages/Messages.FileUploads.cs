using ChatApp.Blazor.Client.Features.Files.Services;
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
    /// Direct message-lər üçün fayllarla mesaj göndərir.
    /// Hər fayl üçün ayrı mesaj yaradılır.
    /// </summary>
    private async Task SendDirectMessagesWithFiles(
        List<(bool IsSuccess, string? FileId, SelectedFile File)> uploadResults,
        string messageText)
    {
        if (!selectedConversationId.HasValue) return;

        // İlk mesajda mətn var (əgər user yazdısa), qalanları boş content ilə göndər
        for (int i = 0; i < uploadResults.Count; i++)
        {
            var (_, fileId, file) = uploadResults[i];
            if (fileId == null) continue;

            // Yalnız ilk mesajda user-in yazdığı mətn göndər, qalanları boş göndər
            var content = (i == 0 && !string.IsNullOrWhiteSpace(messageText)) ? messageText : string.Empty;

            var result = await ConversationService.SendMessageAsync(
                selectedConversationId.Value,
                content,
                fileId: fileId,
                replyToMessageId: null,
                isForwarded: false);

            if (result.IsSuccess)
            {
                var messageTime = DateTime.UtcNow;
                var messageId = result.Value;

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
                    false);                                         // IsForwarded

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

                // Conversation list-i yenilə (yalnız sonuncu mesaj üçün)
                if (i == uploadResults.Count - 1)
                {
                    UpdateConversationLocally(selectedConversationId.Value, content, messageTime);
                }
            }
            else
            {
                ShowError($"Failed to send message with file {file.FileName}: {result.Error}");
            }
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
                        false,                                      // IsForwarded
                        ReadByCount: 0,
                        TotalMemberCount: totalMembers,
                        ReadBy: [],
                        Reactions: []);

                    channelMessages.Add(newMessage);

                    // Pending marker sil
                    pendingMessageAdds.Remove(messageId);
                }

                // Channel list-i yenilə (yalnız sonuncu mesaj üçün)
                if (i == uploadResults.Count - 1)
                {
                    UpdateChannelLocally(selectedChannelId.Value, content, messageTime, UserState.CurrentUser?.DisplayName);
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
