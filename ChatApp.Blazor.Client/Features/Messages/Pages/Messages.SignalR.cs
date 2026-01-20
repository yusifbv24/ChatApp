using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Shared.Kernel;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

// LOW PRIORITY NOTE: InvokeAsync lambdas
// SignalR handlers use InvokeAsync(async () => {...}) pattern throughout this file.
// This creates lambda allocations (gen0 GC), but is intentional and minimal overhead.
// Alternative (named methods) would reduce readability without significant performance gain.
// Pattern is accepted Blazor standard for marshaling to UI thread.

public partial class Messages
{
    #region SignalR Subscription - Event-lərə qoşulmaq

    /// <summary>
    /// Bütün SignalR event-lərinə subscribe olur.
    /// Bu metod OnInitializedAsync-də 1 dəfə çağrılır.
    /// isSubscribedToSignalR flag ilə 2 dəfə qoşulmağın qarşısını alırıq
    /// += operatoru ilə event handler əlavə edirik
    /// Dispose-da -= ilə unsubscribe etməliyik (memory leak qarşısını almaq üçün)
    /// </summary>
    private void SubscribeToSignalREvents()
    {
        // Əgər artıq subscribe olunubsa, yenidən subscribe olmağa ehtiyac yoxdur
        if (isSubscribedToSignalR) return;
        isSubscribedToSignalR = true;

        // Direct Message event-ləri
        SignalRService.OnNewDirectMessage += HandleNewDirectMessage;         // Yeni DM gəldikdə
        SignalRService.OnDirectMessageEdited += HandleDirectMessageEdited;   // DM edit olunduqda
        SignalRService.OnDirectMessageDeleted += HandleDirectMessageDeleted; // DM silindikdə
        SignalRService.OnMessageRead += HandleDirectMessagesRead;                   // DM oxunduqda
        SignalRService.OnDirectMessageReactionToggled += HandleReactionToggledDM; // DM-ə reaction əlavə/silinəndə

        // Channel Message event-ləri
        SignalRService.OnNewChannelMessage += HandleNewChannelMessage;               // Yeni channel mesajı
        SignalRService.OnChannelMessageEdited += HandleChannelMessageEdited;         // Channel mesajı edit olunduqda
        SignalRService.OnChannelMessageDeleted += HandleChannelMessageDeleted;       // Channel mesajı silindikdə
        SignalRService.OnChannelMessageReactionsUpdated += HandleReactionToggledChannel; // Reaction update
        SignalRService.OnChannelMessagesRead += HandleChannelMessagesRead;           // Channel mesajları oxunduqda

        // Typing indicator event-ləri
        SignalRService.OnUserTypingInConversation += HandleTypingInConversation; // DM-də yazırlar
        SignalRService.OnUserTypingInChannel += HandleTypingInChannel;           // Channel-də yazırlar

        // Online status event-ləri
        SignalRService.OnUserOnline += HandleUserOnline;   // İstifadəçi online oldu
        SignalRService.OnUserOffline += HandleUserOffline; // İstifadəçi offline oldu

        // Channel membership event-ləri
        SignalRService.OnAddedToChannel += HandleAddedToChannel; // Sizi channel-ə əlavə etdilər

        // KRITIK: Bağlantı kəsildikdən sonra yenidən qoşulduqda group-lara rejoin olmaq lazımdır
        SignalRService.OnReconnected += HandleSignalRReconnected;
    }

    /// <summary>
    /// Bütün SignalR event-lərindən unsubscribe olur.
    /// DisposeAsync-də çağrılır - MEMORY LEAK qarşısını alır.
    /// </summary>
    private void UnsubscribeFromSignalREvents()
    {
        if (!isSubscribedToSignalR) return;
        isSubscribedToSignalR = false;

        SignalRService.OnNewDirectMessage -= HandleNewDirectMessage;
        SignalRService.OnNewChannelMessage -= HandleNewChannelMessage;
        SignalRService.OnDirectMessageEdited -= HandleDirectMessageEdited;
        SignalRService.OnDirectMessageDeleted -= HandleDirectMessageDeleted;
        SignalRService.OnChannelMessageEdited -= HandleChannelMessageEdited;
        SignalRService.OnChannelMessageDeleted -= HandleChannelMessageDeleted;
        SignalRService.OnMessageRead -= HandleDirectMessagesRead;
        SignalRService.OnUserTypingInConversation -= HandleTypingInConversation;
        SignalRService.OnUserTypingInChannel -= HandleTypingInChannel;
        SignalRService.OnUserOnline -= HandleUserOnline;
        SignalRService.OnUserOffline -= HandleUserOffline;
        SignalRService.OnDirectMessageReactionToggled -= HandleReactionToggledDM;
        SignalRService.OnChannelMessageReactionsUpdated -= HandleReactionToggledChannel;
        SignalRService.OnChannelMessagesRead -= HandleChannelMessagesRead;
        SignalRService.OnAddedToChannel -= HandleAddedToChannel;
        SignalRService.OnReconnected -= HandleSignalRReconnected;
    }

