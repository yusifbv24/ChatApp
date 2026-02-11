using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Messages;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Load Conversations and Channels - İlkin yükləmə

    /// <summary>
    /// Unified conversation listini yükləyir.
    /// Tək API çağırışı ilə DM + Channel + Department users birlikdə gəlir.
    /// OnInitializedAsync-də və yeni conversation yaradıldıqda çağrılır.
    /// </summary>
    private async Task LoadConversationsAndChannels()
    {
        // Guard: Artıq yüklənir
        if (isLoadingConversationList) return;

        isLoadingConversationList = true;
        unifiedPageNumber = 1;
        StateHasChanged();

        try
        {
            var result = await ConversationService.GetUnifiedListAsync(1, ConversationListPageSize);

            if (result.IsSuccess && result.Value != null)
            {
                var response = result.Value;

                // Unified response-dan ayrı listlərə parse et
                directConversations = response.Items
                    .Where(i => i.Type == UnifiedChatItemType.Conversation)
                    .Select(MapToDirectConversationDto)
                    .ToList();

                channelConversations = response.Items
                    .Where(i => i.Type == UnifiedChatItemType.Channel)
                    .Select(MapToChannelDto)
                    .ToList();

                departmentUsers = response.Items
                    .Where(i => i.Type == UnifiedChatItemType.DepartmentUser)
                    .Select(MapToDepartmentUserDto)
                    .ToList();

                hasMoreConversationItems = response.HasNextPage;
            }
            else
            {
                directConversations = [];
                channelConversations = [];
                departmentUsers = [];
                hasMoreConversationItems = false;
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

    /// <summary>
    /// Daha çox conversation item yüklə (infinite scroll).
    /// Page 2+ yalnız department users gətirir (conversations/channels page 1-də tam yüklənib).
    /// </summary>
    private async Task LoadMoreConversationItems()
    {
        if (isLoadingMoreItems || !hasMoreConversationItems) return;

        isLoadingMoreItems = true;
        StateHasChanged();

        try
        {
            unifiedPageNumber++;
            var result = await ConversationService.GetUnifiedListAsync(unifiedPageNumber, ConversationListPageSize);

            if (result.IsSuccess && result.Value != null)
            {
                var response = result.Value;

                // Page 2+ yalnız department users gətirir
                // DUPLICATE FIX: Artıq mövcud olan user-ləri əlavə etmə
                var existingUserIds = departmentUsers.Select(u => u.UserId).ToHashSet();
                var newUsers = response.Items
                    .Where(i => i.Type == UnifiedChatItemType.DepartmentUser)
                    .Select(MapToDepartmentUserDto)
                    .Where(u => !existingUserIds.Contains(u.UserId))
                    .ToList();

                // CRITICAL: Yeni list yaradırıq ki ReferenceEquals dəyişsin və cache invalidate olsun
                departmentUsers = [..departmentUsers, ..newUsers];
                hasMoreConversationItems = response.HasNextPage;
            }
            else
            {
                hasMoreConversationItems = false;
            }
        }
        catch
        {
            hasMoreConversationItems = false;
        }
        finally
        {
            isLoadingMoreItems = false;
            StateHasChanged();
        }
    }

    #region Unified DTO Mappers

    private static DirectConversationDto MapToDirectConversationDto(UnifiedChatItemDto item) => new(
        Id: item.Id,
        OtherUserId: item.OtherUserId ?? Guid.Empty,
        OtherUserEmail: item.OtherUserEmail ?? string.Empty,
        OtherUserFullName: item.Name,
        OtherUserAvatarUrl: item.AvatarUrl,
        OtherUserPosition: item.OtherUserPosition,
        OtherUserRole: item.OtherUserRole,
        OtherUserLastSeenAtUtc: item.OtherUserLastSeenAtUtc,
        LastMessageContent: item.LastMessage,
        LastMessageAtUtc: item.LastMessageAtUtc ?? DateTime.MinValue,
        UnreadCount: item.UnreadCount,
        HasUnreadMentions: item.HasUnreadMentions,
        LastReadLaterMessageId: item.LastReadLaterMessageId,
        LastMessageSenderId: item.LastMessageSenderId,
        LastMessageStatus: item.LastMessageStatus,
        LastMessageId: item.LastMessageId,
        FirstUnreadMessageId: item.FirstUnreadMessageId,
        IsNotes: item.IsNotes,
        IsPinned: item.IsPinned,
        IsMuted: item.IsMuted,
        IsMarkedReadLater: item.IsMarkedReadLater
    );

    private static ChannelDto MapToChannelDto(UnifiedChatItemDto item) => new(
        Id: item.Id,
        Name: item.Name,
        Description: item.ChannelDescription,
        Type: Enum.TryParse<ChannelType>(item.ChannelType, out var ct) ? ct : ChannelType.Public,
        CreatedBy: item.CreatedBy ?? Guid.Empty,
        MemberCount: item.MemberCount ?? 0,
        IsArchived: false,
        CreatedAtUtc: item.CreatedAtUtc ?? DateTime.UtcNow,
        ArchivedAtUtc: null,
        AvatarUrl: item.AvatarUrl,
        LastMessageContent: item.LastMessage,
        LastMessageAtUtc: item.LastMessageAtUtc,
        UnreadCount: item.UnreadCount,
        HasUnreadMentions: item.HasUnreadMentions,
        LastReadLaterMessageId: item.LastReadLaterMessageId,
        LastMessageId: item.LastMessageId,
        LastMessageSenderId: item.LastMessageSenderId,
        LastMessageStatus: item.LastMessageStatus,
        LastMessageSenderAvatarUrl: item.LastMessageSenderAvatarUrl,
        LastMessageSenderFullName: item.LastMessageSenderFullName,
        FirstUnreadMessageId: item.FirstUnreadMessageId,
        IsPinned: item.IsPinned,
        IsMuted: item.IsMuted,
        IsMarkedReadLater: item.IsMarkedReadLater
    );

    private static DepartmentUserDto MapToDepartmentUserDto(UnifiedChatItemDto item) => new(
        UserId: item.Id,
        FullName: item.Name,
        Email: item.Email ?? item.OtherUserEmail ?? string.Empty,
        AvatarUrl: item.AvatarUrl,
        PositionName: item.PositionName,
        DepartmentId: null,
        DepartmentName: item.DepartmentName
    );

    #endregion

    /// <summary>
    /// ProfilePanel-dən chat başlatma tələbini emal edir.
    /// AppState.PendingChatUserId-ni oxuyub sıfırlayır.
    /// Mövcud conversation varsa seçir, yoxdursa pending mode-da açır.
    /// </summary>
    private async Task HandlePendingChatUserAsync()
    {
        var pendingUserId = AppState.ConsumePendingChatUserId();
        if (pendingUserId == null) return;

        // Öz profilimiz üçün chat açmağa ehtiyac yoxdur
        if (pendingUserId.Value == currentUserId) return;

        // Mövcud conversation varsa, birbaşa onu seç
        var existingConversation = directConversations.FirstOrDefault(c => c.OtherUserId == pendingUserId.Value);
        if (existingConversation != null)
        {
            await SelectDirectConversation(existingConversation);
            return;
        }

        // Mövcud conversation yoxdur - user məlumatını al və pending mode-da aç
        try
        {
            var userResult = await UserService.GetUserByIdAsync(pendingUserId.Value);
            if (userResult.IsSuccess && userResult.Value != null)
            {
                var user = userResult.Value;
                var searchUser = new UserSearchResultDto(
                    Id: user.Id,
                    FirstName: user.FirstName,
                    LastName: user.LastName,
                    Email: user.Email,
                    AvatarUrl: user.AvatarUrl,
                    Position: user.Position
                );

                userSearchResults = [searchUser];
                await StartConversationWithUser(user.Id);
            }
        }
        catch
        {
            // Silently handle - user mövcud deyilsə heç nə etmə
        }
    }

    /// <summary>
    /// Department istifadəçisi ilə conversation yarat.
    /// StartConversationWithUser ilə eyni pending conversation pattern istifadə edir.
    /// Full reload yoxdur - yalnız UI state hazırlanır, ilk mesajda conversation yaranır.
    /// </summary>
    private async Task StartConversationWithDepartmentUser(DepartmentUserDto user)
    {
        // DepartmentUserDto → UserSearchResultDto çevir (StartConversationWithUser tələb edir)
        var nameParts = user.FullName.Split(' ', 2);
        var searchUser = new UserSearchResultDto(
            Id: user.UserId,
            FirstName: nameParts[0],
            LastName: nameParts.Length > 1 ? nameParts[1] : string.Empty,
            Email: user.Email,
            AvatarUrl: user.AvatarUrl,
            Position: user.PositionName
        );

        // userSearchResults-a əlavə et (StartConversationWithUser oradan oxuyur)
        userSearchResults = [searchUser];
        await StartConversationWithUser(user.UserId);
    }

    /// <summary>
    /// Closes the currently selected conversation or channel.
    /// Called when conversation is closed from ConversationList (e.g., toggle mark read later)
    /// </summary>
    private void CloseConversation()
    {
        selectedConversationId = null;
        selectedChannelId = null;
        isDirectMessage = false;

        // Clear all conversation-specific state
        directMessages.Clear();
        channelMessages.Clear();

        // Close sidebar if open
        showSidebar = false;

        StateHasChanged();
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

        // Create group paneli açıqdırsa bağla
        if (showCreateGroupPanel)
        {
            CloseCreateGroupPanel();
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
                catch
                {
                    // Silently handle mark-as-read errors
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

            // Unread count, mention badge, FirstUnreadMessageId VƏ read later marks clear et
            // CRITICAL: FirstUnreadMessageId clear edilməlidir ki, geri qayıtdıqda köhnə mesaja scroll olunmasın
            // CRITICAL: IsMarkedReadLater və LastReadLaterMessageId də clear edilməlidir (conversation açıldıqda icon yox olmalıdır)
            if (conversation.UnreadCount > 0 || conversation.HasUnreadMentions || conversation.FirstUnreadMessageId.HasValue
                || conversation.IsMarkedReadLater || conversation.LastReadLaterMessageId.HasValue)
            {
                // CRITICAL FIX: Save original state for revert if API fails
                var originalIsMarkedReadLater = conversation.IsMarkedReadLater;
                var originalLastReadLaterMessageId = conversation.LastReadLaterMessageId;

                if (conversation.UnreadCount > 0)
                {
                    AppState.DecrementUnreadMessages(conversation.UnreadCount);
                }

                // Optimistic UI update: Clear flags
                UpdateListItemWhere(
                    ref directConversations,
                    c => c.Id == conversation.Id,
                    c => c with
                    {
                        UnreadCount = 0,
                        HasUnreadMentions = false,
                        FirstUnreadMessageId = null, // Clear after reading
                        IsMarkedReadLater = false,   // Clear conversation-level mark
                        LastReadLaterMessageId = null // Clear message-level mark
                    }
                );

                // API call to backend - only if flags were set
                if (originalIsMarkedReadLater || originalLastReadLaterMessageId.HasValue)
                {
                    try
                    {
                        var result = await ConversationService.UnmarkConversationReadLaterAsync(conversation.Id);
                        if (!result.IsSuccess)
                        {
                            // CRITICAL FIX: Revert optimistic update on failure
                            UpdateListItemWhere(
                                ref directConversations,
                                c => c.Id == conversation.Id,
                                c => c with
                                {
                                    IsMarkedReadLater = originalIsMarkedReadLater,
                                    LastReadLaterMessageId = originalLastReadLaterMessageId
                                }
                            );

                            ShowError($"Failed to unmark conversation: {result.Error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // CRITICAL FIX: Revert on exception
                        UpdateListItemWhere(
                            ref directConversations,
                            c => c.Id == conversation.Id,
                            c => c with
                            {
                                IsMarkedReadLater = originalIsMarkedReadLater,
                                LastReadLaterMessageId = originalLastReadLaterMessageId
                            }
                        );

                        ShowError($"Error unmarking conversation: {ex.Message}");
                    }
                }
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

            recipientName = conversation.IsNotes ? "Notes" : conversation.OtherUserFullName;
            recipientUserId = conversation.OtherUserId;
            isNotesConversation = conversation.IsNotes;

            // Notes conversation-da recipient məlumatları lazım deyil - null edək
            // Bu Edge browser-də state caching probleminin qarşısını alır
            if (conversation.IsNotes)
            {
                recipientAvatarUrl = null;
                recipientPosition = null;
                recipientRole = null;
                recipientLastSeenAt = null;
            }
            else
            {
                recipientAvatarUrl = conversation.OtherUserAvatarUrl;
                recipientPosition = conversation.OtherUserPosition;
                recipientRole = conversation.OtherUserRole;
                recipientLastSeenAt = conversation.OtherUserLastSeenAtUtc;
            }

            // Conversation preferences
            selectedConversationIsPinned = conversation.IsPinned;
            selectedConversationIsMuted = conversation.IsMuted;
            selectedConversationIsMarkedReadLater = conversation.IsMarkedReadLater;

            // Mention data: DM üçün conversation partner
            currentConversationPartner = new MentionUserDto
            {
                Id = conversation.OtherUserId,
                Name = conversation.OtherUserFullName,
                AvatarUrl = conversation.OtherUserAvatarUrl,
                IsMember = true,
                IsAll = false
            };
            currentChannelMembers = []; // DM-də channel member yoxdur

            // Online status-u real-time yoxla
            isRecipientOnline = await SignalRService.IsUserOnlineAsync(conversation.OtherUserId);

            // Draft yüklə
            DraftManager.CurrentDraft = LoadDraft(conversation.Id, null);

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
                await Task.Yield(); // Blazor render cycle tamamlansın

                // Scroll and show (no flash - container was hidden during load)
                // JS funksiyası şəkillərin yüklənməsini gözləyir (max 400ms)
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
        isNotesConversation = false;

        recipientUserId = user.Id;
        recipientName = user.FullName;
        recipientAvatarUrl = user.AvatarUrl;
        recipientPosition = user.Position;
        recipientRole = null;
        recipientLastSeenAt = null;
        isRecipientOnline = await SignalRService.IsUserOnlineAsync(user.Id);

        // Mention data: Pending conversation üçün conversation partner
        currentConversationPartner = new MentionUserDto
        {
            Id = user.Id,
            Name = user.FullName,
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
        DraftManager.CurrentDraft = LoadDraft(null, null, user.Id);

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

        // Create group paneli açıqdırsa bağla
        if (showCreateGroupPanel)
        {
            CloseCreateGroupPanel();
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
                catch
                {
                    // Silently handle mark-as-read errors
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
                await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
            }

            // Unread count, FirstUnreadMessageId VƏ read later marks clear et
            // CRITICAL: FirstUnreadMessageId clear edilməlidir ki, geri qayıtdıqda köhnə mesaja scroll olunmasın
            // CRITICAL: IsMarkedReadLater və LastReadLaterMessageId də clear edilməlidir (channel açıldıqda icon yox olmalıdır)
            if (channel.UnreadCount > 0 || channel.FirstUnreadMessageId.HasValue
                || channel.IsMarkedReadLater || channel.LastReadLaterMessageId.HasValue)
            {
                // CRITICAL FIX: Save original state for revert if API fails
                var originalIsMarkedReadLater = channel.IsMarkedReadLater;
                var originalLastReadLaterMessageId = channel.LastReadLaterMessageId;

                if (channel.UnreadCount > 0)
                {
                    AppState.DecrementUnreadMessages(channel.UnreadCount);
                }

                // Optimistic UI update: Clear flags
                UpdateListItemWhere(
                    ref channelConversations,
                    ch => ch.Id == channel.Id,
                    ch => ch with
                    {
                        UnreadCount = 0,
                        FirstUnreadMessageId = null, // Clear after reading
                        IsMarkedReadLater = false,   // Clear conversation-level mark
                        LastReadLaterMessageId = null // Clear message-level mark
                    }
                );

                // API call to backend - only if flags were set
                if (originalIsMarkedReadLater || originalLastReadLaterMessageId.HasValue)
                {
                    try
                    {
                        var result = await ChannelService.UnmarkChannelReadLaterAsync(channel.Id);
                        if (!result.IsSuccess)
                        {
                            // CRITICAL FIX: Revert optimistic update on failure
                            UpdateListItemWhere(
                                ref channelConversations,
                                ch => ch.Id == channel.Id,
                                ch => ch with
                                {
                                    IsMarkedReadLater = originalIsMarkedReadLater,
                                    LastReadLaterMessageId = originalLastReadLaterMessageId
                                }
                            );

                            ShowError($"Failed to unmark channel: {result.Error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // CRITICAL FIX: Revert on exception
                        UpdateListItemWhere(
                            ref channelConversations,
                            ch => ch.Id == channel.Id,
                            ch => ch with
                            {
                                IsMarkedReadLater = originalIsMarkedReadLater,
                                LastReadLaterMessageId = originalLastReadLaterMessageId
                            }
                        );

                        ShowError($"Error unmarking channel: {ex.Message}");
                    }
                }
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
            isNotesConversation = false; // Channel-də Notes yoxdur
            selectedChannelName = channel.Name;
            selectedChannelAvatarUrl = channel.AvatarUrl;
            selectedChannelDescription = channel.Description;
            selectedChannelType = channel.Type;
            selectedChannelMemberCount = channel.MemberCount;

            // Channel preferences
            selectedConversationIsPinned = channel.IsPinned;
            selectedConversationIsMuted = channel.IsMuted;
            selectedConversationIsMarkedReadLater = channel.IsMarkedReadLater;

            // Draft yüklə
            DraftManager.CurrentDraft = LoadDraft(null, channel.Id);

            // ADMIN YOXLAMASI və MENTION DATA
            // Channel yaradıcısı avtomatik admin-dir
            isChannelAdmin = channel.CreatedBy == currentUserId;
            currentUserChannelRole = isChannelAdmin ? MemberRole.Owner : MemberRole.Member;
            // Default true: ConversationList-dəki channel-lar artıq join olunmuş channel-lardır
            // API cavabında dəqiq yoxlanılacaq (line ~812)
            isCurrentUserChannelMember = true;

            // Mention data: Channel üçün member-lər (@All MessageInput-da dinamik əlavə olunur)
            currentConversationPartner = null; // Channel-da conversation partner yoxdur
            currentChannelMembers = [];

            // PERFORMANCE: Fetch channel details once (was called twice: admin check + mentions)
            var channelDetails = await ChannelService.GetChannelAsync(channel.Id);
            if (channelDetails.IsSuccess && channelDetails.Value != null)
            {
                // Member check - istifadəçi channel-ın üzvüdür?
                var currentMember = channelDetails.Value.Members.FirstOrDefault(m => m.UserId == currentUserId);
                isCurrentUserChannelMember = currentMember != null;

                // Yaradıcı deyilsə, role-u yoxla (admin check)
                if (!isChannelAdmin && currentMember != null)
                {
                    currentUserChannelRole = currentMember.Role;
                    isChannelAdmin = currentMember.Role == MemberRole.Admin ||
                                    currentMember.Role == MemberRole.Owner;
                }

                // Channel member-lərini mention üçün əlavə et
                var memberDtos = channelDetails.Value.Members
                    .Where(m => m.UserId != currentUserId) // Özünü çıxar
                    .Select(m => new MentionUserDto
                    {
                        Id = m.UserId,
                        Name = m.FullName,
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
                await Task.Yield(); // Blazor render cycle tamamlansın

                // Scroll and show (no flash - container was hidden during load)
                // JS funksiyası şəkillərin yüklənməsini gözləyir (max 400ms)
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

                    // CRITICAL FIX: Calculate status for loaded messages
                    CalculateDirectMessageStatuses(newMessages);

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

                    // CRITICAL FIX: Calculate status for loaded messages
                    CalculateChannelMessageStatuses(newMessages);

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
                    await Task.Yield(); // Blazor render cycle tamamlansın
                    await Task.Delay(100); // DOM stabillik üçün

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
                    await Task.Yield(); // Blazor render cycle tamamlansın
                    await Task.Delay(100); // DOM stabillik üçün

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
    private async Task AddMemberToChannel((Guid userId, MemberRole role) memberData)
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
            if (memberData.role == MemberRole.Admin)
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

    #region Join Channel - Public channel-a qoşulma

    /// <summary>
    /// Public channel-a qoşulur.
    /// Channel-a join etdikdən sonra message input aktiv olur.
    /// </summary>
    private async Task HandleJoinChannel()
    {
        if (!selectedChannelId.HasValue) return;

        try
        {
            var result = await ChannelService.JoinChannelAsync(selectedChannelId.Value);
            if (result.IsSuccess)
            {
                // Join uğurlu - state-i yenilə
                isCurrentUserChannelMember = true;
                selectedChannelMemberCount++;

                // Conversation list-də channel-ı tap və ya əlavə et
                var channelIndex = channelConversations.FindIndex(c => c.Id == selectedChannelId.Value);
                if (channelIndex >= 0)
                {
                    // Mövcud channel - member count yenilə
                    var newList = new List<ChannelDto>(channelConversations);
                    newList[channelIndex] = channelConversations[channelIndex] with
                    {
                        MemberCount = selectedChannelMemberCount
                    };
                    channelConversations = newList;
                }
                else
                {
                    // Channel list-də yoxdur (axtarışdan gəldi) - əlavə et
                    var newChannel = new ChannelDto(
                        Id: selectedChannelId.Value,
                        Name: selectedChannelName,
                        Description: selectedChannelDescription,
                        Type: selectedChannelType,
                        CreatedBy: Guid.Empty, // Bilinmir
                        MemberCount: selectedChannelMemberCount,
                        IsArchived: false,
                        CreatedAtUtc: DateTime.UtcNow,
                        ArchivedAtUtc: null,
                        AvatarUrl: selectedChannelAvatarUrl
                    );

                    // Yeni list yarat (reference change üçün - cache invalidation)
                    channelConversations = [newChannel, .. channelConversations];
                }

                // Channel members siyahısına özümü əlavə et (mention üçün)
                currentChannelMembers.Add(new MentionUserDto
                {
                    Id = currentUserId,
                    Name = UserState.FullName ?? "You",
                    AvatarUrl = UserState.AvatarUrl,
                    IsMember = true,
                    IsAll = false
                });

                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to join channel");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to join channel: {ex.Message}");
        }
    }

    /// <summary>
    /// Channel adını dəyişdirir.
    /// Admin tərəfindən header-dan edit edilir.
    /// </summary>
    private async Task HandleChannelNameChanged(string newName)
    {
        if (!selectedChannelId.HasValue || string.IsNullOrWhiteSpace(newName)) return;

        try
        {
            var result = await ChannelService.UpdateChannelAsync(selectedChannelId.Value, new UpdateChannelRequest
            {
                Name = newName.Trim()
            });

            if (result.IsSuccess)
            {
                // Local state-i yenilə
                selectedChannelName = newName.Trim();

                // Channel list-də yenilə
                var channelIndex = channelConversations.FindIndex(c => c.Id == selectedChannelId.Value);
                if (channelIndex >= 0)
                {
                    var updatedChannel = channelConversations[channelIndex] with { Name = newName.Trim() };
                    var newList = new List<ChannelDto>(channelConversations);
                    newList[channelIndex] = updatedChannel;
                    channelConversations = newList;
                }

                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to update channel name");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to update channel name: {ex.Message}");
        }
    }

    #endregion
}