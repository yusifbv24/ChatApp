using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.Models.Search;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Dialog Methods - Dialog metodları

    /// <summary>
    /// Yeni conversation dialog-unu aç.
    /// </summary>
    private void OpenNewConversationDialog()
    {
        showNewConversationDialog = true;
        userSearchQuery = string.Empty;
        userSearchResults.Clear();
        StateHasChanged();
    }

    /// <summary>
    /// Yeni conversation dialog-unu bağla.
    /// </summary>
    private void CloseNewConversationDialog()
    {
        showNewConversationDialog = false;
        _searchCts?.Cancel();
        StateHasChanged();
    }

    /// <summary>
    /// Yeni channel dialog-unu aç.
    /// </summary>
    private void OpenNewChannelDialog()
    {
        showNewChannelDialog = true;
        newChannelRequest = new Models.Messages.CreateChannelRequest();
        StateHasChanged();
    }

    /// <summary>
    /// Yeni channel dialog-unu bağla.
    /// </summary>
    private void CloseNewChannelDialog()
    {
        showNewChannelDialog = false;
        StateHasChanged();
    }

    #endregion

    #region User Search - İstifadəçi axtarışı

    /// <summary>
    /// İstifadəçi axtarış input-u dəyişdikdə.
    /// DEBOUNCE pattern: 300ms gözləyir, sonra axtarır.
    /// Bu sayədə hər keystroke-da API çağrılmır.
    /// </summary>
    private async Task OnUserSearchInput(ChangeEventArgs e)
    {
        userSearchQuery = e.Value?.ToString() ?? string.Empty;

        // Əvvəlki axtarışı ləğv et
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        // Sorğu çox qısadırsa - nəticələri təmizlə
        if (string.IsNullOrWhiteSpace(userSearchQuery) || userSearchQuery.Length < 2)
        {
            userSearchResults.Clear();
            isSearchingUsers = false;
            StateHasChanged();
            return;
        }

        // DEBOUNCE - 300ms gözlə
        try
        {
            await Task.Delay(300, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await SearchUsers(token);
    }

    /// <summary>
    /// İstifadəçiləri axtar.
    /// </summary>
    private async Task SearchUsers(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        isSearchingUsers = true;
        StateHasChanged();

        try
        {
            var result = await UserService.SearchUsersAsync(userSearchQuery);

            if (token.IsCancellationRequested) return;

            if (result.IsSuccess)
            {
                userSearchResults = result.Value ?? [];
            }
            else
            {
                userSearchResults.Clear();
            }
        }
        catch
        {
            userSearchResults.Clear();
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                isSearchingUsers = false;
                StateHasChanged();
            }
        }
    }

    #endregion

    #region Message Search Panel - Mesaj axtarış paneli

    /// <summary>
    /// Search panel-i toggle et.
    /// </summary>
    private void ToggleSearchPanel()
    {
        showSearchPanel = !showSearchPanel;
    }

    /// <summary>
    /// Search panel-i bağla.
    /// </summary>
    private void CloseSearchPanel()
    {
        showSearchPanel = false;
    }

    /// <summary>
    /// Mesajları axtar.
    /// SearchPanel component-dən çağrılır.
    /// </summary>
    private async Task<SearchResultsDto?> SearchMessagesAsync(Guid targetId, string searchTerm, int page, int pageSize)
    {
        try
        {
            Result<SearchResultsDto> result;

            if (isDirectMessage)
            {
                result = await SearchService.SearchInConversationAsync(targetId, searchTerm, page, pageSize);
            }
            else
            {
                result = await SearchService.SearchInChannelAsync(targetId, searchTerm, page, pageSize);
            }

            return result.IsSuccess ? result.Value : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Axtarış nəticəsinə naviqasiya et.
    /// Mesaj yüklənməyibsə GetMessagesAround ilə yüklənir.
    /// </summary>
    private async Task NavigateToSearchResult(Guid messageId)
    {
        await NavigateToMessageAsync(messageId);
    }

    /// <summary>
    /// Mesaja scroll et və highlight et.
    ///
    /// LOGIC:
    /// 1. Mesaj yüklənib? Yoxsa - LoadMore çağır
    /// 2. Maksimum 20 cəhd (20*50=1000 mesaj)
    /// 3. Mesaj tapıldıqda - JS ilə scroll və highlight
    /// </summary>
    private async Task ScrollToAndHighlightMessage(Guid messageId)
    {
        try
        {
            // Mesaj artıq yüklənib?
            bool messageExists = isDirectMessage
                ? directMessages.Any(m => m.Id == messageId)
                : channelMessages.Any(m => m.Id == messageId);

            // Tapılana qədər daha çox mesaj yüklə
            int maxAttempts = 20;
            int attempts = 0;

            while (!messageExists && hasMoreMessages && attempts < maxAttempts)
            {
                await LoadMoreMessages();
                attempts++;

                messageExists = isDirectMessage
                    ? directMessages.Any(m => m.Id == messageId)
                    : channelMessages.Any(m => m.Id == messageId);

                StateHasChanged();
                await Task.Delay(50); // DOM update üçün gözlə
            }

            if (messageExists)
            {
                // DOM tam render olana qədər gözlə
                await Task.Delay(100);
                // JS ilə scroll və highlight
                await JS.InvokeVoidAsync("chatAppUtils.scrollToMessageAndHighlight", $"message-{messageId}");
            }
        }
        catch
        {
        }
    }

    #endregion

    #region Draft Management - Qaralama idarəetməsi

    /// <summary>
    /// Cari draft-ı saxla.
    /// Conversation/channel dəyişdikdə çağrılır.
    ///
    /// DRAFT KEY FORMAT:
    /// - conv_{conversationId} - Conversation üçün
    /// - chan_{channelId} - Channel üçün
    /// - pending_{userId} - Pending conversation üçün
    /// </summary>
    private void SaveCurrentDraft(string draft)
    {
        string? key = null;

        if (selectedConversationId.HasValue)
        {
            key = $"conv_{selectedConversationId.Value}";
        }
        else if (selectedChannelId.HasValue)
        {
            key = $"chan_{selectedChannelId.Value}";
        }
        else if (isPendingConversation && pendingUser != null)
        {
            key = $"pending_{pendingUser.Id}";
        }

        if (key == null) return;

        if (string.IsNullOrWhiteSpace(draft))
        {
            // Boş draft - sil
            messageDrafts.Remove(key);
        }
        else
        {
            // Draft saxla
            messageDrafts[key] = draft;
        }
    }

    /// <summary>
    /// Draft yüklə.
    /// Conversation/channel seçildikdə çağrılır.
    /// </summary>
    private string LoadDraft(Guid? conversationId, Guid? channelId, Guid? pendingUserId = null)
    {
        string? key = null;

        if (conversationId.HasValue)
        {
            key = $"conv_{conversationId.Value}";
        }
        else if (channelId.HasValue)
        {
            key = $"chan_{channelId.Value}";
        }
        else if (pendingUserId.HasValue)
        {
            key = $"pending_{pendingUserId.Value}";
        }

        if (key == null) return string.Empty;

        return messageDrafts.TryGetValue(key, out var draft) ? draft : string.Empty;
    }

    /// <summary>
    /// Draft dəyişdikdə çağrılır.
    /// MessageInput component-dən.
    /// </summary>
    private void HandleDraftChanged(string draft)
    {
        currentDraft = draft;
        SaveCurrentDraft(draft);
    }

    #endregion

    #region Update Methods - Yeniləmə metodları

    /// <summary>
    /// Conversation-u local olaraq yenilə.
    /// Son mesaj göndərildikdə çağrılır.
    /// </summary>
    private void UpdateConversationLocally(Guid conversationId, string lastMessage, DateTime messageTime)
    {
        var conversation = directConversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            var updatedConversation = conversation with
            {
                LastMessageContent = lastMessage,
                LastMessageAtUtc = messageTime,
                LastMessageSenderId = currentUserId
            };

            // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
            var newList = new List<DirectConversationDto>(directConversations.Count) { updatedConversation };
            newList.AddRange(directConversations.Where(c => c.Id != conversationId));
            directConversations = newList;

            StateHasChanged();
        }
    }

    /// <summary>
    /// Channel-ı local olaraq yenilə.
    /// </summary>
    private void UpdateChannelLocally(Guid channelId, string lastMessage, DateTime messageTime, string? senderName = null)
    {
        var channel = channelConversations.FirstOrDefault(c => c.Id == channelId);
        if (channel != null)
        {
            var updatedChannel = channel with
            {
                LastMessageContent = lastMessage,
                LastMessageAtUtc = messageTime,
                LastMessageSenderId = currentUserId
            };

            // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
            var newList = new List<ChannelDto>(channelConversations.Count) { updatedChannel };
            newList.AddRange(channelConversations.Where(c => c.Id != channelId));
            channelConversations = newList;

            StateHasChanged();
        }
    }

    /// <summary>
    /// Conversation-un son mesaj content-ini yenilə.
    /// Edit/delete zamanı çağrılır.
    /// </summary>
    private void UpdateConversationLastMessage(Guid conversationId, string newContent)
    {
        var convIndex = directConversations.FindIndex(c => c.Id == conversationId);
        if (convIndex >= 0)
        {
            var conv = directConversations[convIndex];
            // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
            var newList = new List<DirectConversationDto>(directConversations);
            newList[convIndex] = conv with { LastMessageContent = newContent };
            directConversations = newList;
        }
    }

    /// <summary>
    /// Channel-ın son mesaj content-ini yenilə.
    /// </summary>
    private void UpdateChannelLastMessage(Guid channelId, string newContent, string? senderName = null)
    {
        var channelIndex = channelConversations.FindIndex(c => c.Id == channelId);
        if (channelIndex >= 0)
        {
            var channel = channelConversations[channelIndex];
            // Yeni list yaradırıq ki cache invalidate olsun (ReferenceEquals)
            var newList = new List<ChannelDto>(channelConversations);
            newList[channelIndex] = channel with { LastMessageContent = newContent };
            channelConversations = newList;
        }
    }

    /// <summary>
    /// Mesajdan file preview string-ini çıxarır (conversation list üçün).
    /// Sadə format: [Image], [File]
    /// </summary>
    private string GetFilePreview(DirectMessageDto message)
    {
        if (message.FileId != null)
        {
            if (message.FileContentType != null && message.FileContentType.StartsWith("image/"))
            {
                return string.IsNullOrWhiteSpace(message.Content) ? "[Image]" : $"[Image] {message.Content}";
            }

            return string.IsNullOrWhiteSpace(message.Content) ? "[File]" : $"[File] {message.Content}";
        }
        return message.Content;
    }

    /// <summary>
    /// Mesajdan file preview string-ini çıxarır (conversation list üçün).
    /// Sadə format: [Image], [File]
    /// </summary>
    private string GetFilePreview(ChannelMessageDto message)
    {
        if (message.FileId != null)
        {
            if (message.FileContentType != null && message.FileContentType.StartsWith("image/"))
            {
                return string.IsNullOrWhiteSpace(message.Content) ? "[Image]" : $"[Image] {message.Content}";
            }

            return string.IsNullOrWhiteSpace(message.Content) ? "[File]" : $"[File] {message.Content}";
        }
        return message.Content;
    }

    /// <summary>
    /// File type-a görə emoji və label qaytarır.
    /// </summary>
    private string GetFileTypePrefix(string? contentType, string? fileName)
    {
        // Content type-a görə
        if (!string.IsNullOrEmpty(contentType))
        {
            if (contentType == "application/pdf")
                return "📄 PDF";

            if (contentType == "application/vnd.ms-excel" ||
                contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                return "📊 Excel";

            if (contentType == "application/msword" ||
                contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                return "📝 Word";

            if (contentType == "application/vnd.ms-powerpoint" ||
                contentType == "application/vnd.openxmlformats-officedocument.presentationml.presentation")
                return "📽️ PowerPoint";

            if (contentType.StartsWith("video/"))
                return "🎥 Video";

            if (contentType.StartsWith("audio/"))
                return "🎵 Audio";

            if (contentType == "application/zip" || contentType == "application/x-rar-compressed")
                return "🗜️ Archive";
        }

        // Extension-a görə fallback
        if (!string.IsNullOrEmpty(fileName))
        {
            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "📄 PDF",
                ".xls" or ".xlsx" => "📊 Excel",
                ".doc" or ".docx" => "📝 Word",
                ".ppt" or ".pptx" => "📽️ PowerPoint",
                ".zip" or ".rar" or ".7z" => "🗜️ Archive",
                ".mp4" or ".avi" or ".mov" => "🎥 Video",
                ".mp3" or ".wav" or ".flac" => "🎵 Audio",
                _ => "📎 File"
            };
        }

        return "📎 File";
    }

    /// <summary>
    /// Global unread count-u yenilə.
    /// </summary>
    private void UpdateGlobalUnreadCount()
    {
        var totalUnread = directConversations.Sum(c => c.UnreadCount) + channelConversations.Sum(c => c.UnreadCount);
        AppState.UnreadMessageCount = totalUnread;
    }

    /// <summary>
    /// Mesaj cache version-u artır.
    /// ChatArea cache-i invalidate etmək üçün çağrılır.
    /// In-place mesaj dəyişikliklərindən sonra (edit/delete/reaction/pin/read) çağrılmalıdır.
    /// </summary>
    private void InvalidateMessageCache()
    {
        messageCacheVersion++;
    }

    #endregion

    #region Helper Methods - Yardımçı metodlar

    /// <summary>
    /// Bu mesaj conversation-un son mesajıdır?
    /// Edit/delete zamanı conversation list-i yeniləmək üçün.
    /// </summary>
    private bool IsLastMessageInConversation(Guid conversationId, Guid messageId)
    {
        var conv = directConversations.FirstOrDefault(c => c.Id == conversationId);
        if (conv == null) return false;

        // Aktiv conversation-dayıqsa, yüklənmiş mesajları yoxla
        if (conversationId == selectedConversationId && directMessages.Count != 0)
        {
            var lastMessage = directMessages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
            return lastMessage?.Id == messageId;
        }

        // Başqa conversation - LastMessageId ilə müqayisə
        return conv.LastMessageId == messageId;
    }

    /// <summary>
    /// Bu mesaj channel-ın son mesajıdır?
    /// </summary>
    private bool IsLastMessageInChannel(Guid channelId, Guid messageId)
    {
        var channel = channelConversations.FirstOrDefault(c => c.Id == channelId);
        if (channel == null) return false;

        if (channelId == selectedChannelId && channelMessages.Count != 0)
        {
            var lastMessage = channelMessages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
            return lastMessage?.Id == messageId;
        }

        // Başqa channel - LastMessageId ilə müqayisə
        return channel.LastMessageId == messageId;
    }

    /// <summary>
    /// Unread separator pozisiyasını hesabla.
    /// İlk oxunmamış mesajın əvvəlinə "New messages" separator qoyuruq.
    /// </summary>
    private void CalculateUnreadSeparatorPosition<T>(
        List<T> messages,
        Func<T, bool> isUnreadPredicate,
        Func<T, Guid> getIdFunc,
        Func<T, DateTime> getCreatedAtFunc)
    {
        if (!shouldCalculateUnreadSeparator || messages.Count == 0)
            return;

        var orderedMessages = messages.OrderBy(getCreatedAtFunc).ToList();
        var unreadMessages = orderedMessages.Where(isUnreadPredicate).ToList();

        if (unreadMessages.Count > 0)
        {
            var firstUnread = unreadMessages.First();
            var firstUnreadIndex = orderedMessages.FindIndex(m => getIdFunc(m).Equals(getIdFunc(firstUnread)));

            if (firstUnreadIndex > 0)
            {
                // Separator əvvəlki mesajdan sonra qoyulur
                unreadSeparatorAfterMessageId = getIdFunc(orderedMessages[firstUnreadIndex - 1]);
            }
            else if (firstUnreadIndex == 0)
            {
                // CRITICAL FIX: İlk mesaj unread-dirsə (30+ unread mesaj senariusu)
                // Separator Guid.Empty ilə işarələnir və ən yuxarıda göstərilir
                unreadSeparatorAfterMessageId = Guid.Empty;
            }
        }

        shouldCalculateUnreadSeparator = false;
    }

    #endregion

    #region Debounced State Updates - Gecikmeli UI yeniləmə

    /// <summary>
    /// Debounce edilmiş StateHasChanged.
    /// 50ms ərzində bir neçə çağırış bir UI yeniləməyə birləşdirilir.
    ///
    /// NİYƏ LAZIMDIR?
    /// Typing/online event-ləri çox tez-tez gəlir.
    /// Hər birində StateHasChanged çağırsaq, UI freeze olur.
    /// Debounce ilə batch edirik.
    /// </summary>
    private void ScheduleStateUpdate()
    {
        lock (_stateChangeLock)
        {
            if (_stateChangeScheduled) return;
            _stateChangeScheduled = true;

            _stateChangeDebounceTimer?.Dispose();
            _stateChangeDebounceTimer = new Timer(_ =>
            {
                InvokeAsync(() =>
                {
                    lock (_stateChangeLock)
                    {
                        _stateChangeScheduled = false;
                    }
                    StateHasChanged();
                });
            }, null, 50, Timeout.Infinite);
        }
    }

    #endregion

    #region Mention Support - Mention dəstəyi

    /// <summary>
    /// Mention üçün user axtarışı.
    /// MessageInput component-dən çağrılır (@ simvolu trigger edir).
    /// </summary>
    private async Task<List<MentionUserDto>> SearchUsersForMention(string searchTerm)
    {
        try
        {
            var result = await UserService.SearchUsersAsync(searchTerm);

            if (result.IsSuccess && result.Value != null)
            {
                // UserDto-nu MentionUserDto-ya map et
                return result.Value.Select(u => new MentionUserDto
                {
                    Id = u.Id,
                    Name = u.DisplayName,
                    AvatarUrl = u.AvatarUrl,
                    IsMember = false, // User search-də member yoxdur
                    IsAll = false
                }).ToList();
            }

            return [];
        }
        catch
        {
            return [];
        }
    }

    #endregion

    #region Error Handling - Xəta idarəetməsi

    /// <summary>
    /// Error mesajını göstər.
    /// 5 saniyə sonra avtomatik gizlənir.
    /// </summary>
    private void ShowError(string message)
    {
        errorMessage = message;
        StateHasChanged();

        // Auto-hide: 5 saniyə sonra
        _ = Task.Delay(5000).ContinueWith(_ =>
        {
            InvokeAsync(() =>
            {
                if (errorMessage == message)
                {
                    errorMessage = null;
                    StateHasChanged();
                }
            });
        });
    }

    /// <summary>
    /// Error mesajını təmizlə.
    /// </summary>
    private void ClearError()
    {
        errorMessage = null;
        StateHasChanged();
    }

    #endregion
}