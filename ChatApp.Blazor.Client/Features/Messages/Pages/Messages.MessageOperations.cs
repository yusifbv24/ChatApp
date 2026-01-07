using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Send Message - Mesaj göndər

    /// <summary>
    /// Mesaj göndərir.
    /// OPTİMİSTİC UI: Dərhal list-ə əlavə et (server cavabını gözləmədən)
    /// Conversation/Channel list-i yenilə
    /// </summary>
    private async Task SendMessage(string content)
    {
        // Boş mesaj göndərilməsin
        if (string.IsNullOrWhiteSpace(content)) return;

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

                // Conversation yaradıldı
                selectedConversationId = createResult.Value;
                isPendingConversation = false;
                pendingUser = null;

                // SignalR group-a join
                await SignalRService.JoinConversationAsync(selectedConversationId.Value);

                // List-i yenidən yüklə
                await LoadConversationsAndChannels();
            }

            // DM MESAJI
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.SendMessageAsync(
                    selectedConversationId.Value,
                    content,
                    fileId: null,
                    replyToMessageId: replyToMessageId,
                    isForwarded: false);

                if (result.IsSuccess)
                {
                    var messageTime = DateTime.UtcNow;
                    var messageId = result.Value;

                    // Pending read receipt yoxla (race condition halı)
                    bool hasReadReceipt = pendingReadReceipts.TryGetValue(messageId, out var readReceipt);

                    // OPTİMİSTİC UI: Dərhal UI-a əlavə et
                    var newMessage = new DirectMessageDto(
                        messageId,
                        selectedConversationId.Value,
                        currentUserId,
                        UserState.CurrentUser?.Username ?? "",
                        UserState.CurrentUser?.DisplayName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        recipientUserId,
                        content,
                        null,                                           // FileId
                        null,                                           // FileName
                        null,                                           // FileContentType
                        null,                                           // FileSizeInBytes
                        false,                                          // IsEdited
                        false,                                          // IsDeleted
                        hasReadReceipt,                                 // IsRead
                        false,                                          // IsPinned
                        0,                                              // ReactionCount
                        messageTime,                                    // CreatedAtUtc
                        null,                                           // EditedAtUtc
                        null,                                           // PinnedAtUtc
                        replyToMessageId,
                        replyToContent,
                        replyToSenderName,
                        false);                                         // IsForwarded

                    // Dublikat yoxla (SignalR-dan gəlmiş ola bilər)
                    if (!directMessages.Any(m => m.Id == messageId))
                    {
                        directMessages.Add(newMessage);
                    }

                    // Pending receipt istifadə olundusa, sil
                    if (hasReadReceipt)
                    {
                        pendingReadReceipts.Remove(messageId);
                    }

                    // Conversation list-i yenilə (son mesaj olaraq)
                    UpdateConversationLocally(selectedConversationId.Value, content, messageTime);
                }
                else
                {
                    ShowError(result.Error ?? "Failed to send message");
                }
            }

            // CHANNEL MESAJI
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.SendMessageAsync(
                    selectedChannelId.Value,
                    content,
                    fileId: null,
                    replyToMessageId: replyToMessageId,
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

                        // TotalMemberCount = üzvlər - sender
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
                            null,                                       // FileId
                            null,                                       // FileName
                            null,                                       // FileContentType
                            null,                                       // FileSizeInBytes
                            false,
                            false,
                            false,
                            0,
                            messageTime,
                            null,
                            null,
                            replyToMessageId,
                            replyToContent,
                            replyToSenderName,
                            false,
                            ReadByCount: 0,
                            TotalMemberCount: totalMembers,
                            ReadBy: [],
                            Reactions: []);

                        channelMessages.Add(newMessage);

                        // Pending marker sil
                        pendingMessageAdds.Remove(messageId);
                    }

                    UpdateChannelLocally(selectedChannelId.Value, content, messageTime, UserState.CurrentUser?.DisplayName);
                }
                else
                {
                    ShowError(result.Error ?? "Failed to send message");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to send message: " + ex.Message);
        }
        finally
        {
            isSendingMessage = false;
            CancelReply();
            StateHasChanged();
        }
    }

    #endregion

    #region Edit Message - Mesajı redaktə et

    /// <summary>
    /// Mesajı redaktə edir.
    /// Yalnız öz mesajlarını redaktə edə bilərsiniz.
    /// Silinmiş mesajlar redaktə olunmur
    /// Forward olunmuş mesajlar redaktə olunmur
    /// </summary>
    private async Task EditMessage((Guid messageId, string content) edit)
    {
        try
        {
            // DM
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.EditMessageAsync(
                    selectedConversationId.Value,
                    edit.messageId,
                    edit.content);

                if (result.IsSuccess)
                {
                    // Local mesajı yenilə (əgər content dəyişibsə)
                    var message = directMessages.FirstOrDefault(m => m.Id == edit.messageId);
                    if (message != null && message.Content != edit.content)
                    {
                        var index = directMessages.IndexOf(message);
                        var updatedMessage = message with { Content = edit.content, IsEdited = true };
                        directMessages[index] = updatedMessage;
                        InvalidateMessageCache();

                        // Conversation list-i də yenilə (son mesaj isə)
                        if (IsLastMessageInConversation(selectedConversationId.Value, updatedMessage.Id))
                        {
                            UpdateConversationLastMessage(selectedConversationId.Value, edit.content);
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowError(result.Error ?? "Failed to edit message");
                }
            }

            // CHANNEL
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.EditMessageAsync(
                    selectedChannelId.Value,
                    edit.messageId,
                    edit.content);

                if (result.IsSuccess)
                {
                    var message = channelMessages.FirstOrDefault(m => m.Id == edit.messageId);
                    if (message != null && message.Content != edit.content)
                    {
                        var index = channelMessages.IndexOf(message);
                        var updatedMessage = message with { Content = edit.content, IsEdited = true };
                        channelMessages[index] = updatedMessage;
                        InvalidateMessageCache();

                        if (IsLastMessageInChannel(selectedChannelId.Value, updatedMessage.Id))
                        {
                            UpdateChannelLastMessage(selectedChannelId.Value, edit.content, message.SenderDisplayName);
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowError(result.Error ?? "Failed to edit message");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to edit message: " + ex.Message);
        }
    }

    #endregion

    #region Delete Message - Mesajı sil

    /// <summary>
    /// Mesajı silir.
    /// Mesaj list-dən silinmir, IsDeleted=true ilə işarələnir.
    /// UI-da "This message was deleted" göstərilir.
    /// </summary>
    private async Task DeleteMessage(Guid messageId)
    {
        try
        {
            // DM
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.DeleteMessageAsync(
                    selectedConversationId.Value,
                    messageId);

                if (result.IsSuccess)
                {
                    var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        // Content-i boşalt, IsDeleted=true
                        var deletedMessage = message with { IsDeleted = true, Content = "" };
                        directMessages[index] = deletedMessage;
                        InvalidateMessageCache();

                        // Son mesaj isə, conversation list-i yenilə
                        if (IsLastMessageInConversation(selectedConversationId.Value, deletedMessage.Id))
                        {
                            UpdateConversationLastMessage(selectedConversationId.Value, "This message was deleted");
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowError(result.Error ?? "Failed to delete message");
                }
            }

            // CHANNEL
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.DeleteMessageAsync(
                    selectedChannelId.Value,
                    messageId);

                if (result.IsSuccess)
                {
                    var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                    if (message != null)
                    {
                        var index = channelMessages.IndexOf(message);
                        var deletedMessage = message with { IsDeleted = true, Content = "" };
                        channelMessages[index] = deletedMessage;
                        InvalidateMessageCache();

                        if (IsLastMessageInChannel(selectedChannelId.Value, deletedMessage.Id))
                        {
                            UpdateChannelLastMessage(selectedChannelId.Value, "This message was deleted", message.SenderDisplayName);
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowError(result.Error ?? "Failed to delete message");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to delete message: " + ex.Message);
        }
    }

    #endregion

    #region Reactions - Emoji reaksiyalar

    /// <summary>
    /// Mesaja reaction əlavə et / sil.
    /// Toggle pattern: eyni emoji-yə 2-ci dəfə click = sil.
    /// </summary>
    private async Task AddReaction((Guid messageId, string emoji) reaction)
    {
        try
        {
            // DM
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.ToggleReactionAsync(
                    selectedConversationId.Value,
                    reaction.messageId,
                    reaction.emoji);

                if (result.IsSuccess && result.Value != null)
                {
                    UpdateDirectMessageReactions(reaction.messageId, result.Value.Reactions);
                }
            }

            // CHANNEL
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.ToggleReactionAsync(
                    selectedChannelId.Value,
                    reaction.messageId,
                    reaction.emoji);

                if (result.IsSuccess && result.Value != null)
                {
                    UpdateChannelMessageReactions(reaction.messageId, result.Value);
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to toggle reaction: " + ex.Message);
        }
    }

    /// <summary>
    /// DM mesajının reaction-larını yenilə.
    /// </summary>
    private void UpdateDirectMessageReactions(Guid messageId, List<ReactionSummary> reactions)
    {
        var message = directMessages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            var totalCount = reactions.Sum(r => r.Count);
            var index = directMessages.IndexOf(message);

            var updatedMessage = message with
            {
                ReactionCount = totalCount,
                Reactions = reactions.Select(r => new MessageReactionDto(r.Emoji, r.Count, r.UserIds)).ToList()
            };

            directMessages[index] = updatedMessage;
            InvalidateMessageCache();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Channel mesajının reaction-larını yenilə.
    /// </summary>
    private void UpdateChannelMessageReactions(Guid messageId, List<ChannelMessageReactionDto> reactions)
    {
        var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            var totalCount = reactions.Sum(r => r.Count);
            var index = channelMessages.IndexOf(message);

            var updatedMessage = message with
            {
                ReactionCount = totalCount,
                Reactions = reactions
            };

            channelMessages[index] = updatedMessage;
            InvalidateMessageCache();
            StateHasChanged();
        }
    }

    #endregion

    #region Typing Indicator - Yazır göstəricisi

    /// <summary>
    /// "Yazır..." göstəricisini göndərir.
    /// MessageInput component-dən çağrılır.
    ///
    /// THROTTLE:
    /// MessageInput throttle edir (2 saniyədə 1 dəfə).
    /// Bu sayədə hər keystroke-da SignalR mesajı göndərilmir.
    /// </summary>
    private async Task HandleTyping(bool isTyping)
    {
        if (isDirectMessage && selectedConversationId.HasValue && recipientUserId != Guid.Empty)
        {
            await SignalRService.SendTypingInConversationAsync(selectedConversationId.Value, recipientUserId, isTyping);
        }
        else if (!isDirectMessage && selectedChannelId.HasValue)
        {
            await SignalRService.SendTypingInChannelAsync(selectedChannelId.Value, isTyping);
        }
    }

    #endregion

    #region Mark As Read/Unread - Oxundu işarəsi

    /// <summary>
    /// Tab görünür olduqda oxunmamış mesajları mark as read edir.
    /// Page Visibility API istifadə edir.
    ///
    /// SMART THRESHOLD:
    /// 5+ mesaj = bulk API (1 request)
    /// <5 mesaj = individual API-lar (paralel)
    /// </summary>
    private async Task MarkUnreadMessagesAsRead()
    {
        // DM
        if (selectedConversationId.HasValue)
        {
            var unreadMessages = directMessages.Where(m => !m.IsRead && m.SenderId != currentUserId).ToList();
            if (unreadMessages.Count != 0)
            {
                try
                {
                    if (unreadMessages.Count >= 5)
                    {
                        // Bulk
                        await ConversationService.MarkAllAsReadAsync(selectedConversationId.Value);
                    }
                    else
                    {
                        // Individual (paralel)
                        var markTasks = unreadMessages.Select(message =>
                            ConversationService.MarkAsReadAsync(message.ConversationId, message.Id)
                        );
                        await Task.WhenAll(markTasks);
                    }

                    // UI yenilə
                    foreach (var message in unreadMessages)
                    {
                        var index = directMessages.IndexOf(message);
                        if (index >= 0)
                        {
                            directMessages[index] = message with { IsRead = true };
                        }
                    }
                    InvalidateMessageCache();
                    StateHasChanged();
                }
                catch
                {
                    // Mark as read error-ları kritik deyil
                }
            }
        }

        // CHANNEL
        else if (selectedChannelId.HasValue)
        {
            try
            {
                var unreadMessages = channelMessages.Where(m =>
                    m.SenderId != currentUserId &&
                    (m.ReadBy == null || !m.ReadBy.Contains(currentUserId))
                ).ToList();

                if (unreadMessages.Count >= 5)
                {
                    await ChannelService.MarkAsReadAsync(selectedChannelId.Value);
                }
                else if (unreadMessages.Count > 0)
                {
                    foreach (var msg in unreadMessages)
                    {
                        await ChannelService.MarkSingleMessageAsReadAsync(selectedChannelId.Value, msg.Id);
                    }
                }
                // SignalR UI-ı avtomatik yeniləyəcək
            }
            catch
            {
                // Error-ları ignore et
            }
        }
    }

    #endregion

    #region Mark As Later - Sonra oxu işarəsi

    /// <summary>
    /// Mesajı "sonra oxu" kimi işarələ.
    /// Conversation/channel-dan çıxdıqda avtomatik silinir.
    ///
    /// TOGGLE PATTERN:
    /// Eyni mesaja 2-ci dəfə click = işarəni sil.
    /// </summary>
    private async Task HandleToggleMarkAsLaterClick(Guid messageId)
    {
        try
        {
            Result result;

            if (selectedChannelId.HasValue)
            {
                result = await ChannelService.ToggleMessageAsLaterAsync(selectedChannelId.Value, messageId);
            }
            else if (selectedConversationId.HasValue)
            {
                result = await ConversationService.ToggleMessageAsLaterAsync(selectedConversationId.Value, messageId);
            }
            else
            {
                return;
            }

            if (result.IsSuccess)
            {
                // Toggle state
                if (lastReadLaterMessageId.HasValue && lastReadLaterMessageId.Value == messageId)
                {
                    // Artıq işarəli - sil
                    lastReadLaterMessageId = null;
                    lastReadLaterMessageIdOnEntry = null;
                }
                else
                {
                    // Yeni işarə
                    lastReadLaterMessageId = messageId;
                    lastReadLaterMessageIdOnEntry = null;
                    // "New messages" separator-u gizlət
                    unreadSeparatorAfterMessageId = null;
                }

                // List-i yenilə
                if (selectedChannelId.HasValue)
                {
                    var channelIndex = channelConversations.FindIndex(c => c.Id == selectedChannelId.Value);
                    if (channelIndex >= 0)
                    {
                        var currentChannel = channelConversations[channelIndex];
                        channelConversations[channelIndex] = currentChannel with { LastReadLaterMessageId = lastReadLaterMessageId };
                        channelConversations = new List<ChannelDto>(channelConversations);
                    }
                }
                else if (selectedConversationId.HasValue)
                {
                    var conversationIndex = directConversations.FindIndex(c => c.Id == selectedConversationId.Value);
                    if (conversationIndex >= 0)
                    {
                        directConversations[conversationIndex] = directConversations[conversationIndex] with { LastReadLaterMessageId = lastReadLaterMessageId };
                        directConversations = new List<DirectConversationDto>(directConversations);
                    }
                }

                StateHasChanged();
            }
            else
            {
                errorMessage = result.Error ?? "Failed to toggle read later";
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error toggling read later: {ex.Message}";
            StateHasChanged();
        }
    }

    #endregion

    #region Selection Mode - Seçim rejimi

    /// <summary>
    /// Seçim rejimini toggle et.
    /// Bir neçə mesajı seçib delete/forward etmək üçün.
    /// </summary>
    private void ToggleSelectMode()
    {
        isSelectingMessageBuble = !isSelectingMessageBuble;
        if (!isSelectingMessageBuble)
        {
            selectedMessageIds.Clear();
        }
        StateHasChanged();
    }

    /// <summary>
    /// Mesajı seçim rejimində seç/seçimi ləğv et.
    /// </summary>
    private void HandleSelectToggle(Guid messageId)
    {
        if (!isSelectingMessageBuble)
        {
            // Seçim rejiminə daxil ol
            isSelectingMessageBuble = true;
            selectedMessageIds.Add(messageId);
        }
        else
        {
            ToggleMessageSelection(messageId);
        }
        StateHasChanged();
    }

    /// <summary>
    /// Mesaj seçimini toggle et.
    /// </summary>
    private void ToggleMessageSelection(Guid messageId)
    {
        if (!selectedMessageIds.Remove(messageId))
            selectedMessageIds.Add(messageId);

        StateHasChanged();
    }

    /// <summary>
    /// Seçilmiş mesajları silmək olur?
    /// Yalnız öz mesajlarını silmək olar.
    /// </summary>
    private bool CanDeleteSelected()
    {
        if (selectedMessageIds.Count == 0)
            return false;

        if (isDirectMessage)
        {
            return directMessages
                .Where(m => selectedMessageIds.Contains(m.Id))
                .All(m => m.SenderId == currentUserId);
        }
        else
        {
            return channelMessages
                .Where(m => selectedMessageIds.Contains(m.Id))
                .All(m => m.SenderId == currentUserId);
        }
    }

    /// <summary>
    /// Seçilmiş mesajları sil.
    /// </summary>
    private async Task DeleteSelectedMessages()
    {
        if (!CanDeleteSelected())
            return;

        var messagesToDelete = selectedMessageIds.ToList();

        foreach (var messageId in messagesToDelete)
        {
            await DeleteMessage(messageId);
        }

        ToggleSelectMode();
    }

    /// <summary>
    /// Seçilmiş mesajları forward et.
    /// Hələlik yalnız ilk mesajı forward edir.
    /// </summary>
    private void ForwardSelectedMessages()
    {
        if (selectedMessageIds.Count == 0)
            return;

        var firstMessageId = selectedMessageIds.First();
        HandleForward(firstMessageId);
    }

    #endregion
}