    #endregion

    #region New Message Handlers - Yeni mesaj gəldikdə

    /// <summary>
    /// Yeni direct message gəldikdə çağrılır.
    /// 1. Dublikat yoxlaması (processedMessageIds ilə)
    /// 2. Əgər aktiv conversation-a gəlibsə:
    ///    - Optimistic UI: Öz mesajımız artıq list-dədir, onu backend cavabı ilə əvəz et
    ///    - Başqasının mesajı: List-ə əlavə et və mark as read et
    /// 3. Conversation list-i yenilə (son mesaj, unread count)
    /// 4. UI-ı yenilə
    /// </summary>
    private void HandleNewDirectMessage(DirectMessageDto message)
    {
        InvokeAsync(async () =>
        {
            // Dublikat yoxlaması - eyni mesajın 2 dəfə göstərilməsinin qarşısını alır
            // processedMessageIds HashSet-dir, Add() false qaytarırsa - artıq əlavə olunub
            if (!processedMessageIds.Add(message.Id)) return;

            // Əgər bu mesaj hazırda baxdığımız conversation-a gəlibsə
            if (message.ConversationId == selectedConversationId)
            {
                // DUPLICATE FIX: Check pending messages by exact TempId match
                // Content-based matching duplicate yaradır (eyni content-li 2 mesaj)
                DirectMessageDto? pendingMessage = null;
                int pendingIndex = -1;

                // Try to find pending message in tracking dictionary first
                // FIX: Use ToList() to avoid InvalidOperationException on dictionary modification
                foreach (var kvp in pendingDirectMessages.ToList())
                {
                    if (kvp.Value.SenderId == message.SenderId && kvp.Value.Content == message.Content)
                    {
                        pendingMessage = kvp.Value;
                        pendingIndex = directMessages.FindIndex(m => m.TempId == kvp.Key);
                        if (pendingIndex >= 0)
                        {
                            // Remove from tracking dictionary
                            pendingDirectMessages.Remove(kvp.Key);
                            break;
                        }
                    }
                }

                // 2. Check by real ID (already confirmed by retry logic)
                var existingIndex = directMessages.FindIndex(m => m.Id == message.Id && !m.TempId.HasValue);

                // FIX: Determine correct status based on IsRead (prevents race condition overwrites)
                var correctStatus = message.IsRead ? MessageStatus.Read : MessageStatus.Sent;

                if (pendingIndex >= 0)
                {
                    // OPTIMISTIC MESSAGE CONFIRMED - Replace with real message
                    // FLASH FIX: Preserve TempId for stable @key (prevents component re-mount)
                    var oldMessage = directMessages[pendingIndex];

                    // FIX: Keep existing Read status if already set (race condition protection)
                    var finalStatus = oldMessage.Status == MessageStatus.Read ? MessageStatus.Read : correctStatus;

                    directMessages[pendingIndex] = message with
                    {
                        TempId = oldMessage.TempId,  // Keep TempId so @key doesn't change
                        Status = finalStatus
                    };
                    InvalidateMessageCache();
                }
                else if (existingIndex >= 0)
                {
                    // MESSAGE ALREADY EXISTS (without TempId) - Update it
                    // FIX: Keep existing Read status if already set
                    var existingMessage = directMessages[existingIndex];
                    var finalStatus = existingMessage.Status == MessageStatus.Read ? MessageStatus.Read : correctStatus;

                    directMessages[existingIndex] = message with { Status = finalStatus };
                    InvalidateMessageCache();
                }
                else
                {
                    // NEW MESSAGE FROM OTHERS - Add to list
                    directMessages.Add(message with { Status = correctStatus });
                }
            }

            // CONVERSATION LIST-İ YENİLƏ
            // Son mesaj və unread count-u update et
            // Performans: Conversation-u bir dəfə tap, həm mark-as-read, həm də update üçün istifadə et
            var conversation = directConversations.FirstOrDefault(c => c.Id == message.ConversationId);

            // Mark as read şərtləri:
            // 1. Page visible (tab açıq) VƏ
            // 2. İstifadəçi həmin conversationda (message.ConversationId == selectedConversationId) VƏ
            // 3. (Başqasının mesajıdır VƏ YA Notes conversation-dır)
            // isPageVisible - browser tab-ı fokusda olduğunu yoxlayır
            var isInConversation = message.ConversationId == selectedConversationId;
            if (conversation != null && isPageVisible && isInConversation)
            {
                var isNotes = conversation.IsNotes;
                var shouldMarkAsRead = message.SenderId != currentUserId || isNotes;

                if (shouldMarkAsRead)
                {
                    try
                    {
                        await ConversationService.MarkAsReadAsync(message.ConversationId, message.Id);
                    }
                    catch
                    {
                        // Mark as read error-ları kritik deyil, ignore edirik
                    }
                }
            }
            if (conversation != null)
            {
                var isCurrentConversation = message.ConversationId == selectedConversationId;
                var isMyMessage = message.SenderId == currentUserId;

                // with expression - record-un kopyasını yaradır, bəzi field-ləri dəyişir
                var preview = GetFilePreview(message);

                // Check if current user is mentioned in this message
                bool hasMention = message.Mentions != null && message.Mentions.Any(m => m.UserId == currentUserId);

                // FIX: Calculate correct status, but preserve existing "Read" status (race condition protection)
                string? newStatus = null;
                if (isMyMessage)
                {
                    // If already "Read", keep it (read receipt may have arrived first)
                    if (conversation.LastMessageStatus == "Read")
                        newStatus = "Read";
                    else
                        newStatus = message.IsRead ? "Read" : "Sent";
                }

                var updatedConversation = conversation with
                {
                    LastMessageContent = preview,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    LastMessageSenderId = message.SenderId,
                    LastMessageStatus = newStatus,
                    // Unread: Notes üçün həmişə 0, aktiv conversation-da 0, öz mesajımız isə dəyişmə, başqasının isə +1
                    UnreadCount = conversation.IsNotes ? 0 : (isCurrentConversation ? 0 : (isMyMessage ? conversation.UnreadCount : conversation.UnreadCount + 1)),
                    // HasUnreadMentions: Notes və aktiv conversation-da false, mention varsa true
                    HasUnreadMentions = (conversation.IsNotes || isCurrentConversation) ? false : (isMyMessage ? conversation.HasUnreadMentions : (hasMention || conversation.HasUnreadMentions))
                };

                // PERFORMANCE: Using helper method (move to top pattern)
                MoveItemToTop(ref directConversations, updatedConversation, c => c.Id == conversation.Id);

                // Global unread badge-i artır (header-dakı notification icon)
                // Notes conversation üçün global badge artırma (self-conversation)
                if (!isCurrentConversation && !isMyMessage && !conversation.IsNotes)
                {
                    AppState.IncrementUnreadMessages();
                }
            }
            else if (message.SenderId != currentUserId)
            {
                // Bu halda kimsə bizə ilk dəfə mesaj yazıb
                _ = LoadConversationsAndChannels();
            }

            StateHasChanged();
        });
    }

