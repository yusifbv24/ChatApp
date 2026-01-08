using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Forward - Mesaj yönləndirmə

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
        forwardSearchQuery = string.Empty;
        StateHasChanged();
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
            var content = forwardingDirectMessage?.Content ?? forwardingChannelMessage?.Content;
            if (string.IsNullOrEmpty(content)) return;

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
                        UserState.CurrentUser?.Username ?? "",
                        UserState.CurrentUser?.DisplayName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        conversation?.OtherUserId ?? Guid.Empty,
                        content,
                        null,                   // FileId
                        null,                   // FileName
                        null,                   // FileContentType
                        null,                   // FileSizeInBytes
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

                // Seçim rejimindən çıx
                if (isSelectingMessageBuble)
                {
                    ToggleSelectMode();
                }

                CancelForward();
                StateHasChanged();
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
            var content = forwardingDirectMessage?.Content ?? forwardingChannelMessage?.Content;
            if (string.IsNullOrEmpty(content)) return;

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
                        null,                                       // ReplyToMessageId
                        null,                                       // ReplyToContent
                        null,                                       // ReplyToSenderName
                        null,                                       // ReplyToFileId
                        null,                                       // ReplyToFileName
                        null,                                       // ReplyToFileContentType
                        true,                                       // IsForwarded
                        0,                                          // ReadByCount
                        totalMembers,                               // TotalMemberCount
                        new List<Guid>(),                           // ReadBy
                        new List<ChannelMessageReactionDto>());

                    if (!channelMessages.Any(m => m.Id == messageId))
                    {
                        channelMessages.Add(newMessage);
                    }

                    processedMessageIds.Add(messageId);
                }

                // Forward olunanda file attachment-lər transfer olunmur, ona görə preview sadəcə content-dir
                UpdateChannelLocally(channelId, content, messageTime, UserState.CurrentUser?.DisplayName);

                if (isSelectingMessageBuble)
                {
                    ToggleSelectMode();
                }

                CancelForward();
                StateHasChanged();
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
    private record ForwardItem(Guid Id, string Name, string? AvatarUrl, bool IsChannel, bool IsPrivate, DateTime? LastMessageAt);

    /// <summary>
    /// Forward dialog üçün filter olunmuş item-ları qaytarır.
    /// Axtarış sorğusuna görə filter edir.
    /// Son mesaj tarixinə görə sıralayır.
    /// </summary>
    private List<ForwardItem> GetFilteredForwardItems()
    {
        var items = new List<ForwardItem>();

        // Direct Conversation-ları əlavə et
        foreach (var conv in directConversations)
        {
            items.Add(new ForwardItem(
                conv.Id,
                conv.OtherUserDisplayName,
                conv.OtherUserAvatarUrl,
                IsChannel: false,
                IsPrivate: false,
                conv.LastMessageAtUtc));
        }

        // Channel-ları əlavə et
        foreach (var channel in channelConversations)
        {
            items.Add(new ForwardItem(
                channel.Id,
                channel.Name,
                null,
                IsChannel: true,
                IsPrivate: channel.Type == ChannelType.Private,
                channel.LastMessageAtUtc));
        }

        // Son mesaj tarixinə görə sırala (ən yenilər üstdə)
        items = items.OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue).ToList();

        // Axtarış filter-i
        if (!string.IsNullOrWhiteSpace(forwardSearchQuery))
        {
            items = items.Where(x => x.Name.Contains(forwardSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return items;
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
                replyToSenderName = message.SenderDisplayName;
                replyToContent = message.Content;
                replyToFileId = message.FileId;
                replyToFileName = message.FileName;
                replyToFileContentType = message.FileContentType;
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
                replyToSenderName = message.SenderDisplayName;
                replyToContent = message.Content;
                replyToFileId = message.FileId;
                replyToFileName = message.FileName;
                replyToFileContentType = message.FileContentType;
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
}