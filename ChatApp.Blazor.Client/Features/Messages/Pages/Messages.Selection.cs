using ChatApp.Blazor.Client.Models.Messages;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Load Conversations and Channels - İlkin yükləmə

    /// <summary>
    /// Bütün conversation və channel-ları yükləyir.
    /// OnInitializedAsync-də və yeni conversation yaradıldıqda çağrılır.
    /// </summary>
    private async Task LoadConversationsAndChannels()
    {
        isLoadingConversationList = true;
        StateHasChanged();

        try
        {
            // PARALEL yükləmə - hər iki sorğunu eyni anda göndər
            var directConversationsTask = ConversationService.GetConversationsAsync();
            var channelConversationsTask = ChannelService.GetMyChannelsAsync();

            await Task.WhenAll(directConversationsTask, channelConversationsTask);

            var directConversationsResult = await directConversationsTask;
            var channelConversationsResult = await channelConversationsTask;

            if (directConversationsResult.IsSuccess)
            {
                directConversations = directConversationsResult.Value ?? [];
            }

            if (channelConversationsResult.IsSuccess)
            {
                channelConversations = channelConversationsResult.Value ?? [];
            }

            // Global unread badge-i yenilə
            UpdateGlobalUnreadCount();
        }
        catch (Exception ex)
        {
            ShowError("Failed to load conversations: " + ex.Message);
        }
        finally
        {
            isLoadingConversationList = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Select Conversation - DM seçimi

    /// <summary>
    /// DM conversation seçildikdə çağrılır.
    ///
    /// İŞ AXINI:
    /// 1. Guard check-lər (disposed, selecting, null, already selected)
    /// 2. Əvvəlki channel-ı mark as read et
    /// 3. Pending conversation state-i təmizlə
    /// 4. LAZY LOADING: Əvvəlki group-dan leave, yenisina join
    /// 5. State-i sıfırla (messages, typing, pending receipts)
    /// 6. Draft yüklə
    /// 7. Mesajları yüklə
    /// 8. Pin və favorite count-ları yüklə
    ///
    /// GUARD PATTERN:
    /// - _isSelecting: Eyni anda 2 selection-un qarşısını alır
    /// - _disposed: Component bağlanıbsa işləmə
    /// - null check: Null conversation gəlməsin
    /// - already selected: Eyni conversation-a 2 dəfə click
    /// </summary>
    private async Task SelectDirectConversation(DirectConversationDto conversation)
    {
        // Selection mode-dan çıx
        if (isSelectingMessageBuble)
        {
            ToggleSelectMode();
        }

        // GUARD: Race condition prevention
        if (_isConversationSelecting || _disposed)
        {
            return;
        }

        // GUARD: Null check
        if (conversation == null)
        {
            return;
        }

        // GUARD: Already selected
        if (selectedConversationId.HasValue && selectedConversationId.Value == conversation.Id)
        {
            return;
        }

        _isConversationSelecting = true;

        try
        {
            // Əvvəlki channel-ı mark as read et
            if (selectedChannelId.HasValue)
            {
                try
                {
                    await ChannelService.MarkAsReadAsync(selectedChannelId.Value);
                }
                catch (Exception ex)
                {
                    // LOW PRIORITY FIX: Log error for debugging
                    System.Diagnostics.Debug.WriteLine($"[Messages] Mark channel as read on switch error: {ex.Message}");
                }
            }

            // Pending conversation state-i təmizlə
            isPendingConversation = false;
            pendingUser = null;

            // FIX: Clear reply/edit mode when switching conversations
            if (isReplying)
            {
                CancelReply();
            }

            // MEMORY LEAK FIX: Clear processed message IDs to prevent unbounded growth
            processedMessageIds.Clear();

            // DUPLICATE FIX: Clear pending message tracking dictionaries
            pendingDirectMessages.Clear();
            pendingChannelMessages.Clear();

            // MEMORY LEAK FIX: Clear typing state collections
            conversationTypingState.Clear();
            channelTypingUsers.Clear();

            // LAZY LOADING: Əvvəlki group-dan leave
            if (selectedConversationId.HasValue && selectedConversationId.Value != conversation.Id)
            {
                await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
            }

            // Channel group-dan da leave (conversation-a keçirik)
            if (selectedChannelId.HasValue)
            {
                await SignalRService.LeaveChannelAsync(selectedChannelId.Value);
            }

            // Unread count, mention badge və FirstUnreadMessageId-ni clear et
            // CRITICAL: FirstUnreadMessageId clear edilməlidir ki, geri qayıtdıqda köhnə mesaja scroll olunmasın
            if (conversation.UnreadCount > 0 || conversation.HasUnreadMentions || conversation.FirstUnreadMessageId.HasValue)
            {
                if (conversation.UnreadCount > 0)
                {
                    AppState.DecrementUnreadMessages(conversation.UnreadCount);
                }

                // PERFORMANCE: Using helper method (eliminated duplicate pattern)
                UpdateListItemWhere(
                    ref directConversations,
                    c => c.Id == conversation.Id,
                    c => c with
                    {
                        UnreadCount = 0,
                        HasUnreadMentions = false,
                        FirstUnreadMessageId = null // Clear after reading
                    }
                );
            }

            // STATE RESET
            directMessages.Clear();
            channelMessages.Clear();
            hasMoreMessages = true;
            hasMoreNewerMessages = false;
            oldestMessageDate = null;
            newestMessageDate = null;
            isViewingAroundMessage = false;
            typingUsers.Clear();
            pendingReadReceipts.Clear();
            pendingMessageAdds.Clear();
            favoriteMessageIds.Clear();
            showSearchPanel = false;
            pageSize = 30; // İlk yükləmə 30 mesaj (optimizasiya)

            // Auto-unmark read later (channel)
            if (selectedChannelId.HasValue && lastReadLaterMessageId.HasValue && lastReadLaterMessageIdOnEntry.HasValue)
            {
                try
                {
                    await ChannelService.ToggleMessageAsLaterAsync(selectedChannelId.Value, lastReadLaterMessageId.Value);
                    var channelIndex = channelConversations.FindIndex(c => c.Id == selectedChannelId.Value);
                    if (channelIndex >= 0)
                    {
                        channelConversations[channelIndex] = channelConversations[channelIndex] with { LastReadLaterMessageId = null };
                        channelConversations = channelConversations.ToList();
                    }
                }
                catch (Exception ex)
                {
                    // LOW PRIORITY FIX: Log for debugging
                    System.Diagnostics.Debug.WriteLine($"[Messages] Operation error: {ex.Message}");
                }
            }

            // Auto-unmark read later (conversation)
            if (selectedConversationId.HasValue && lastReadLaterMessageId.HasValue && lastReadLaterMessageIdOnEntry.HasValue)
            {
                try
                {
                    await ConversationService.ToggleMessageAsLaterAsync(selectedConversationId.Value, lastReadLaterMessageId.Value);
                    var conversationIndex = directConversations.FindIndex(c => c.Id == selectedConversationId.Value);
                    if (conversationIndex >= 0)
                    {
                        directConversations[conversationIndex] = directConversations[conversationIndex] with { LastReadLaterMessageId = null };
                        directConversations = directConversations.ToList();
                    }
                }
                catch (Exception ex)
                {
                    // LOW PRIORITY FIX: Log for debugging
                    System.Diagnostics.Debug.WriteLine($"[Messages] Operation error: {ex.Message}");
                }
            }

            // Unread separator reset
            unreadSeparatorAfterMessageId = null;
            shouldCalculateUnreadSeparator = conversation.UnreadCount > 0;

            // Read later marker
            lastReadLaterMessageId = conversation.LastReadLaterMessageId;
            lastReadLaterMessageIdOnEntry = conversation.LastReadLaterMessageId;

            // SELECTION - state dəyiş
            selectedConversationId = conversation.Id;
            selectedChannelId = null;
            isDirectMessage = true;

            recipientName = conversation.IsNotes ? "Notes" : conversation.OtherUserDisplayName;
            recipientAvatarUrl = conversation.OtherUserAvatarUrl;
            recipientUserId = conversation.OtherUserId;
            isNotesConversation = conversation.IsNotes;

            // Mention data: DM üçün conversation partner
            currentConversationPartner = new MentionUserDto
            {
                Id = conversation.OtherUserId,
                Name = conversation.OtherUserDisplayName,
                AvatarUrl = conversation.OtherUserAvatarUrl,
                IsMember = true,
                IsAll = false
            };
            currentChannelMembers = []; // DM-də channel member yoxdur

            // Online status-u real-time yoxla
            isRecipientOnline = await SignalRService.IsUserOnlineAsync(conversation.OtherUserId);

            // Draft yüklə
            currentDraft = LoadDraft(conversation.Id, null);

            // SignalR group-a join
            await SignalRService.JoinConversationAsync(conversation.Id);

            // PRIORITY 1: First unread message varsa, GetAroundMessage ilə yüklə
            if (conversation.FirstUnreadMessageId.HasValue)
            {
                // Pinned və favorites paralel yüklə, mesajları isə first unread message-ın ətrafından yüklə
                await Task.WhenAll(
                    LoadPinnedDirectMessageCount(),
                    LoadFavoriteDirectMessages()
                );

                // Dinamik count hesabla: UnreadCount əsasında (minimum 30, maksimum 100)
                // Əgər 30+ unread mesaj varsa bütün unread mesajları yükləmək üçün count artırılır
                int messageCount = Math.Max(30, Math.Min(100, conversation.UnreadCount * 2 + 20));

                // First unread mesajının ətrafındakı mesajları yüklə və scroll et
                await NavigateToMessageAsync(conversation.FirstUnreadMessageId.Value, messageCount);
            }
            // PRIORITY 2: Read later message varsa, GetAroundMessage ilə yüklə
            else if (conversation.LastReadLaterMessageId.HasValue)
            {
                // Pinned və favorites paralel yüklə, mesajları isə read later message-ın ətrafından yüklə
                await Task.WhenAll(
                    LoadPinnedDirectMessageCount(),
                    LoadFavoriteDirectMessages()
                );

                // Read later mesajının ətrafındakı mesajları yüklə və scroll et
                await NavigateToMessageAsync(conversation.LastReadLaterMessageId.Value);
            }
            else
            {
                // Hide container BEFORE loading (prevents flash)
                await JS.InvokeVoidAsync("chatAppUtils.hideElement", "chat-messages");

                // Normal yükləmə (separator yoxdur)
                await Task.WhenAll(
                    LoadDirectMessages(),
                    LoadPinnedDirectMessageCount(),
                    LoadFavoriteDirectMessages()
                );

                // CRITICAL: Pinned header render olsun ƏVVƏL scroll etmədən
                // Əks halda pinned header sonra render olur və scroll position yuxarı itələnir
                StateHasChanged();
                await Task.Delay(50); // Pinned header DOM-a əlavə olunması üçün

                // Scroll and show (no flash - container was hidden during load)
                await JS.InvokeVoidAsync("chatAppUtils.scrollToBottomAndShow", "chat-messages");
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to select conversation: {ex.Message}");
        }
        finally
        {
            _isConversationSelecting = false;
        }
    }


    /// <summary>
    /// Yeni istifadəçi ilə söhbət başlatmaq və boş conversation-ların qarşısını almaq üçün.
    /// İstifadəçi axtarışdan birini seçdikdə, hələ conversation yaratmırıq.
    /// Yalnız UI-ı hazırlayırıq. İlk mesaj göndərildikdə yaradılır.
    /// </summary>
    private async Task StartConversationWithUser(Guid userId)
    {
        // Mövcud conversation varsa, onu seç
        var existingConversation = directConversations.FirstOrDefault(c => c.OtherUserId == userId);
        if (existingConversation != null)
        {
            CloseNewConversationDialog();
            await SelectDirectConversation(existingConversation);
            return;
        }

        // Search results-dan user-i tap
        var user = userSearchResults.FirstOrDefault(u => u.Id == userId);
        if (user == null) return;

        // PENDING CONVERSATION - UI-ı hazırla
        isPendingConversation = true;
        pendingUser = user;
        selectedConversationId = null;
        selectedChannelId = null;
        isDirectMessage = true;

        recipientUserId = user.Id;
        recipientName = user.DisplayName;
        recipientAvatarUrl = user.AvatarUrl;
        isRecipientOnline = await SignalRService.IsUserOnlineAsync(user.Id);

        // Mention data: Pending conversation üçün conversation partner
        currentConversationPartner = new MentionUserDto
        {
            Id = user.Id,
            Name = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            IsMember = true,
            IsAll = false
        };
        currentChannelMembers = []; // DM-də channel member yoxdur

        // State reset
        directMessages.Clear();
        channelMessages.Clear();
        typingUsers.Clear();
        pendingReadReceipts.Clear();
        pendingMessageAdds.Clear();
        hasMoreMessages = false;
        hasMoreNewerMessages = false;
        oldestMessageDate = null;
        newestMessageDate = null;
        pageSize = 30;

        // Draft yüklə (pending user üçün)
        currentDraft = LoadDraft(null, null, user.Id);

        CloseNewConversationDialog();
        StateHasChanged();
    }

    #endregion

    #region Select Channel - Channel seçimi

    /// <summary>
    /// Channel seçildikdə çağrılır.
    /// </summary>
    private async Task SelectChannel(ChannelDto channel)
    {
        if (isSelectingMessageBuble)
        {
            ToggleSelectMode();
        }

        // GUARD checks
        if (_isConversationSelecting || _disposed)
        {
            return;
        }

        if (channel == null)
        {
            return;
        }

        if (selectedChannelId.HasValue && selectedChannelId.Value == channel.Id)
        {
            return;
        }

        _isConversationSelecting = true;

        try
        {
            // Əvvəlki channel-ı mark as read et
            if (selectedChannelId.HasValue && selectedChannelId.Value != channel.Id)
            {
                try
                {
                    await ChannelService.MarkAsReadAsync(selectedChannelId.Value);
                }
                catch (Exception ex)
                {
                    // LOW PRIORITY FIX: Log for debugging
                    System.Diagnostics.Debug.WriteLine($"[Messages] Operation error: {ex.Message}");
                }

                // Auto-unmark read later
                if (lastReadLaterMessageId.HasValue && lastReadLaterMessageIdOnEntry.HasValue)
                {
                    try
                    {
                        await ChannelService.ToggleMessageAsLaterAsync(selectedChannelId.Value, lastReadLaterMessageId.Value);
                        var channelIndex = channelConversations.FindIndex(c => c.Id == selectedChannelId.Value);
                        if (channelIndex >= 0)
                        {
                            channelConversations[channelIndex] = channelConversations[channelIndex] with { LastReadLaterMessageId = null };
                            channelConversations = channelConversations.ToList();
                        }
                    }
                    catch (Exception ex)
                {
                    // LOW PRIORITY FIX: Log for debugging
                    System.Diagnostics.Debug.WriteLine($"[Messages] Operation error: {ex.Message}");
                }
                }
            }

            isPendingConversation = false;
            pendingUser = null;

            // FIX: Clear reply/edit mode when switching channels
            if (isReplying)
            {
                CancelReply();
            }

            // MEMORY LEAK FIX: Clear processed message IDs to prevent unbounded growth
            processedMessageIds.Clear();

            // DUPLICATE FIX: Clear pending message tracking dictionaries
            pendingDirectMessages.Clear();
            pendingChannelMessages.Clear();

            // MEMORY LEAK FIX: Clear typing state collections
            conversationTypingState.Clear();
            channelTypingUsers.Clear();

            // LAZY LOADING: Leave previous groups
            if (selectedChannelId.HasValue && selectedChannelId.Value != channel.Id)
            {
                await SignalRService.LeaveChannelAsync(selectedChannelId.Value);
            }

            if (selectedConversationId.HasValue)
            {
                // Auto-unmark read later (conversation)
                if (lastReadLaterMessageId.HasValue && lastReadLaterMessageIdOnEntry.HasValue)
                {
                    try
                    {
                        await ConversationService.ToggleMessageAsLaterAsync(selectedConversationId.Value, lastReadLaterMessageId.Value);
                        var conversationIndex = directConversations.FindIndex(c => c.Id == selectedConversationId.Value);
                        if (conversationIndex >= 0)
                        {
                            directConversations[conversationIndex] = directConversations[conversationIndex] with { LastReadLaterMessageId = null };
                            directConversations = directConversations.ToList();
                        }
                    }
                    catch (Exception ex)
                {
                    // LOW PRIORITY FIX: Log for debugging
                    System.Diagnostics.Debug.WriteLine($"[Messages] Operation error: {ex.Message}");
                }
                }

                await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
            }

            // Unread count və FirstUnreadMessageId-ni clear et
            // CRITICAL: FirstUnreadMessageId clear edilməlidir ki, geri qayıtdıqda köhnə mesaja scroll olunmasın
            if (channel.UnreadCount > 0 || channel.FirstUnreadMessageId.HasValue)
            {
                if (channel.UnreadCount > 0)
                {
                    AppState.DecrementUnreadMessages(channel.UnreadCount);
                }
                // PERFORMANCE: Using helper method (eliminated duplicate pattern)
                UpdateListItemWhere(
                    ref channelConversations,
                    ch => ch.Id == channel.Id,
                    ch => ch with
                    {
                        UnreadCount = 0,
                        FirstUnreadMessageId = null // Clear after reading
                    }
                );
            }

            // STATE RESET
            directMessages.Clear();
            channelMessages.Clear();
            hasMoreMessages = true;
            hasMoreNewerMessages = false;
            oldestMessageDate = null;
            newestMessageDate = null;
            isViewingAroundMessage = false;
            typingUsers.Clear();
            pendingReadReceipts.Clear();
            pendingMessageAdds.Clear();
            favoriteMessageIds.Clear();
            showSearchPanel = false;
            pageSize = 30;

            // Unread separator
            unreadSeparatorAfterMessageId = null;
            shouldCalculateUnreadSeparator = channel.UnreadCount > 0;

            // Read later marker
            lastReadLaterMessageId = channel.LastReadLaterMessageId;
            lastReadLaterMessageIdOnEntry = channel.LastReadLaterMessageId;

            // SELECTION
            selectedChannelId = channel.Id;
            selectedConversationId = null;
            isDirectMessage = false;
            selectedChannelName = channel.Name;
            selectedChannelDescription = channel.Description;
            selectedChannelType = channel.Type;
            selectedChannelMemberCount = channel.MemberCount;

            // Draft yüklə
            currentDraft = LoadDraft(null, channel.Id);

            // ADMIN YOXLAMASI və MENTION DATA
            // Channel yaradıcısı avtomatik admin-dir
            isChannelAdmin = channel.CreatedBy == currentUserId;
            currentUserChannelRole = isChannelAdmin ? ChannelMemberRole.Owner : ChannelMemberRole.Member;

            // Mention data: Channel üçün member-lər (@All MessageInput-da dinamik əlavə olunur)
            currentConversationPartner = null; // Channel-da conversation partner yoxdur
            currentChannelMembers = [];

            // PERFORMANCE: Fetch channel details once (was called twice: admin check + mentions)
            var channelDetails = await ChannelService.GetChannelAsync(channel.Id);
            if (channelDetails.IsSuccess && channelDetails.Value != null)
            {
                // Yaradıcı deyilsə, role-u yoxla (admin check)
                if (!isChannelAdmin)
                {
                    var currentMember = channelDetails.Value.Members.FirstOrDefault(m => m.UserId == currentUserId);
                    if (currentMember != null)
                    {
                        currentUserChannelRole = currentMember.Role;
                        isChannelAdmin = currentMember.Role == ChannelMemberRole.Admin ||
                                        currentMember.Role == ChannelMemberRole.Owner;
                    }
                }

                // Channel member-lərini mention üçün əlavə et
                var memberDtos = channelDetails.Value.Members
                    .Where(m => m.UserId != currentUserId) // Özünü çıxar
                    .Select(m => new MentionUserDto
                    {
                        Id = m.UserId,
                        Name = m.DisplayName,
                        AvatarUrl = m.AvatarUrl,
                        IsMember = true,
                        IsAll = false
                    })
                    .ToList();

                currentChannelMembers.AddRange(memberDtos);
            }

            // SignalR group-a join
            await SignalRService.JoinChannelAsync(channel.Id);

            // PRIORITY 1: First unread message varsa, GetAroundMessage ilə yüklə
            if (channel.FirstUnreadMessageId.HasValue)
            {
                // Pinned və favorites paralel yüklə, mesajları isə first unread message-ın ətrafından yüklə
                await Task.WhenAll(
                    LoadPinnedMessageCount(),
                    LoadFavoriteChannelMessages()
                );

                // Dinamik count hesabla: UnreadCount əsasında (minimum 30, maksimum 100)
                // Əgər 30+ unread mesaj varsa bütün unread mesajları yükləmək üçün count artırılır
                int messageCount = Math.Max(30, Math.Min(100, channel.UnreadCount * 2 + 20));

                // First unread mesajının ətrafındakı mesajları yüklə və scroll et
                await NavigateToMessageAsync(channel.FirstUnreadMessageId.Value, messageCount);
            }
            // PRIORITY 2: Read later message varsa, GetAroundMessage ilə yüklə
            else if (channel.LastReadLaterMessageId.HasValue)
            {
                // Pinned və favorites paralel yüklə, mesajları isə read later message-ın ətrafından yüklə
                await Task.WhenAll(
                    LoadPinnedMessageCount(),
                    LoadFavoriteChannelMessages()
                );

                // Read later mesajının ətrafındakı mesajları yüklə və scroll et
                await NavigateToMessageAsync(channel.LastReadLaterMessageId.Value);
            }
            else
            {
                // Hide container BEFORE loading (prevents flash)
                await JS.InvokeVoidAsync("chatAppUtils.hideElement", "chat-messages");

                // Normal yükləmə (separator yoxdur)
                await Task.WhenAll(
                    LoadChannelMessages(),
                    LoadPinnedMessageCount(),
                    LoadFavoriteChannelMessages()
                );

                // CRITICAL: Pinned header render olsun ƏVVƏL scroll etmədən
                // Əks halda pinned header sonra render olur və scroll position yuxarı itələnir
                StateHasChanged();
                await Task.Delay(50); // Pinned header DOM-a əlavə olunması üçün

                // Scroll and show (no flash - container was hidden during load)
                await JS.InvokeVoidAsync("chatAppUtils.scrollToBottomAndShow", "chat-messages");
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to select channel: {ex.Message}");
        }
        finally
        {
            _isConversationSelecting = false;
        }
    }

    #endregion

    #region Load Messages - Mesajları yüklə

    /// <summary>
    /// DM mesajlarını yükləyir.
    /// Around mode-da GetMessagesBeforeAsync, normal mode-da GetMessagesAsync.
    /// </summary>
    private async Task LoadDirectMessages()
    {
        isLoadingMessages = true;
        StateHasChanged();

        try
        {
            var result = isViewingAroundMessage && oldestMessageDate.HasValue
                ? await ConversationService.GetMessagesBeforeAsync(
                    selectedConversationId!.Value,
                    oldestMessageDate.Value,
                    30) // Around mode: 30 mesaj (optimizasiya)
                : await ConversationService.GetMessagesAsync(
                    selectedConversationId!.Value,
                    pageSize,
                    oldestMessageDate);

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    // DUBLİKAT YOXLAMASI - artıq olan mesajları əlavə etmə
                    var existingIds = directMessages.Select(m => m.Id).ToHashSet();
                    var newMessages = messages.Where(m => !existingIds.Contains(m.Id)).OrderBy(m => m.CreatedAtUtc).ToList();

                    // YENİ MESAJLARI ƏVVƏLƏ ƏLAVƏ ET (köhnə mesajlar üstdədir)
                    directMessages.InsertRange(0, newMessages);

                    // ən köhnə mesajın tarixini saxla (növbəti pagination üçün)
                    // SpecifyKind: PostgreSQL UTC timestamp-ı C#-da düzgün işləməsi üçün
                    oldestMessageDate = DateTime.SpecifyKind(messages.Min(m => m.CreatedAtUtc), DateTimeKind.Utc);

                    // Daha çox mesaj var? (Backend 30 gəlibsə - var, az gəlibsə - yoxdur)
                    hasMoreMessages = messages.Count >= 30;
                }
                else
                {
                    hasMoreMessages = false;
                }

                // UNREAD SEPARATOR hesabla
                CalculateUnreadSeparatorPosition(
                    messages,
                    m => !m.IsRead && m.SenderId != currentUserId,
                    m => m.Id,
                    m => m.CreatedAtUtc
                );

                // Mark as read (5+ mesaj = bulk API, <5 = individual)
                await MarkDirectMessagesAsReadAsync(messages.Where(m => !m.IsRead && m.SenderId != currentUserId).ToList());
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to load messages: " + ex.Message);
        }
        finally
        {
            isLoadingMessages = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Channel mesajlarını yükləyir.
    /// Around mode-da GetMessagesBeforeAsync, normal mode-da GetMessagesAsync.
    /// </summary>
    private async Task LoadChannelMessages()
    {
        isLoadingMessages = true;
        StateHasChanged();

        try
        {
            var result = isViewingAroundMessage && oldestMessageDate.HasValue
                ? await ChannelService.GetMessagesBeforeAsync(
                    selectedChannelId!.Value,
                    oldestMessageDate.Value,
                    30) // Around mode: 30 mesaj (optimizasiya)
                : await ChannelService.GetMessagesAsync(
                    selectedChannelId!.Value,
                    pageSize,
                    oldestMessageDate);

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    // DUBLİKAT YOXLAMASI - artıq olan mesajları əlavə etmə
                    var existingIds = channelMessages.Select(m => m.Id).ToHashSet();
                    var newMessages = messages.Where(m => !existingIds.Contains(m.Id)).OrderBy(m => m.CreatedAtUtc).ToList();

                    channelMessages.InsertRange(0, newMessages);

                    // ən köhnə mesajın tarixini saxla (növbəti pagination üçün)
                    oldestMessageDate = DateTime.SpecifyKind(messages.Min(m => m.CreatedAtUtc), DateTimeKind.Utc);

                    // Daha çox mesaj var? (Backend 30 gəlibsə - var, az gəlibsə - yoxdur)
                    hasMoreMessages = messages.Count >= 30;
                }
                else
                {
                    hasMoreMessages = false;
                }

                // UNREAD SEPARATOR (Channel-də ReadBy list var)
                CalculateUnreadSeparatorPosition(
                    messages,
                    m => m.SenderId != currentUserId && (m.ReadBy == null || !m.ReadBy.Contains(currentUserId)),
                    m => m.Id,
                    m => m.CreatedAtUtc
                );

                // Mark as read (SignalR-dan UI update gələcək)
                await MarkChannelMessagesAsReadAsync(messages.Where(m =>
                    m.SenderId != currentUserId &&
                    (m.ReadBy == null || !m.ReadBy.Contains(currentUserId))
                ).ToList());
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to load messages: " + ex.Message);
        }
        finally
        {
            isLoadingMessages = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// "Load More" düyməsi basıldıqda çağrılır (yuxarı scroll - köhnə mesajlar).
    /// PAGINATION STANDARD: Həmişə 30 mesaj yüklənir (consistent everywhere).
    /// </summary>
    private async Task LoadMoreMessages()
    {
        if (isLoadingMoreMessages || !hasMoreMessages) return;

        isLoadingMoreMessages = true;
        StateHasChanged();

        try
        {
            // pageSize həmişə 30-dur (optimizasiya)
            pageSize = 30;

            if (isDirectMessage && selectedConversationId.HasValue)
            {
                await LoadDirectMessages();
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                await LoadChannelMessages();
            }
        }
        finally
        {
            isLoadingMoreMessages = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Around mode-da aşağı scroll edildikdə yeni mesajlar yükləyir (APPEND).
    /// </summary>
    private async Task LoadNewerMessages()
    {
        if (isLoadingMoreMessages || !hasMoreNewerMessages || !isViewingAroundMessage) return;

        isLoadingMoreMessages = true;
        StateHasChanged();

        try
        {
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                await LoadNewerDirectMessages();
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                await LoadNewerChannelMessages();
            }
        }
        finally
        {
            isLoadingMoreMessages = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Yeni DM mesajlarını yükləyir (APPEND - aşağıya doğru).
    /// </summary>
    private async Task LoadNewerDirectMessages()
    {
        if (!newestMessageDate.HasValue) return;

        try
        {
            var result = await ConversationService.GetMessagesAfterAsync(
                selectedConversationId!.Value,
                newestMessageDate.Value,
                30); // Optimizasiya: 30 mesaj

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    // DUBLİKAT YOXLAMASI
                    var existingIds = directMessages.Select(m => m.Id).ToHashSet();
                    var newMessages = messages.Where(m => !existingIds.Contains(m.Id)).OrderBy(m => m.CreatedAtUtc);

                    // YENİ MESAJLARI SONA ƏLAVƏ ET (APPEND)
                    directMessages.AddRange(newMessages);

                    // ən yeni mesajın tarixini yenilə
                    newestMessageDate = DateTime.SpecifyKind(messages.Max(m => m.CreatedAtUtc), DateTimeKind.Utc);

                    // Daha yeni mesajlar var?
                    hasMoreNewerMessages = messages.Count >= 30;
                }
                else
                {
                    hasMoreNewerMessages = false;
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to load newer messages: " + ex.Message);
        }
    }

    /// <summary>
    /// Yeni channel mesajlarını yükləyir (APPEND - aşağıya doğru).
    /// </summary>
    private async Task LoadNewerChannelMessages()
    {
        if (!newestMessageDate.HasValue) return;

        try
        {
            var result = await ChannelService.GetMessagesAfterAsync(
                selectedChannelId!.Value,
                newestMessageDate.Value,
                30); // Optimizasiya: 30 mesaj

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    // DUBLİKAT YOXLAMASI
                    var existingIds = channelMessages.Select(m => m.Id).ToHashSet();
                    var newMessages = messages.Where(m => !existingIds.Contains(m.Id)).OrderBy(m => m.CreatedAtUtc);

                    // YENİ MESAJLARI SONA ƏLAVƏ ET (APPEND)
                    channelMessages.AddRange(newMessages);

                    // ən yeni mesajın tarixini yenilə
                    newestMessageDate = DateTime.SpecifyKind(messages.Max(m => m.CreatedAtUtc), DateTimeKind.Utc);

                    // Daha yeni mesajlar var?
                    hasMoreNewerMessages = messages.Count >= 30;
                }
                else
                {
                    hasMoreNewerMessages = false;
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to load newer messages: " + ex.Message);
        }
    }

    /// <summary>
    /// Müəyyən bir mesaja naviqasiya edir (pinned/favorite/reply).
    /// Mesaj yüklənmiş mesajlar arasındadırsa scroll edir,
    /// yoxdursa GetMessagesAround ilə mesajın ətrafını yükləyir.
    /// </summary>
    /// <param name="count">GetMessagesAround count parametri (default 30, FirstUnreadMessage üçün dinamik hesablanır)</param>
    private async Task NavigateToMessageAsync(Guid messageId, int count = 30)
    {
        // 1. Mesaj artıq yüklənibsə - sadəcə scroll et
        if (isDirectMessage)
        {
            if (directMessages.Any(m => m.Id == messageId))
            {
                await ScrollToAndHighlightMessage(messageId);
                return;
            }

            // 2. Mesaj yüklənməyib - ətrafını yüklə
            if (!selectedConversationId.HasValue) return;

            isLoadingMessages = true;
            StateHasChanged();

            try
            {
                var result = await ConversationService.GetMessagesAroundAsync(
                    selectedConversationId.Value,
                    messageId,
                    count);

                if (result.IsSuccess && result.Value != null && result.Value.Count > 0)
                {
                    // Mövcud mesajları əvəz et
                    directMessages.Clear();
                    directMessages.AddRange(result.Value.OrderBy(m => m.CreatedAtUtc));

                    // Pagination state-i yenilə (bi-directional)
                    oldestMessageDate = DateTime.SpecifyKind(directMessages.First().CreatedAtUtc, DateTimeKind.Utc);
                    newestMessageDate = DateTime.SpecifyKind(directMessages.Last().CreatedAtUtc, DateTimeKind.Utc);
                    hasMoreMessages = true;
                    hasMoreNewerMessages = true; // Around mode: hər iki istiqamətdə load mümkündür
                    isViewingAroundMessage = true;

                    // Unread separator hesabla (GetAroundMessage)
                    if (shouldCalculateUnreadSeparator)
                    {
                        CalculateUnreadSeparatorPosition(
                            directMessages,
                            m => !m.IsRead && m.ReceiverId == currentUserId,
                            m => m.Id,
                            m => m.CreatedAtUtc);
                    }

                    // Mark as read
                    await MarkDirectMessagesAsReadAsync(result.Value.Where(m => !m.IsRead && m.SenderId != currentUserId).ToList());

                    StateHasChanged();
                    await Task.Delay(500); // DOM render + potential image loading

                    // Separator varsa separator-a, yoxdursa mesaja scroll et
                    try
                    {
                        if (unreadSeparatorAfterMessageId.HasValue)
                        {
                            // Separator varsa separator-a scroll et - instant scroll (no smooth)
                            await JS.InvokeVoidAsync("chatAppUtils.scrollToElement", "unread-separator");
                        }
                        else
                        {
                            // Separator yoxdursa mesaja scroll et
                            await JS.InvokeVoidAsync("chatAppUtils.scrollToMessageAndHighlight", $"message-{messageId}");
                        }
                    }
                    catch
                    {
                        // Scroll error silently ignore
                    }

                    // Scroll tamamlandı - indi bi-directional loading-ə icazə ver
                    isViewingAroundMessage = false;
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to navigate to message: " + ex.Message);
            }
            finally
            {
                isLoadingMessages = false;
                StateHasChanged();
            }
        }
        else
        {
            if (channelMessages.Any(m => m.Id == messageId))
            {
                await ScrollToAndHighlightMessage(messageId);
                return;
            }

            // 2. Mesaj yüklənməyib - ətrafını yüklə
            if (!selectedChannelId.HasValue) return;

            isLoadingMessages = true;
            StateHasChanged();

            try
            {
                var result = await ChannelService.GetMessagesAroundAsync(
                    selectedChannelId.Value,
                    messageId,
                    count);

                if (result.IsSuccess && result.Value != null && result.Value.Count > 0)
                {
                    // Mövcud mesajları əvəz et
                    channelMessages.Clear();
                    channelMessages.AddRange(result.Value.OrderBy(m => m.CreatedAtUtc));

                    // Pagination state-i yenilə (bi-directional)
                    oldestMessageDate = DateTime.SpecifyKind(channelMessages.First().CreatedAtUtc, DateTimeKind.Utc);
                    newestMessageDate = DateTime.SpecifyKind(channelMessages.Last().CreatedAtUtc, DateTimeKind.Utc);
                    hasMoreMessages = true;
                    hasMoreNewerMessages = true; // Around mode: hər iki istiqamətdə load mümkündür
                    isViewingAroundMessage = true;

                    // Unread separator hesabla (GetAroundMessage)
                    if (shouldCalculateUnreadSeparator)
                    {
                        CalculateUnreadSeparatorPosition(
                            channelMessages,
                            m => m.SenderId != currentUserId && (m.ReadBy == null || !m.ReadBy.Contains(currentUserId)),
                            m => m.Id,
                            m => m.CreatedAtUtc);
                    }

                    // Mark as read (SignalR-dan UI update gələcək)
                    await MarkChannelMessagesAsReadAsync(result.Value.Where(m =>
                        m.SenderId != currentUserId &&
                        (m.ReadBy == null || !m.ReadBy.Contains(currentUserId))
                    ).ToList());

                    StateHasChanged();
                    await Task.Delay(500); // DOM render + potential image loading

                    // Separator varsa separator-a, yoxdursa mesaja scroll et
                    try
                    {
                        if (unreadSeparatorAfterMessageId.HasValue)
                        {
                            // Separator varsa separator-a scroll et - instant scroll (no smooth)
                            await JS.InvokeVoidAsync("chatAppUtils.scrollToElement", "unread-separator");
                        }
                        else
                        {
                            // Separator yoxdursa mesaja scroll et
                            await JS.InvokeVoidAsync("chatAppUtils.scrollToMessageAndHighlight", $"message-{messageId}");
                        }
                    }
                    catch
                    {
                        // Scroll error silently ignore
                    }

                    // Scroll tamamlandı - indi bi-directional loading-ə icazə ver
                    isViewingAroundMessage = false;
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to navigate to message: " + ex.Message);
            }
            finally
            {
                isLoadingMessages = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Ən aşağıya scroll edir (Scroll to Bottom).
    /// HƏMIŞƏ clear + reload (conversation switch kimi davranır).
    /// Harada olursan ol, ən son 30 mesaj yüklənir.
    /// </summary>
    private async Task ScrollToBottomAsync()
    {
        // Reset pagination state
        oldestMessageDate = null;
        newestMessageDate = null;
        hasMoreNewerMessages = false;
        isViewingAroundMessage = false;
        pageSize = 30;

        if (isDirectMessage && selectedConversationId.HasValue)
        {
            directMessages.Clear();
            await LoadDirectMessages();
        }
        else if (!isDirectMessage && selectedChannelId.HasValue)
        {
            channelMessages.Clear();
            await LoadChannelMessages();
        }

        // Scroll to bottom
        StateHasChanged();
        await Task.Delay(50);
        await JS.InvokeVoidAsync("chatAppUtils.scrollToBottomById", "chat-messages");
    }
    #endregion

    #region Create Channel - Yeni channel yaratmaq

    /// <summary>
    /// Dialog-dan alınan newChannelRequest ilə yeni channel yaradır
    /// </summary>
    private async Task CreateChannel()
    {
        isCreatingChannel = true;
        StateHasChanged();

        try
        {
            var result = await ChannelService.CreateChannelAsync(newChannelRequest);
            if (result.IsSuccess)
            {
                CloseNewChannelDialog();
                await LoadConversationsAndChannels();

                // Yaradılan channel-ı seç
                var channel = channelConversations.FirstOrDefault(c => c.Id == result.Value);
                if (channel != null)
                {
                    await SelectChannel(channel);
                }
            }
            else
            {
                ShowError(result.Error ?? "Failed to create channel");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to create channel: " + ex.Message);
        }
        finally
        {
            isCreatingChannel = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Add Member to Channel - Channel-ə üzv əlavə et

    /// <summary>
    /// Channel-ə yeni üzv əlavə edir.
    /// Admin/Owner yalnız bu əməliyyatı edə bilər.
    /// </summary>
    private async Task SearchUsersForAddMember(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            memberSearchResults.Clear();
            isSearchingMembersForAdd = false;
            StateHasChanged();
            return;
        }

        isSearchingMembersForAdd = true;
        StateHasChanged();

        try
        {
            var result = await UserService.SearchUsersAsync(query);
            if (result.IsSuccess && result.Value != null)
            {
                // Mövcud üzvləri çıxar
                var channelDetails = selectedChannelId.HasValue
                    ? await ChannelService.GetChannelAsync(selectedChannelId.Value)
                    : null;

                var existingMemberIds = channelDetails?.Value?.Members
                    .Select(m => m.UserId)
                    .ToHashSet() ?? [];

                // Özümü və mövcud üzvləri çıxar
                memberSearchResults = result.Value
                    .Where(u => u.Id != currentUserId && !existingMemberIds.Contains(u.Id))
                    .ToList();
            }
            else
            {
                memberSearchResults.Clear();
            }
        }
        catch
        {
            memberSearchResults.Clear();
        }
        finally
        {
            isSearchingMembersForAdd = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Channel-ə üzv əlavə edir.
    /// Role verilə bilər (Member və ya Admin).
    /// </summary>
    private async Task AddMemberToChannel((Guid userId, ChannelMemberRole role) memberData)
    {
        if (!selectedChannelId.HasValue) return;

        try
        {
            // Əvvəlcə üzv əlavə et
            var addResult = await ChannelService.AddMemberAsync(selectedChannelId.Value, memberData.userId);
            if (addResult.IsFailure)
            {
                throw new Exception(addResult.Error ?? "Failed to add member");
            }

            // Role Admin-dirsə, role-u yenilə
            if (memberData.role == ChannelMemberRole.Admin)
            {
                var roleResult = await ChannelService.UpdateMemberRoleAsync(
                    selectedChannelId.Value,
                    memberData.userId,
                    memberData.role);
            }

            // Member count artır
            selectedChannelMemberCount++;

            // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
            var channelIndex = channelConversations.FindIndex(c => c.Id == selectedChannelId.Value);
            if (channelIndex >= 0)
            {
                var newList = new List<ChannelDto>(channelConversations);
                newList[channelIndex] = channelConversations[channelIndex] with
                {
                    MemberCount = selectedChannelMemberCount
                };
                channelConversations = newList;
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    #endregion
}