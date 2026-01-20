using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Shared.Kernel;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Send Message - Mesaj göndər

    /// <summary>
    /// Mesaj göndərir.
    /// OPTİMİSTİC UI: Dərhal list-ə əlavə et (server cavabını gözləmədən)
    /// Conversation/Channel list-i yenilə
    /// </summary>
    private async Task SendMessage((string Message, Dictionary<string, Guid> MentionedUsers) data)
    {
        var content = data.Message;
        var mentionedUsers = data.MentionedUsers;

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

            // DM MESAJI - TRUE OPTIMISTIC UI
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var conversationId = selectedConversationId.Value;
                var messageTime = DateTime.UtcNow;
                var tempId = Guid.NewGuid();

                // Mentions-ı include et
                var optimisticMentions = mentionedUsers?.Select(m => new MessageMentionDto(m.Value, m.Key)).ToList();

                // 1. OPTIMISTIC UI - Dərhal əlavə et (Pending status)
                var pendingMessage = new DirectMessageDto(
                    tempId,
                    conversationId,
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
                    false,                                          // IsRead
                    false,                                          // IsPinned
                    0,                                              // ReactionCount
                    messageTime,                                    // CreatedAtUtc
                    null,                                           // EditedAtUtc
                    null,                                           // PinnedAtUtc
                    replyToMessageId,
                    replyToContent,
                    replyToSenderName,
                    null,                                           // ReplyToFileId
                    null,                                           // ReplyToFileName
                    null,                                           // ReplyToFileContentType
                    false,                                          // IsForwarded
                    null,                                           // Reactions
                    optimisticMentions,                             // Mentions
                    MessageStatus.Pending,                          // Status
                    tempId);                                        // TempId

                directMessages.Add(pendingMessage);

                // DUPLICATE FIX: Track pending message by TempId
                pendingDirectMessages[tempId] = pendingMessage;

                UpdateConversationLocally(conversationId, content, messageTime);
                StateHasChanged();

                // 2. BACKGROUND SEND - Retry logic ilə (fire and forget)
                _ = Task.Run(async () =>
                {
                    var realId = await SendDirectMessageWithRetry(
                        conversationId,
                        content,
                        replyToMessageId,
                        mentionedUsers,
                        tempId,
                        maxRetries: 3);

                    // Pending read receipt yoxla (race condition halı)
                    if (realId.HasValue && pendingReadReceipts.TryGetValue(realId.Value, out var _))
                    {
                        await InvokeAsync(() =>
                        {
                            pendingReadReceipts.Remove(realId.Value);
                            // FIX: IsRead və Status-u birlikdə update et
                            var message = directMessages.FirstOrDefault(m => m.Id == realId.Value);
                            if (message != null)
                            {
                                var index = directMessages.IndexOf(message);
                                directMessages[index] = message with { IsRead = true, Status = MessageStatus.Read };
                                InvalidateMessageCache();
                                StateHasChanged();
                            }
                        });
                    }
                });
            }

            // CHANNEL MESAJI - TRUE OPTIMISTIC UI
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var channelId = selectedChannelId.Value;
                var messageTime = DateTime.UtcNow;
                var tempId = Guid.NewGuid();

                // TotalMemberCount = üzvlər - sender
                var totalMembers = Math.Max(0, selectedChannelMemberCount - 1);

                // Mentions-ı include et
                var optimisticChannelMentions = mentionedUsers?.Select(m =>
                    new ChannelMessageMentionDto(m.Value, m.Key, false)).ToList();

                // 1. OPTIMISTIC UI - Dərhal əlavə et (Pending status)
                var pendingMessage = new ChannelMessageDto(
                    tempId,
                    channelId,
                    currentUserId,
                    UserState.CurrentUser?.Username ?? "",
                    UserState.CurrentUser?.DisplayName ?? "",
                    UserState.CurrentUser?.AvatarUrl,
                    content,
                    null,                                       // FileId
                    null,                                       // FileName
                    null,                                       // FileContentType
                    null,                                       // FileSizeInBytes
                    false,                                      // IsEdited
                    false,                                      // IsDeleted
                    false,                                      // IsPinned
                    0,                                          // ReactionCount
                    messageTime,                                // CreatedAtUtc
                    null,                                       // EditedAtUtc
                    null,                                       // PinnedAtUtc
                    replyToMessageId,
                    replyToContent,
                    replyToSenderName,
                    null,                                       // ReplyToFileId
                    null,                                       // ReplyToFileName
                    null,                                       // ReplyToFileContentType
                    false,                                      // IsForwarded
                    0,                                          // ReadByCount
                    totalMembers,                               // TotalMemberCount
                    [],                                         // ReadBy
                    [],                                         // Reactions
                    optimisticChannelMentions,                  // Mentions
                    MessageStatus.Pending,                      // Status
                    tempId);                                    // TempId

                channelMessages.Add(pendingMessage);

                // DUPLICATE FIX: Track pending message by TempId
                pendingChannelMessages[tempId] = pendingMessage;

                UpdateChannelLocally(channelId, content, messageTime, UserState.CurrentUser?.DisplayName);
                StateHasChanged();

                // 2. BACKGROUND SEND - Retry logic ilə (fire and forget)
                _ = Task.Run(async () =>
                {
                    await SendChannelMessageWithRetry(
                        channelId,
                        content,
                        replyToMessageId,
                        mentionedUsers,
                        tempId,
                        maxRetries: 3);
                });
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

    // DEBOUNCE FIX: DirectMessage mark-as-read debounce timer
    private System.Threading.Timer? _markDMAsReadDebounceTimer;
    private readonly object _markDMAsReadLock = new object();
    private Guid? _pendingConversationId;
    private List<DirectMessageDto>? _pendingUnreadDMMessages;

    /// <summary>
    /// Direct Message-ları mark-as-read edir (bulk və ya individual API).
    /// DEBOUNCE FIX: 500ms debounce - paralel request-ləri prevent edir.
    /// UI state-i də yenilə.
    /// </summary>
    private async Task MarkDirectMessagesAsReadAsync(List<DirectMessageDto> unreadMessages)
    {
        if (unreadMessages.Count == 0 || !selectedConversationId.HasValue) return;

        // DEBOUNCE FIX: Prevent concurrent mark-as-read requests
        lock (_markDMAsReadLock)
        {
            _pendingConversationId = selectedConversationId;
            _pendingUnreadDMMessages = unreadMessages;

            // Reset timer - yalnız 500ms sonra request göndər
            _markDMAsReadDebounceTimer?.Dispose();
            _markDMAsReadDebounceTimer = new Timer(async _ =>
            {
                // FIX: Check disposed before processing (timer may fire after component disposal)
                if (_disposed) return;

                Guid? conversationId;
                List<DirectMessageDto>? messages;

                lock (_markDMAsReadLock)
                {
                    conversationId = _pendingConversationId;
                    messages = _pendingUnreadDMMessages;
                    _pendingConversationId = null;
                    _pendingUnreadDMMessages = null;
                }

                if (conversationId.HasValue && messages != null && messages.Count > 0)
                {
                    try
                    {
                        // API call (5+ bulk, <5 individual)
                        if (messages.Count >= 5)
                        {
                            await ConversationService.MarkAllAsReadAsync(conversationId.Value);
                        }
                        else
                        {
                            await Task.WhenAll(messages.Select(msg =>
                                ConversationService.MarkAsReadAsync(conversationId.Value, msg.Id)
                            ));
                        }

                        // UI state update
                        if (_disposed) return;
                        await InvokeAsync(() =>
                        {
                            if (_disposed) return;
                            foreach (var message in messages)
                            {
                                var index = directMessages.IndexOf(message);
                                if (index >= 0)
                                {
                                    directMessages[index] = message with { IsRead = true };
                                }
                            }
                            InvalidateMessageCache();
                            StateHasChanged();
                        });
                    }
                    catch
                    {
                        // Silently handle mark-as-read errors
                    }
                }
            }, null, 500, Timeout.Infinite);
        }
    }

    // DEADLOCK FIX: Channel mark-as-read debounce timer
    private System.Threading.Timer? _markAsReadDebounceTimer;
    private readonly object _markAsReadLock = new object();
    private Guid? _pendingChannelId;
    private List<ChannelMessageDto>? _pendingUnreadMessages;

    /// <summary>
    /// Channel mesajlarını mark-as-read edir (bulk və ya individual API).
    /// DEADLOCK FIX: 500ms debounce - paralel request-ləri prevent edir.
    /// SignalR UI-ı update edəcək.
    /// </summary>
    private async Task MarkChannelMessagesAsReadAsync(List<ChannelMessageDto> unreadMessages)
    {
        if (unreadMessages.Count == 0 || !selectedChannelId.HasValue) return;

        // DEADLOCK FIX: Debounce mark-as-read requests
        // Prevents concurrent API calls that cause PostgreSQL index deadlock
        lock (_markAsReadLock)
        {
            _pendingChannelId = selectedChannelId;
            _pendingUnreadMessages = unreadMessages;

            // Reset timer - yalnız 500ms sonra request göndər
            _markAsReadDebounceTimer?.Dispose();
            _markAsReadDebounceTimer = new System.Threading.Timer(async _ =>
            {
                // FIX: Check disposed before processing (timer may fire after component disposal)
                if (_disposed) return;

                Guid? channelId;
                List<ChannelMessageDto>? messages;

                lock (_markAsReadLock)
                {
                    channelId = _pendingChannelId;
                    messages = _pendingUnreadMessages;
                    _pendingChannelId = null;
                    _pendingUnreadMessages = null;
                }

                if (channelId.HasValue && messages != null && messages.Count > 0)
                {
                    try
                    {
                        // API call (5+ bulk, <5 individual)
                        if (messages.Count >= 5)
                        {
                            await ChannelService.MarkAsReadAsync(channelId.Value);
                        }
                        else
                        {
                            foreach (var msg in messages)
                            {
                                if (_disposed) return;
                                await ChannelService.MarkSingleMessageAsReadAsync(channelId.Value, msg.Id);
                            }
                        }
                        // SignalR event-i UI-ı avtomatik yeniləyəcək
                    }
                    catch
                    {
                        // Silently handle mark-as-read errors
                    }
                }
            }, null, 500, Timeout.Infinite);
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

    #region Mention Actions - Mention-a klik

    /// <summary>
    /// Mention-a klik edildikdə həmin şəxslə conversation aç.
    /// Özünə mention etsə, Notes conversation-u aç.
    /// </summary>
    private async Task HandleMentionClick(Guid userId)
    {
        // Özünə mention etsə, Notes conversation-u aç
        if (userId == currentUserId)
        {
            var notesConversation = directConversations.FirstOrDefault(c => c.IsNotes);
            if (notesConversation != null)
            {
                // Use SelectDirectConversation for proper state management + scroll
                await SelectDirectConversation(notesConversation);
            }
            return;
        }

        try
        {
            // Start conversation with this user

            // Check if conversation already exists in the list (OtherUserId match)
            var existingConversation = directConversations.FirstOrDefault(c => c.OtherUserId == userId);

            if (existingConversation != null)
            {
                // Conversation exists, use SelectDirectConversation for proper state management + scroll
                await SelectDirectConversation(existingConversation);
            }
            else
            {
                // Start new conversation (pending conversation until first message)
                // Backend StartConversationAsync artıq user yoxlamasını edir
                var result = await ConversationService.StartConversationAsync(userId);

                if (result.IsSuccess && result.Value != Guid.Empty)
                {
                    // Reload conversation list (yeni conversation əlavə olunub)
                    await LoadConversationsAndChannels();

                    // Find the new conversation
                    var newConversation = directConversations.FirstOrDefault(c => c.Id == result.Value);
                    if (newConversation != null)
                    {
                        // Use SelectDirectConversation for proper state management + scroll
                        await SelectDirectConversation(newConversation);
                    }
                }
                else
                {
                    // Backend error (user not found, silinib, və s.)
                    errorMessage = result.Error ?? "Failed to start conversation with this user";
                    StateHasChanged();
                }
            }
        }
        catch
        {
            errorMessage = "An error occurred while opening the conversation";
            StateHasChanged();
        }
    }

    #endregion

    #region Retry Logic - Yenidən cəhd

    /// <summary>
    /// Direct Message göndərir - retry logic ilə (exponential backoff).
    /// 3 cəhd: 1s, 2s, 4s interval.
    /// Uğurlu olduqda real ID-ni qaytarır, uğursuz olduqda null.
    /// </summary>
    private async Task<Guid?> SendDirectMessageWithRetry(
        Guid conversationId,
        string content,
        Guid? replyToMessageId,
        Dictionary<string, Guid>? mentionedUsers,
        Guid tempId,
        int maxRetries)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await ConversationService.SendMessageAsync(
                    conversationId,
                    content,
                    fileId: null,
                    replyToMessageId: replyToMessageId,
                    isForwarded: false,
                    mentionedUsers: mentionedUsers);

                if (result.IsSuccess)
                {
                    var realId = result.Value;

                    // Update: tempId → realId, Status → Sent
                    // IMPORTANT: Keep TempId so SignalR can find and replace this message
                    await InvokeAsync(() =>
                    {
                        var message = directMessages.FirstOrDefault(m => m.TempId == tempId);
                        if (message != null)
                        {
                            var index = directMessages.IndexOf(message);
                            // FIX: Preserve "Read" status if already set (race condition protection)
                            var newStatus = message.Status == MessageStatus.Read ? MessageStatus.Read : MessageStatus.Sent;
                            var updatedMessage = message with
                            {
                                Id = realId,
                                Status = newStatus
                                // Keep TempId - SignalR will clear it when replacing
                            };
                            directMessages[index] = updatedMessage;
                            InvalidateMessageCache();
                            StateHasChanged();
                        }
                    });

                    return realId;
                }
            }
            catch
            {
                // Retry on failure
            }

            // Son cəhd deyilsə, gözlə (exponential backoff)
            if (attempt < maxRetries)
            {
                var delayMs = (int)Math.Pow(2, attempt - 1) * 1000; // 1s, 2s, 4s
                await Task.Delay(delayMs);
            }
        }

        // Bütün cəhdlər uğursuz - Status → Failed
        await InvokeAsync(() =>
        {
            var message = directMessages.FirstOrDefault(m => m.TempId == tempId);
            if (message != null)
            {
                var index = directMessages.IndexOf(message);
                var failedMessage = message with { Status = MessageStatus.Failed };
                directMessages[index] = failedMessage;
                InvalidateMessageCache();
                StateHasChanged();
            }
        });

        return null;
    }

    /// <summary>
    /// Channel Message göndərir - retry logic ilə (exponential backoff).
    /// 3 cəhd: 1s, 2s, 4s interval.
    /// Uğurlu olduqda real ID-ni qaytarır, uğursuz olduqda null.
    /// </summary>
    private async Task<Guid?> SendChannelMessageWithRetry(
        Guid channelId,
        string content,
        Guid? replyToMessageId,
        Dictionary<string, Guid>? mentionedUsers,
        Guid tempId,
        int maxRetries)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await ChannelService.SendMessageAsync(
                    channelId,
                    content,
                    fileId: null,
                    replyToMessageId: replyToMessageId,
                    isForwarded: false,
                    mentionedUsers: mentionedUsers);

                if (result.IsSuccess)
                {
                    var realId = result.Value;

                    // Update: tempId → realId, Status → Sent
                    // IMPORTANT: Keep TempId so SignalR can find and replace this message
                    await InvokeAsync(() =>
                    {
                        var message = channelMessages.FirstOrDefault(m => m.TempId == tempId);
                        if (message != null)
                        {
                            var index = channelMessages.IndexOf(message);
                            // FIX: Preserve "Read"/"Delivered" status if already set (race condition protection)
                            var newStatus = (message.Status == MessageStatus.Read || message.Status == MessageStatus.Delivered)
                                ? message.Status
                                : MessageStatus.Sent;
                            var updatedMessage = message with
                            {
                                Id = realId,
                                Status = newStatus
                                // Keep TempId - SignalR will clear it when replacing
                            };
                            channelMessages[index] = updatedMessage;
                            InvalidateMessageCache();
                            StateHasChanged();
                        }
                    });

                    return realId;
                }
            }
            catch
            {
                // Retry on failure
            }

            // Son cəhd deyilsə, gözlə (exponential backoff)
            if (attempt < maxRetries)
            {
                var delayMs = (int)Math.Pow(2, attempt - 1) * 1000; // 1s, 2s, 4s
                await Task.Delay(delayMs);
            }
        }

        // Bütün cəhdlər uğursuz - Status → Failed
        await InvokeAsync(() =>
        {
            var message = channelMessages.FirstOrDefault(m => m.TempId == tempId);
            if (message != null)
            {
                var index = channelMessages.IndexOf(message);
                var failedMessage = message with { Status = MessageStatus.Failed };
                channelMessages[index] = failedMessage;
                InvalidateMessageCache();
                StateHasChanged();
            }
        });

        return null;
    }

    #endregion
}