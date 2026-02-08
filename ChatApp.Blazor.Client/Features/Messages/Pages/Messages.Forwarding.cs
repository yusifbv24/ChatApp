using System.Globalization;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Forward - Mesaj yönləndirmə

    /// <summary>
    /// Çoxlu forward üçün mesaj ID-ləri (tarix sırasına görə).
    /// SelectionToolbar-dan forward edildikdə doldurulur.
    /// </summary>
    private List<Guid> forwardingMultipleMessageIds = [];

    /// <summary>
    /// Forward dialog-unu açır.
    /// MessageBubble component-dən çağrılır.
    /// </summary>
    private void HandleForward(Guid messageId)
    {
        // Forward ediləcək mesajı tap
        if (isDirectMessage)
        {
            var message = directMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                forwardingDirectMessage = message;
                forwardingChannelMessage = null;
                showForwardDialog = true;
                StateHasChanged();
            }
        }
        else
        {
            var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                forwardingChannelMessage = message;
                forwardingDirectMessage = null;
                showForwardDialog = true;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Forward dialog-unu bağla.
    /// </summary>
    private void CancelForward()
    {
        showForwardDialog = false;
        forwardingDirectMessage = null;
        forwardingChannelMessage = null;
        forwardingMultipleMessageIds.Clear();
        forwardSearchQuery = string.Empty;
        isForwardSearchActive = false;
        isForwardSearching = false;
        forwardUserSearchResults = [];
        forwardChannelSearchResults = [];
        StateHasChanged();
    }

    /// <summary>
    /// Forward search input-a focus olundu.
    /// </summary>
    private void EnterForwardSearchMode()
    {
        isForwardSearchActive = true;
    }

    /// <summary>
    /// Forward search input focus itirdi.
    /// </summary>
    private async Task HandleForwardSearchBlur()
    {
        await Task.Delay(200); // Click-ləri tutmaq üçün
        if (string.IsNullOrWhiteSpace(forwardSearchQuery))
        {
            isForwardSearchActive = false;
            forwardUserSearchResults = [];
            forwardChannelSearchResults = [];
        }
    }

    /// <summary>
    /// Forward search sorğusunu icra et.
    /// </summary>
    private async Task HandleForwardSearchInput()
    {
        if (string.IsNullOrWhiteSpace(forwardSearchQuery) || forwardSearchQuery.Length < 2)
        {
            forwardUserSearchResults = [];
            forwardChannelSearchResults = [];
            return;
        }

        isForwardSearching = true;
        StateHasChanged();

        try
        {
            // Paralel axtarış
            var userTask = ConversationService.SearchUsersAsync(forwardSearchQuery);
            var channelTask = ChannelService.SearchChannelsAsync(forwardSearchQuery);

            await Task.WhenAll(userTask, channelTask);

            var userResult = await userTask;
            var channelResult = await channelTask;

            forwardUserSearchResults = userResult.IsSuccess && userResult.Value != null
                ? userResult.Value
                : [];

            forwardChannelSearchResults = channelResult.IsSuccess && channelResult.Value != null
                ? channelResult.Value
                : [];
        }
        catch
        {
            forwardUserSearchResults = [];
            forwardChannelSearchResults = [];
        }
        finally
        {
            isForwardSearching = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Forward search-i təmizlə.
    /// </summary>
    private void ClearForwardSearch()
    {
        forwardSearchQuery = string.Empty;
        isForwardSearchActive = false;
        forwardUserSearchResults = [];
        forwardChannelSearchResults = [];
        StateHasChanged();
    }

    /// <summary>
    /// Forward search-dən istifadəçiyə mesaj göndər.
    /// </summary>
    private async Task ForwardToSearchedUser(UserSearchResultDto user)
    {
        try
        {
            var content = forwardingDirectMessage?.Content ?? forwardingChannelMessage?.Content;
            if (string.IsNullOrEmpty(content)) return;

            // Conversation tap və ya yarat
            var convResult = await ConversationService.StartConversationAsync(user.Id);
            if (!convResult.IsSuccess)
            {
                ShowError("Failed to create conversation");
                return;
            }

            await ForwardToConversation(convResult.Value);
        }
        catch (Exception ex)
        {
            ShowError("Failed to forward message: " + ex.Message);
        }
    }

    /// <summary>
    /// Forward search-dən channel-a mesaj göndər.
    /// </summary>
    private async Task ForwardToSearchedChannel(ChannelDto channel)
    {
        await ForwardToChannel(channel.Id);
    }

    /// <summary>
    /// Mesaj ID-lərindən content-ləri əldə et.
    /// Çoxlu forward zamanı bütün mesajların content-lərini qaytarır.
    /// </summary>
    private List<string> GetForwardMessageContents()
    {
        var messageIds = forwardingMultipleMessageIds.Count > 0
            ? forwardingMultipleMessageIds
            : new List<Guid> { forwardingDirectMessage?.Id ?? forwardingChannelMessage?.Id ?? Guid.Empty };

        var contents = new List<string>();
        foreach (var id in messageIds)
        {
            string? content = null;
            if (isDirectMessage)
            {
                content = directMessages.FirstOrDefault(m => m.Id == id)?.Content;
            }
            else
            {
                content = channelMessages.FirstOrDefault(m => m.Id == id)?.Content;
            }
            if (!string.IsNullOrEmpty(content))
            {
                contents.Add(content);
            }
        }

        // Fallback: əgər heç bir content tapılmadısa, tək mesajdan al
        if (contents.Count == 0)
        {
            var fallback = forwardingDirectMessage?.Content ?? forwardingChannelMessage?.Content;
            if (!string.IsNullOrEmpty(fallback))
                contents.Add(fallback);
        }

        return contents;
    }

    /// <summary>
    /// Mesajı conversation-a forward et.
    ///
    /// İŞ AXINI:
    /// 1. Mesaj content-ini al
    /// 2. Yeni mesaj göndər (isForwarded=true)
    /// 3. Aktiv conversation-a forward edilirsə - optimistic UI
    /// 4. Conversation list-i yenilə
    /// </summary>
    private async Task ForwardToConversation(Guid conversationId)
    {
        try
        {
            var contents = GetForwardMessageContents();
            if (contents.Count == 0) return;

            // İlk mesaj üçün əvvəlki davranış (son göndərilən mesaj conversation list-i yeniləyir)
            var content = contents[0];

            var result = await ConversationService.SendMessageAsync(
                conversationId,
                content,
                fileId: null,
                replyToMessageId: null,
                isForwarded: true);

            if (result.IsSuccess)
            {
                var messageTime = DateTime.UtcNow;
                var messageId = result.Value;

                // Aktiv conversation-a forward edilirsə - UI-a əlavə et
                if (conversationId == selectedConversationId)
                {
                    var conversation = directConversations.FirstOrDefault(c => c.Id == conversationId);
                    var newMessage = new DirectMessageDto(
                        messageId,
                        conversationId,
                        currentUserId,
                        UserState.CurrentUser?.Email ?? "",
                        UserState.CurrentUser?.FullName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        conversation?.OtherUserId ?? Guid.Empty,
                        content,
                        null,                   // FileId
                        null,                   // FileName
                        null,                   // FileContentType
                        null,                   // FileSizeInBytes
                        null,                   // FileUrl
                        null,                   // ThumbnailUrl
                        false,                  // IsEdited
                        false,                  // IsDeleted
                        false,                  // IsRead
                        false,                  // IsPinned
                        0,                      // ReactionCount
                        messageTime,            // CreatedAtUtc
                        null,                   // EditedAtUtc
                        null,                   // PinnedAtUtc
                        null,                   // ReplyToMessageId
                        null,                   // ReplyToContent
                        null,                   // ReplyToSenderName
                        null,                   // ReplyToFileId
                        null,                   // ReplyToFileName
                        null,                   // ReplyToFileContentType
                        null,                   // ReplyToFileUrl
                        null,                   // ReplyToThumbnailUrl
                        true,                   // IsForwarded
                        null);                  // Reactions

                    // Dublikat yoxla
                    if (!directMessages.Any(m => m.Id == messageId))
                    {
                        directMessages.Add(newMessage);
                    }

                    // SignalR dublikatının qarşısını al
                    processedMessageIds.Add(messageId);
                }

                // Conversation list-i yenilə
                // Forward olunanda file attachment-lər transfer olunmur, ona görə preview sadəcə content-dir
                UpdateConversationLocally(conversationId, content, messageTime);

                // API uğurlu olduğu üçün statusu dərhal "Sent" edək
                UpdateListItemWhere(
                    ref directConversations,
                    c => c.Id == conversationId && c.LastMessageSenderId == currentUserId && c.LastMessageStatus == "Pending",
                    c => c with { LastMessageStatus = "Sent", LastMessageId = messageId }
                );

                // Qalan mesajları ardıcıl forward et (əgər çoxlu seçim varsa)
                for (int i = 1; i < contents.Count; i++)
                {
                    await ConversationService.SendMessageAsync(
                        conversationId,
                        contents[i],
                        fileId: null,
                        replyToMessageId: null,
                        isForwarded: true);
                }

                // Seçim rejimindən çıx
                if (isSelectingMessageBuble)
                {
                    ToggleSelectMode();
                }

                CancelForward();
            }
            else
            {
                ShowError(result.Error ?? "Failed to forward message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to forward message: " + ex.Message);
        }
    }

    /// <summary>
    /// Mesajı channel-a forward et.
    /// </summary>
    private async Task ForwardToChannel(Guid channelId)
    {
        try
        {
            var contents = GetForwardMessageContents();
            if (contents.Count == 0) return;

            var content = contents[0];

            var result = await ChannelService.SendMessageAsync(
                channelId,
                content,
                fileId: null,
                replyToMessageId: null,
                isForwarded: true);

            if (result.IsSuccess)
            {
                var messageTime = DateTime.UtcNow;
                var messageId = result.Value;

                // Aktiv channel-a forward edilirsə
                if (channelId == selectedChannelId)
                {
                    var totalMembers = Math.Max(0, selectedChannelMemberCount - 1);

                    var newMessage = new ChannelMessageDto(
                        messageId,
                        channelId,
                        currentUserId,
                        UserState.CurrentUser?.Email ?? "",
                        UserState.CurrentUser?.FullName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        content,
                        null,                                       // FileId
                        null,                                       // FileName
                        null,                                       // FileContentType
                        null,                                       // FileSizeInBytes
                        null,                                       // FileUrl
                        null,                                       // ThumbnailUrl
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
                        null,                                       // ReplyToFileUrl
                        null,                                       // ReplyToThumbnailUrl
                        true,                                       // IsForwarded
                        0,                                          // ReadByCount
                        totalMembers,                               // TotalMemberCount
                        [],                                         // ReadBy
                        []);

                    if (!channelMessages.Any(m => m.Id == messageId))
                    {
                        channelMessages.Add(newMessage);
                    }

                    processedMessageIds.Add(messageId);
                }

                // Forward olunanda file attachment-lər transfer olunmur, ona görə preview sadəcə content-dir
                UpdateChannelLocally(channelId, content, messageTime, UserState.CurrentUser?.FullName);

                // API uğurlu olduğu üçün statusu dərhal "Sent" edək
                UpdateListItemWhere(
                    ref channelConversations,
                    c => c.Id == channelId && c.LastMessageSenderId == currentUserId && c.LastMessageStatus == "Pending",
                    c => c with { LastMessageStatus = "Sent", LastMessageId = messageId }
                );

                // Qalan mesajları ardıcıl forward et (əgər çoxlu seçim varsa)
                for (int i = 1; i < contents.Count; i++)
                {
                    await ChannelService.SendMessageAsync(
                        channelId,
                        contents[i],
                        fileId: null,
                        replyToMessageId: null,
                        isForwarded: true);
                }

                if (isSelectingMessageBuble)
                {
                    ToggleSelectMode();
                }

                CancelForward();
            }
            else
            {
                ShowError(result.Error ?? "Failed to forward message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to forward message: " + ex.Message);
        }
    }

    /// <summary>
    /// Forward dialog üçün item record-u.
    /// Conversation və channel-ları eyni list-də göstərmək üçün.
    /// </summary>
    private record ForwardItem(Guid Id, Guid? OtherUserId, string Name, string? Subtitle, string? AvatarUrl, bool IsChannel, bool IsPrivate, bool IsNotes, DateTime? LastMessageAt);

    /// <summary>
    /// Forward dialog üçün filter olunmuş item-ları qaytarır.
    /// Axtarış sorğusuna görə filter edir.
    /// Son mesaj tarixinə görə sıralayır.
    /// PERFORMANCE: Optimized - Notes conversation filtered out (self-forward prevented)
    /// </summary>
    private List<ForwardItem> GetFilteredForwardItems()
    {
        var result = new List<ForwardItem>();
        var items = new List<ForwardItem>();

        // Notes conversation-u ayrı saxla
        var notesConv = directConversations.FirstOrDefault(c => c.IsNotes);
        ForwardItem? notesItem = null;
        if (notesConv != null)
        {
            notesItem = new ForwardItem(
                notesConv.Id,
                null,
                "Notes",
                "Visible to you only",
                null,
                IsChannel: false,
                IsPrivate: false,
                IsNotes: true,
                notesConv.LastMessageAtUtc);
        }

        // Direct Conversation-ları əlavə et (Notes-u çıxar)
        foreach (var conv in directConversations)
        {
            if (conv.IsNotes)
                continue;

            items.Add(new ForwardItem(
                conv.Id,
                conv.OtherUserId,
                conv.OtherUserFullName,
                "User",
                conv.OtherUserAvatarUrl,
                IsChannel: false,
                IsPrivate: false,
                IsNotes: false,
                conv.LastMessageAtUtc));
        }

        // Channel-ları əlavə et
        foreach (var channel in channelConversations)
        {
            items.Add(new ForwardItem(
                channel.Id,
                null,
                channel.Name,
                "Group chat",
                channel.AvatarUrl,
                IsChannel: true,
                IsPrivate: channel.Type == ChannelType.Private,
                IsNotes: false,
                channel.LastMessageAtUtc));
        }

        // Son mesaj tarixinə görə sırala (ən yenilər üstdə)
        items = items.OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue).ToList();

        // Axtarış filter-i
        if (!string.IsNullOrWhiteSpace(forwardSearchQuery))
        {
            items = items.Where(x => x.Name.Contains(forwardSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            // Notes da axtarışa uyğundursa əlavə et
            if (notesItem != null && notesItem.Name.Contains(forwardSearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(notesItem);
            }
        }
        else
        {
            // Axtarış yoxdursa, Notes həmişə yuxarıda
            if (notesItem != null)
            {
                result.Add(notesItem);
            }
        }

        result.AddRange(items);
        return result;
    }

    /// <summary>
    /// Forward dialog-da item click edildikdə.
    /// Channel-a və ya conversation-a forward edir.
    /// </summary>
    private async Task HandleForwardItemClick(ForwardItem item)
    {
        if (item.IsChannel)
        {
            await ForwardToChannel(item.Id);
        }
        else
        {
            await ForwardToConversation(item.Id);
        }
    }

    #endregion

    #region Reply - Mesaja cavab

    /// <summary>
    /// Reply state-ini qur.
    /// MessageBubble component-dən çağrılır.
    /// MessageInput component reply preview göstərir.
    /// </summary>
    private void HandleReply(Guid messageId)
    {
        // Reply ediləcək mesajı tap
        if (isDirectMessage)
        {
            var message = directMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                isReplying = true;
                replyToMessageId = messageId;
                replyToSenderName = message.SenderFullName;
                replyToContent = message.IsDeleted ? "This message was deleted" : message.Content;
                replyToFileId = message.IsDeleted ? null : message.FileId;
                replyToFileName = message.IsDeleted ? null : message.FileName;
                replyToFileContentType = message.IsDeleted ? null : message.FileContentType;
                StateHasChanged();
            }
        }
        else
        {
            var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                isReplying = true;
                replyToMessageId = messageId;
                replyToSenderName = message.SenderFullName;
                replyToContent = message.IsDeleted ? "This message was deleted" : message.Content;
                replyToFileId = message.IsDeleted ? null : message.FileId;
                replyToFileName = message.IsDeleted ? null : message.FileName;
                replyToFileContentType = message.IsDeleted ? null : message.FileContentType;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Reply state-ini ləğv et.
    /// MessageInput component-dən cancel düyməsi ilə.
    /// </summary>
    private void CancelReply()
    {
        isReplying = false;
        replyToMessageId = null;
        replyToSenderName = null;
        replyToContent = null;
        replyToFileId = null;
        replyToFileName = null;
        replyToFileContentType = null;
        StateHasChanged();
    }

    #endregion

    #region Forward Helpers

    /// <summary>
    /// Forward panel üçün tarix formatı.
    /// Screenshot-dakı "jan 23" formatını istifadə edir.
    /// </summary>
    private static string FormatForwardTime(DateTime dateTime)
    {
        var localDateTime = dateTime.ToLocalTime();
        return localDateTime.ToString("MMM d", CultureInfo.InvariantCulture).ToLower();
    }

    #endregion
}