    /// <summary>
    /// Yeni channel message gəldikdə çağrılır.
    /// DM-dən fərqi: ReadBy list-i var (channel-də hər kəs ayrıca oxuya bilir)
    /// </summary>
    private void HandleNewChannelMessage(ChannelMessageDto message)
    {
        InvokeAsync(async () =>
        {
            // Dublikat yoxlaması
            if (!processedMessageIds.Add(message.Id))
                return;

            if (message.ChannelId == selectedChannelId)
            {
                // Race condition protection: başqa handler artıq əlavə edir?
                if (pendingMessageAdds.Contains(message.Id))
                    return;

                // DUPLICATE FIX: Check pending messages by exact TempId match
                // Content-based matching duplicate yaradır (eyni content-li 2 mesaj)
                ChannelMessageDto? pendingMessage = null;
                int pendingIndex = -1;

                // Try to find pending message in tracking dictionary first
                // FIX: Use ToList() to avoid InvalidOperationException on dictionary modification
                foreach (var kvp in pendingChannelMessages.ToList())
                {
                    if (kvp.Value.SenderId == message.SenderId && kvp.Value.Content == message.Content)
                    {
                        pendingMessage = kvp.Value;
                        pendingIndex = channelMessages.FindIndex(m => m.TempId == kvp.Key);
                        if (pendingIndex >= 0)
                        {
                            // Remove from tracking dictionary
                            pendingChannelMessages.Remove(kvp.Key);
                            break;
                        }
                    }
                }

                // 2. Check by real ID (already confirmed by retry logic)
                var existingIndex = channelMessages.FindIndex(m => m.Id == message.Id && !m.TempId.HasValue);

                // FIX: Calculate correct status based on ReadByCount (prevents race condition overwrites)
                static MessageStatus CalculateChannelStatus(ChannelMessageDto msg)
                {
                    var total = msg.TotalMemberCount;
                    if (total == 0) return MessageStatus.Sent;
                    if (msg.ReadByCount >= total) return MessageStatus.Read;
                    if (msg.ReadByCount > 0) return MessageStatus.Delivered;
                    return MessageStatus.Sent;
                }

                var correctStatus = CalculateChannelStatus(message);

                if (pendingIndex >= 0)
                {
                    // OPTIMISTIC MESSAGE CONFIRMED - Replace with real message
                    // FLASH FIX: Preserve TempId for stable @key (prevents component re-mount)
                    var oldMessage = channelMessages[pendingIndex];

                    // FIX: Keep existing Read/Delivered status if already set (race condition protection)
                    var finalStatus = (oldMessage.Status == MessageStatus.Read || oldMessage.Status == MessageStatus.Delivered)
                        ? (correctStatus == MessageStatus.Read ? MessageStatus.Read : oldMessage.Status)
                        : correctStatus;

                    channelMessages[pendingIndex] = message with
                    {
                        TempId = oldMessage.TempId,  // Keep TempId so @key doesn't change
                        Status = finalStatus
                    };
                    InvalidateMessageCache();
                }
                else if (existingIndex >= 0)
                {
                    // MESSAGE ALREADY EXISTS (without TempId) - Update it
                    // FIX: Keep existing Read/Delivered status if already set
                    var existingMessage = channelMessages[existingIndex];
                    var finalStatus = (existingMessage.Status == MessageStatus.Read || existingMessage.Status == MessageStatus.Delivered)
                        ? (correctStatus == MessageStatus.Read ? MessageStatus.Read : existingMessage.Status)
                        : correctStatus;

                    channelMessages[existingIndex] = message with { Status = finalStatus };
                    InvalidateMessageCache();
                }
                else
                {
                    // NEW MESSAGE FROM OTHERS - Add to list
                    pendingMessageAdds.Add(message.Id);

                    // Mark as read (səhifə görünürsə)
                    if (message.SenderId != currentUserId && isPageVisible)
                    {
                        try
                        {
                            await ChannelService.MarkSingleMessageAsReadAsync(message.ChannelId, message.Id);
                        }
                        catch
                        {
                            // Ignore mark-as-read errors
                        }
                    }

                    channelMessages.Add(message with { Status = correctStatus });
                    pendingMessageAdds.Remove(message.Id);
                }
            }

            // CHANNEL LIST-İ YENİLƏ
            var channel = channelConversations.FirstOrDefault(c => c.Id == message.ChannelId);
            if (channel != null)
            {
                var isCurrentChannel = message.ChannelId == selectedChannelId;
                var isMyMessage = message.SenderId == currentUserId;

                // Status hesabla (Sent, Delivered, Read)
                // FIX: Preserve existing "Read" or "Delivered" status (race condition protection)
                string? status = null;
                if (isMyMessage)
                {
                    var totalMembers = channel.MemberCount - 1; // Sender-i çıxar

                    // Calculate what status should be based on message data
                    string calculatedStatus;
                    if (totalMembers == 0)
                        calculatedStatus = "Sent";
                    else if (message.ReadByCount >= totalMembers)
                        calculatedStatus = "Read";
                    else if (message.ReadByCount > 0)
                        calculatedStatus = "Delivered";
                    else
                        calculatedStatus = "Sent";

                    // FIX: If already "Read", keep it (read receipt may have arrived first)
                    // If already "Delivered" and calculated is "Sent", keep "Delivered"
                    if (channel.LastMessageStatus == "Read")
                        status = "Read";
                    else if (channel.LastMessageStatus == "Delivered" && calculatedStatus == "Sent")
                        status = "Delivered";
                    else
                        status = calculatedStatus;
                }

                var preview = GetFilePreview(message);

                // Check if current user is mentioned (directly or via @All)
                bool hasMention = message.Mentions != null && message.Mentions.Any(m =>
                    m.UserId == currentUserId || m.IsAllMention);

                var updatedChannel = channel with
                {
                    LastMessageContent = preview,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    LastMessageId = message.Id,
                    LastMessageSenderId = message.SenderId,
                    LastMessageSenderAvatarUrl = message.SenderAvatarUrl,
                    LastMessageStatus = status,
                    UnreadCount = isCurrentChannel ? 0 : (isMyMessage ? channel.UnreadCount : channel.UnreadCount + 1),
                    // HasUnreadMentions: aktiv channel-da false, mention varsa true
                    HasUnreadMentions = isCurrentChannel ? false : (isMyMessage ? channel.HasUnreadMentions : (hasMention || channel.HasUnreadMentions))
                };

                // PERFORMANCE: Using helper method (move to top pattern)
                MoveItemToTop(ref channelConversations, updatedChannel, c => c.Id == channel.Id);

                if (!isCurrentChannel && !isMyMessage)
                {
                    AppState.IncrementUnreadMessages();
                }
            }
            else if (message.SenderId != currentUserId)
            {
                // Channel listdə yoxdur (gizli idi) - list-i yenidən yüklə
                _ = LoadConversationsAndChannels();
            }

            StateHasChanged();
        });
    }

