using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

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
                // Mesaj artıq list-dədir? (Optimistic UI ilə əlavə edilib)
                var existingIndex = directMessages.FindIndex(m => m.Id == message.Id);

                if (existingIndex >= 0)
                {
                    // OPTİMİSTİC MESAJI ƏVƏZ ET
                    // Blazor-un dəyişikliyi görməsi üçün yeni list yaradırıq
                    var updatedList = new List<DirectMessageDto>(directMessages)
                    {
                        [existingIndex] = message
                    };
                    directMessages = updatedList;
                }
                else
                {
                    // BAŞQASININ MESAJI - list-ə əlavə et
                    directMessages.Add(message);

                    // Yalnız başqasının mesajı və səhifə görünürsə mark as read et
                    // isPageVisible - browser tab-ı fokusda olduğunu yoxlayır
                    if (message.SenderId != currentUserId && isPageVisible)
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
            }

            // CONVERSATION LIST-İ YENİLƏ
            // Son mesaj və unread count-u update et
            var conversation = directConversations.FirstOrDefault(c => c.Id == message.ConversationId);
            if (conversation != null)
            {
                var isCurrentConversation = message.ConversationId == selectedConversationId;
                var isMyMessage = message.SenderId == currentUserId;

                // with expression - record-un kopyasını yaradır, bəzi field-ləri dəyişir
                var updatedConversation = conversation with
                {
                    LastMessageContent = message.Content,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    LastMessageSenderId = message.SenderId,
                    LastMessageStatus = isMyMessage ? (message.IsRead ? "Read" : "Sent") : null,
                    // Unread: Aktiv conversation-da isək 0, öz mesajımız isə dəyişmə, başqasının isə +1
                    UnreadCount = isCurrentConversation ? 0 : (isMyMessage ? conversation.UnreadCount : conversation.UnreadCount + 1)
                };

                // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
                var newList = new List<DirectConversationDto>(directConversations.Count) { updatedConversation };
                newList.AddRange(directConversations.Where(c => c.Id != conversation.Id));
                directConversations = newList;

                // Global unread badge-i artır (header-dakı notification icon)
                if (!isCurrentConversation && !isMyMessage)
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

                var existingIndex = channelMessages.FindIndex(m => m.Id == message.Id);

                if (existingIndex >= 0)
                {
                    // OPTİMİSTİC MESAJI ƏVƏZ ET
                    var updatedList = new List<ChannelMessageDto>(channelMessages)
                    {
                        [existingIndex] = message
                    };
                    channelMessages = updatedList;
                }
                else
                {
                    // BAŞQASININ MESAJI
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

                    channelMessages.Add(message);
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
                string? status = null;
                if (isMyMessage)
                {
                    var totalMembers = channel.MemberCount - 1; // Sender-i çıxar
                    if (totalMembers == 0)
                        status = "Sent";
                    else if (message.ReadByCount >= totalMembers)
                        status = "Read";
                    else if (message.ReadByCount > 0)
                        status = "Delivered";
                    else
                        status = "Sent";
                }

                var updatedChannel = channel with
                {
                    LastMessageContent = message.Content,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    LastMessageId = message.Id,
                    LastMessageSenderId = message.SenderId,
                    LastMessageSenderAvatarUrl = message.SenderAvatarUrl,
                    LastMessageStatus = status,
                    UnreadCount = isCurrentChannel ? 0 : (isMyMessage ? channel.UnreadCount : channel.UnreadCount + 1)
                };

                // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
                var newList = new List<ChannelDto>(channelConversations.Count) { updatedChannel };
                newList.AddRange(channelConversations.Where(c => c.Id != channel.Id));
                channelConversations = newList;

                if (!isCurrentChannel && !isMyMessage)
                {
                    AppState.IncrementUnreadMessages();
                }
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
                        InvalidateMessageCache();
                        needsStateUpdate = true;

                        // Son mesaj idisə conversation list-i də yenilə
                        if (IsLastMessageInConversation(editedMessage.ConversationId, editedMessage.Id))
                        {
                            UpdateConversationLastMessage(editedMessage.ConversationId, editedMessage.Content);
                        }
                    }

                    // Reply preview-ları yenilə
                    // Bu mesaja reply edilib idisə, o reply-ın preview-unu da dəyiş
                    for (int i = 0; i < directMessages.Count; i++)
                    {
                        var msg = directMessages[i];
                        if (msg.ReplyToMessageId == editedMessage.Id && msg.ReplyToContent != editedMessage.Content)
                        {
                            directMessages[i] = msg with { ReplyToContent = editedMessage.Content };
                            InvalidateMessageCache();
                        }
                    }
                }

                // Başqa conversation-da olsaq belə, son mesaj edit olunubsa list-i yenilə
                var conversation = directConversations.FirstOrDefault(c => c.Id == editedMessage.ConversationId);
                if (conversation != null && IsLastMessageInConversation(editedMessage.ConversationId, editedMessage.Id))
                {
                    UpdateConversationLastMessage(editedMessage.ConversationId, editedMessage.Content);
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
                        InvalidateMessageCache();
                        needsStateUpdate = true;

                        // Reply preview-ları yenilə
                        for (int i = 0; i < directMessages.Count; i++)
                        {
                            var msg = directMessages[i];
                            if (msg.ReplyToMessageId == deletedMessage.Id)
                            {
                                directMessages[i] = msg with { ReplyToContent = "This message was deleted" };
                                InvalidateMessageCache();
                            }
                        }
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
                        InvalidateMessageCache();
                        needsStateUpdate = true;

                        if (IsLastMessageInChannel(editedMessage.ChannelId, updatedMessage.Id))
                        {
                            UpdateChannelLastMessage(editedMessage.ChannelId, updatedMessage.Content, message.SenderDisplayName);
                        }
                    }

                    // Reply preview-ları yenilə
                    for (int i = 0; i < channelMessages.Count; i++)
                    {
                        var msg = channelMessages[i];
                        if (msg.ReplyToMessageId == editedMessage.Id && msg.ReplyToContent != editedMessage.Content)
                        {
                            channelMessages[i] = msg with { ReplyToContent = editedMessage.Content };
                            InvalidateMessageCache();
                        }
                    }
                }

                var channel = channelConversations.FirstOrDefault(c => c.Id == editedMessage.ChannelId);
                if (channel != null && IsLastMessageInChannel(editedMessage.ChannelId, editedMessage.Id))
                {
                    UpdateChannelLastMessage(editedMessage.ChannelId, editedMessage.Content, editedMessage.SenderDisplayName);
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
                        InvalidateMessageCache();
                        needsStateUpdate = true;

                        for (int i = 0; i < channelMessages.Count; i++)
                        {
                            var msg = channelMessages[i];
                            if (msg.ReplyToMessageId == deletedMessage.Id)
                            {
                                channelMessages[i] = msg with { ReplyToContent = "This message was deleted" };
                                InvalidateMessageCache();
                            }
                        }
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
                directMessages[index] = message with { IsRead = true };
                InvalidateMessageCache();
            }
            else if (conversationId == selectedConversationId)
            {
                // Mesaj tapılmadı amma bu conversation-dayıq
                // Pending receipt kimi saxla (race condition halı)
                pendingReadReceipts[messageId] = readBy;
            }

            // Conversation list status-u yenilə
            var conversation = directConversations.FirstOrDefault(c => c.Id == conversationId);
            if (conversation != null && conversation.LastMessageSenderId == currentUserId)
            {
                var updatedConversation = conversation with
                {
                    LastMessageStatus = "Read"
                };

                // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
                var index = directConversations.IndexOf(conversation);
                if (index >= 0)
                {
                    var newList = new List<DirectConversationDto>(directConversations);
                    newList[index] = updatedConversation;
                    directConversations = newList;
                }
            }

            StateHasChanged();
        });
    }

    /// <summary>
    /// Channel mesajları oxunduqda çağrılır.
    /// Bir neçə mesaj eyni anda oxundu bildirilir.
    /// </summary>
    private void HandleChannelMessagesRead(Guid channelId, Guid userId, List<Guid> messageIds)
    {
        InvokeAsync(() =>
        {
            bool updated = false;

            if (selectedChannelId.HasValue && selectedChannelId.Value == channelId)
            {
                var updatedList = new List<ChannelMessageDto>(channelMessages);

                for (int i = 0; i < updatedList.Count; i++)
                {
                    var message = updatedList[i];
                    if (messageIds.Contains(message.Id))
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
                            updatedList[i] = message with
                            {
                                ReadBy = newReadBy,
                                ReadByCount = newReadBy.Count
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
                messageIds.Contains(channel.LastMessageId.Value))
            {
                var lastMessageInView = channelMessages.FirstOrDefault(m => m.Id == channel.LastMessageId.Value);

                string newStatus;
                if (lastMessageInView != null)
                {
                    var totalMembers = channel.MemberCount - 1;
                    if (totalMembers == 0)
                        newStatus = "Sent";
                    else if (lastMessageInView.ReadByCount >= totalMembers)
                        newStatus = "Read";
                    else if (lastMessageInView.ReadByCount > 0)
                        newStatus = "Delivered";
                    else
                        newStatus = "Sent";
                }
                else
                {
                    newStatus = "Delivered";
                }

                var updatedChannel = channel with { LastMessageStatus = newStatus };
                // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
                var index = channelConversations.IndexOf(channel);
                if (index >= 0)
                {
                    var newList = new List<ChannelDto>(channelConversations);
                    newList[index] = updatedChannel;
                    channelConversations = newList;
                    updated = true;
                }
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