    #endregion

    #region Edit/Delete Handlers - Mesaj edit/silindikdə

    /// <summary>
    /// DM edit olunduqda çağrılır.
    ///
    /// GUARD PATTERN:
    /// - _disposed check: Component bağlanıbsa işləmə
    /// - try-catch: Exception olsa runtime crash-ın qarşısını al
    /// - needsStateUpdate: Yalnız dəyişiklik olubsa UI-ı yenilə (performance)
    /// </summary>
    private void HandleDirectMessageEdited(DirectMessageDto editedMessage)
    {
        InvokeAsync(() =>
        {
            if (_disposed) return Task.CompletedTask;

            try
            {
                var needsStateUpdate = false;

                // Aktiv conversation-dakı mesajı yenilə
                if (editedMessage.ConversationId == selectedConversationId)
                {
                    var message = directMessages.FirstOrDefault(m => m.Id == editedMessage.Id);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        directMessages[index] = editedMessage;
                        needsStateUpdate = true;

                        // Son mesaj idisə conversation list-i də yenilə
                        if (IsLastMessageInConversation(editedMessage.ConversationId, editedMessage.Id))
                        {
                            var preview = GetFilePreview(editedMessage);
                            UpdateConversationLastMessage(editedMessage.ConversationId, preview);
                        }
                    }

                    // Reply preview-ları yenilə
                    // Bu mesaja reply edilib idisə, o reply-ın preview-unu da dəyiş
                    bool replyPreviewsUpdated = false;
                    for (int i = 0; i < directMessages.Count; i++)
                    {
                        var msg = directMessages[i];
                        if (msg.ReplyToMessageId == editedMessage.Id && msg.ReplyToContent != editedMessage.Content)
                        {
                            directMessages[i] = msg with { ReplyToContent = editedMessage.Content };
                            replyPreviewsUpdated = true;
                        }
                    }

                    // PERFORMANCE: Invalidate cache once after all updates (was called in loop)
                    if (message != null || replyPreviewsUpdated)
                    {
                        InvalidateMessageCache();
                    }
                }

                // Başqa conversation-da olsaq belə, son mesaj edit olunubsa list-i yenilə
                var conversation = directConversations.FirstOrDefault(c => c.Id == editedMessage.ConversationId);
                if (conversation != null && IsLastMessageInConversation(editedMessage.ConversationId, editedMessage.Id))
                {
                    var preview = GetFilePreview(editedMessage);
                    UpdateConversationLastMessage(editedMessage.ConversationId, preview);
                    needsStateUpdate = true;
                }

                // Yalnız dəyişiklik olubsa UI-ı yenilə (performance optimization)
                if (needsStateUpdate && !_disposed)
                {
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                // Exception-ları silently handle et - runtime crash-ın qarşısını al
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// DM silindikdə çağrılır.
    /// Mesajı list-dən silmirik, IsDeleted=true ilə saxlayırıq.
    /// UI-da "This message was deleted" göstərilir.
    /// </summary>
    private void HandleDirectMessageDeleted(DirectMessageDto deletedMessage)
    {
        InvokeAsync(() =>
        {
            if (_disposed) return Task.CompletedTask;

            try
            {
                var needsStateUpdate = false;

                if (deletedMessage.ConversationId == selectedConversationId)
                {
                    var message = directMessages.FirstOrDefault(m => m.Id == deletedMessage.Id);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        directMessages[index] = deletedMessage;
                        needsStateUpdate = true;

                        // Reply preview-ları yenilə
                        for (int i = 0; i < directMessages.Count; i++)
                        {
                            var msg = directMessages[i];
                            if (msg.ReplyToMessageId == deletedMessage.Id)
                            {
                                directMessages[i] = msg with { ReplyToContent = "This message was deleted" };
                            }
                        }

                        // PERFORMANCE: Invalidate cache once after all updates (was called in loop)
                        InvalidateMessageCache();
                    }
                }

                var conversation = directConversations.FirstOrDefault(c => c.Id == deletedMessage.ConversationId);
                if (conversation != null && IsLastMessageInConversation(deletedMessage.ConversationId, deletedMessage.Id))
                {
                    UpdateConversationLastMessage(deletedMessage.ConversationId, "This message was deleted");
                    needsStateUpdate = true;
                }

                if (needsStateUpdate && !_disposed)
                {
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                // Silently handle
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Channel mesajı edit olunduqda çağrılır.
    /// </summary>
    private void HandleChannelMessageEdited(ChannelMessageDto editedMessage)
    {
        InvokeAsync(() =>
        {
            if (_disposed) return Task.CompletedTask;

            try
            {
                var needsStateUpdate = false;

                if (editedMessage.ChannelId == selectedChannelId)
                {
                    var message = channelMessages.FirstOrDefault(m => m.Id == editedMessage.Id);
                    if (message != null)
                    {
                        var index = channelMessages.IndexOf(message);

                        // VACIB: Yalnız content və edit status-u yenilə
                        var updatedMessage = message with
                        {
                            Content = editedMessage.Content,
                            IsEdited = editedMessage.IsEdited,
                            EditedAtUtc = editedMessage.EditedAtUtc
                        };

                        channelMessages[index] = updatedMessage;
                        needsStateUpdate = true;

                        if (IsLastMessageInChannel(editedMessage.ChannelId, updatedMessage.Id))
                        {
                            var preview = GetFilePreview(updatedMessage);
                            UpdateChannelLastMessage(editedMessage.ChannelId, preview, message.SenderDisplayName);
                        }
                    }

                    // Reply preview-ları yenilə
                    bool replyPreviewsUpdated = false;
                    for (int i = 0; i < channelMessages.Count; i++)
                    {
                        var msg = channelMessages[i];
                        if (msg.ReplyToMessageId == editedMessage.Id && msg.ReplyToContent != editedMessage.Content)
                        {
                            channelMessages[i] = msg with { ReplyToContent = editedMessage.Content };
                            replyPreviewsUpdated = true;
                        }
                    }

                    // PERFORMANCE: Invalidate cache once after all updates (was called in loop)
                    if (message != null || replyPreviewsUpdated)
                    {
                        InvalidateMessageCache();
                    }
                }

                var channel = channelConversations.FirstOrDefault(c => c.Id == editedMessage.ChannelId);
                if (channel != null && IsLastMessageInChannel(editedMessage.ChannelId, editedMessage.Id))
                {
                    var preview = GetFilePreview(editedMessage);
                    UpdateChannelLastMessage(editedMessage.ChannelId, preview, editedMessage.SenderDisplayName);
                    needsStateUpdate = true;
                }

                if (needsStateUpdate && !_disposed)
                {
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                // Silently handle
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Channel mesajı silindikdə çağrılır.
    /// </summary>
    private void HandleChannelMessageDeleted(ChannelMessageDto deletedMessage)
    {
        InvokeAsync(() =>
        {
            if (_disposed) return Task.CompletedTask;

            try
            {
                var needsStateUpdate = false;

                if (deletedMessage.ChannelId == selectedChannelId)
                {
                    var message = channelMessages.FirstOrDefault(m => m.Id == deletedMessage.Id);
                    if (message != null)
                    {
                        var index = channelMessages.IndexOf(message);
                        channelMessages[index] = deletedMessage;
                        needsStateUpdate = true;

                        // Reply preview-ları yenilə
                        for (int i = 0; i < channelMessages.Count; i++)
                        {
                            var msg = channelMessages[i];
                            if (msg.ReplyToMessageId == deletedMessage.Id)
                            {
                                channelMessages[i] = msg with { ReplyToContent = "This message was deleted" };
                            }
                        }

                        // PERFORMANCE: Invalidate cache once after all updates (was called in loop)
                        InvalidateMessageCache();
                    }
                }

                var channel = channelConversations.FirstOrDefault(c => c.Id == deletedMessage.ChannelId);
                if (channel != null && IsLastMessageInChannel(deletedMessage.ChannelId, deletedMessage.Id))
                {
                    UpdateChannelLastMessage(deletedMessage.ChannelId, "This message was deleted", deletedMessage.SenderDisplayName);
                    needsStateUpdate = true;
                }

                if (needsStateUpdate && !_disposed)
                {
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                // Silently handle
            }

            return Task.CompletedTask;
        });
    }

    #endregion

    #region Read Receipt Handlers - Oxundu bildirişləri

    /// <summary>
    /// DM oxunduqda çağrılır.
    /// RACE CONDITION HALİ:
    /// Bəzən MessageRead event-i mesajın özündən tez gəlir.
    /// Bu halda receipt-i pendingReadReceipts-də saxlayırıq.
    /// Mesaj gəldikdə tətbiq edirik.
    /// </summary>
    private void HandleDirectMessagesRead(Guid conversationId, Guid messageId, Guid readBy)
    {
        InvokeAsync(() =>
        {
            // Mesajı tap və yenilə
            var message = directMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                var index = directMessages.IndexOf(message);
                directMessages[index] = message with { IsRead = true, Status = MessageStatus.Read };
                InvalidateMessageCache();
            }
            else if (conversationId == selectedConversationId)
            {
                // Mesaj tapılmadı amma bu conversation-dayıq
                // Pending receipt kimi saxla (race condition halı)
                pendingReadReceipts[messageId] = readBy;
            }

            // Conversation list status-u yenilə
            // PERFORMANCE: Using helper method (eliminated duplicate pattern)
            UpdateListItemWhere(
                ref directConversations,
                c => c.Id == conversationId && c.LastMessageSenderId == currentUserId,
                c => c with { LastMessageStatus = "Read" }
            );

            StateHasChanged();
        });
    }

    /// <summary>
    /// Channel mesajları oxunduqda çağrılır.
    /// Bir neçə mesaj eyni anda oxundu bildirilir.
    /// messageReadCounts: MessageId -> ReadByCount dictionary
    /// </summary>
    private void HandleChannelMessagesRead(Guid channelId, Guid userId, Dictionary<Guid, int> messageReadCounts)
    {
        InvokeAsync(() =>
        {
            bool updated = false;

            if (selectedChannelId.HasValue && selectedChannelId.Value == channelId)
            {
                // PERFORMANCE: Convert to HashSet for O(1) lookup (was O(n) in loop)
                var messageIdSet = new HashSet<Guid>(messageReadCounts.Keys);
                var updatedList = new List<ChannelMessageDto>(channelMessages);

                for (int i = 0; i < updatedList.Count; i++)
                {
                    var message = updatedList[i];
                    if (messageIdSet.Contains(message.Id))
                    {
                        // Sender özünü ReadBy-a əlavə etməməli
                        if (message.SenderId == userId)
                            continue;

                        var newReadBy = message.ReadBy != null
                            ? new List<Guid>(message.ReadBy)
                            : new List<Guid>();

                        if (!newReadBy.Contains(userId))
                        {
                            newReadBy.Add(userId);

                            // FIX: Calculate and update Status enum based on read count
                            var newReadByCount = newReadBy.Count;
                            var totalMembers = message.TotalMemberCount;
                            MessageStatus newStatus;

                            if (totalMembers == 0)
                                newStatus = MessageStatus.Sent;
                            else if (newReadByCount >= totalMembers)
                                newStatus = MessageStatus.Read;
                            else if (newReadByCount > 0)
                                newStatus = MessageStatus.Delivered;
                            else
                                newStatus = MessageStatus.Sent;

                            updatedList[i] = message with
                            {
                                ReadBy = newReadBy,
                                ReadByCount = newReadByCount,
                                Status = newStatus
                            };
                            updated = true;
                        }
                    }
                }

                if (updated)
                {
                    channelMessages = updatedList;
                    InvalidateMessageCache();
                }
            }

            // Channel list status-u yenilə
            var channel = channelConversations.FirstOrDefault(c => c.Id == channelId);
            if (channel != null &&
                channel.LastMessageSenderId == currentUserId &&
                channel.LastMessageId.HasValue &&
                messageReadCounts.ContainsKey(channel.LastMessageId.Value))
            {
                // CRITICAL FIX: Use ReadByCount from SignalR event instead of channelMessages
                // When user is in another conversation, channelMessages doesn't contain this channel's messages
                var readByCount = messageReadCounts[channel.LastMessageId.Value];
                var totalMembers = channel.MemberCount - 1; // Exclude sender

                string newStatus;
                if (totalMembers == 0)
                    newStatus = "Sent";
                else if (readByCount >= totalMembers)
                    newStatus = "Read";
                else if (readByCount > 0)
                    newStatus = "Delivered";
                else
                    newStatus = "Sent";

                // PERFORMANCE: Using helper method (eliminated duplicate pattern)
                UpdateListItemWhere(
                    ref channelConversations,
                    ch => ch.Id == channelId,
                    ch => ch with { LastMessageStatus = newStatus }
                );
                updated = true;
            }

            if (updated)
            {
                StateHasChanged();
            }
        });
    }

    #endregion

    #region Typing Handlers - Yazır indikatoru

    /// <summary>
    /// DM-də kimsə yazdıqda/dayandıqda çağrılır.
    ///
    /// DEBOUNCE:
    /// Typing event-lər çox tez-tez gəlir (hər keystroke-da).
    /// ScheduleStateUpdate() ilə UI yeniləmələri batch edirik (50ms).
    /// Bu sayədə UI freeze olmur.
    /// </summary>
    private void HandleTypingInConversation(Guid conversationId, Guid userId, bool isTyping)
    {
        // Yalnız başqalarının typing-ini track et
        if (userId != currentUserId)
        {
            InvokeAsync(() =>
            {
                // Conversation list üçün typing state
                if (isTyping)
                {
                    conversationTypingState[conversationId] = true;
                }
                else
                {
                    conversationTypingState.Remove(conversationId);
                }

                // Chat header üçün typing users
                if (conversationId == selectedConversationId)
                {
                    if (isTyping)
                    {
                        if (!typingUsers.Contains("typing"))
                        {
                            typingUsers.Add("typing");
                        }
                    }
                    else
                    {
                        typingUsers.Clear();
                    }
                }

                // Debounced update (typing event-lər çox tez-tez gəlir)
                ScheduleStateUpdate();
            });
        }
    }

    /// <summary>
    /// Channel-də kimsə yazdıqda/dayandıqda çağrılır.
    /// DM-dən fərqi: displayName göstərilir (channel-də bir neçə adam ola bilər)
    /// </summary>
    private void HandleTypingInChannel(Guid channelId, Guid userId, string displayName, bool isTyping)
    {
        if (userId != currentUserId)
        {
            InvokeAsync(() =>
            {
                if (isTyping)
                {
                    // DisplayName-ləri track et
                    if (!channelTypingUsers.TryGetValue(channelId, out List<string>? value))
                    {
                        value = [];
                        channelTypingUsers[channelId] = value;
                    }
                    if (!value.Contains(displayName))
                    {
                        value.Add(displayName);
                    }
                }
                else
                {
                    if (channelTypingUsers.TryGetValue(channelId, out List<string>? value))
                    {
                        value.Remove(displayName);
                        if (channelTypingUsers[channelId].Count == 0)
                        {
                            channelTypingUsers.Remove(channelId);
                        }
                    }
                }

                // Chat header üçün typing users
                if (channelId == selectedChannelId)
                {
                    if (isTyping)
                    {
                        if (!typingUsers.Contains(displayName))
                        {
                            typingUsers.Add(displayName);
                        }
                    }
                    else
                    {
                        typingUsers = typingUsers.Where(u => u != displayName).ToList();
                    }
                }

                ScheduleStateUpdate();
            });
        }
    }

    #endregion

    #region Online Status Handlers - Online/Offline status

    /// <summary>
    /// İstifadəçi online olduqda çağrılır.
    /// Yalnız aktiv conversation-un recipient-i üçün chat header yenilənir.
    /// </summary>
    private void HandleUserOnline(Guid userId)
    {
        if (userId == recipientUserId)
        {
            InvokeAsync(() =>
            {
                isRecipientOnline = true;
                ScheduleStateUpdate();
            });
        }
    }

    /// <summary>
    /// İstifadəçi offline olduqda çağrılır.
    /// Yalnız aktiv conversation-un recipient-i üçün chat header yenilənir.
    /// </summary>
    private void HandleUserOffline(Guid userId)
    {
        if (userId == recipientUserId)
        {
            InvokeAsync(() =>
            {
                isRecipientOnline = false;
                ScheduleStateUpdate();
            });
        }
    }

    #endregion

    #region Reaction Handlers - Emoji reaction-lar

    /// <summary>
    /// DM-ə reaction əlavə/silindikdə çağrılır.
    /// </summary>
    private void HandleReactionToggledDM(Guid conversationId, Guid messageId, List<ReactionSummary> reactions)
    {
        InvokeAsync(() =>
        {
            try
            {
                if (_disposed) return;

                if (selectedConversationId.HasValue && selectedConversationId.Value == conversationId)
                {
                    UpdateDirectMessageReactions(messageId, reactions);
                }
            }
            catch
            {
                // Silently handle
            }
        });
    }

    /// <summary>
    /// Channel mesajına reaction əlavə/silindikdə çağrılır.
    /// </summary>
    private void HandleReactionToggledChannel(Guid messageId, List<ChannelMessageReactionDto> reactions)
    {
        InvokeAsync(() =>
        {
            try
            {
                if (_disposed) return;

                if (!selectedChannelId.HasValue)
                    return;

                var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    var updatedMessage = message with
                    {
                        ReactionCount = reactions.Sum(r => r.Count),
                        Reactions = reactions
                    };

                    channelMessages[index] = updatedMessage;
                    InvalidateMessageCache();
                    StateHasChanged();
                }
            }
            catch
            {
                // Silently handle
            }
        });
    }

    #endregion

    #region Channel Membership Handlers

    /// <summary>
    /// Sizi yeni channel-ə əlavə etdiklərində çağrılır.
    /// Channel list-ə yeni channel əlavə olunur.
    /// </summary>
    private void HandleAddedToChannel(ChannelDto channel)
    {
        InvokeAsync(() =>
        {
            // Artıq list-dədir?
            if (!channelConversations.Any(c => c.Id == channel.Id))
            {
                // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
                var newList = new List<ChannelDto>(channelConversations.Count + 1) { channel };
                newList.AddRange(channelConversations);
                channelConversations = newList;
                StateHasChanged();
            }
        });
    }

    /// <summary>
    /// SignalR yenidən qoşulduqda çağrılır.
    /// KRITIK: Aktiv channel/conversation group-una yenidən join olmaq lazımdır.
    /// Əks halda real-time update-lər gəlməyəcək.
    /// </summary>
    private void HandleSignalRReconnected()
    {
        InvokeAsync(async () =>
        {
            try
            {
                if (selectedChannelId.HasValue)
                {
                    await SignalRService.JoinChannelAsync(selectedChannelId.Value);
                }
                else if (selectedConversationId.HasValue)
                {
                    await SignalRService.JoinConversationAsync(selectedConversationId.Value);
                }

                StateHasChanged();
            }
            catch
            {
                // Reconnection error-ları silently handle - SignalR avtomatik retry edəcək
            }
        });
    }

    #endregion
}