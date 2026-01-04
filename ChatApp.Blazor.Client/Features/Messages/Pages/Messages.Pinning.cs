namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Load Pinned Messages - Pinlənmiş mesajları yüklə

    /// <summary>
    /// Channel-in pinlənmiş mesajlarını yükləyir.
    /// SelectChannel zamanı çağrılır.
    /// </summary>
    private async Task LoadPinnedMessageCount()
    {
        try
        {
            var result = await ChannelService.GetPinnedMessagesAsync(selectedChannelId!.Value);
            if (result.IsSuccess && result.Value != null)
            {
                pinnedChannelMessages = result.Value;
                pinnedChannelMessageCount = result.Value.Count;
            }
        }
        catch
        {
            pinnedChannelMessages = [];
            pinnedChannelMessageCount = 0;
        }
    }

    /// <summary>
    /// Conversation-un pinlənmiş mesajlarını yükləyir.
    /// </summary>
    private async Task LoadPinnedDirectMessageCount()
    {
        try
        {
            var result = await ConversationService.GetPinnedMessagesAsync(selectedConversationId!.Value);
            if (result.IsSuccess && result.Value != null)
            {
                pinnedDirectMessages = result.Value;
                pinnedDirectMessageCount = result.Value.Count;
            }
        }
        catch
        {
            pinnedDirectMessages = [];
            pinnedDirectMessageCount = 0;
        }
    }

    /// <summary>
    /// Pinlənmiş mesaja naviqasiya et.
    /// Mesaj yüklənməyibsə GetMessagesAround ilə yüklənir.
    /// </summary>
    private async Task NavigateToPinnedMessage(Guid messageId)
    {
        await NavigateToMessageAsync(messageId);
    }

    #endregion

    #region Pin Direct Message - DM mesajını pinlə

    /// <summary>
    /// DM mesajını pinlə.
    /// MessageBubble component-dən çağrılır.
    /// </summary>
    private async Task HandlePinDirectMessage(Guid messageId)
    {
        if (!selectedConversationId.HasValue) return;

        try
        {
            var result = await ConversationService.PinMessageAsync(selectedConversationId.Value, messageId);
            if (result.IsSuccess)
            {
                // Local mesaj state-ini yenilə
                var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with { IsPinned = true, PinnedAtUtc = DateTime.UtcNow };
                    InvalidateMessageCache();
                }

                // Pinned count-u yenilə
                await LoadPinnedDirectMessageCount();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to pin message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to pin message: " + ex.Message);
        }
    }

    /// <summary>
    /// DM mesajının pinini sil.
    /// </summary>
    private async Task HandleUnpinDirectMessage(Guid messageId)
    {
        if (!selectedConversationId.HasValue) return;

        try
        {
            var result = await ConversationService.UnpinMessageAsync(selectedConversationId.Value, messageId);
            if (result.IsSuccess)
            {
                // Local mesaj state-ini yenilə
                var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with { IsPinned = false, PinnedAtUtc = null };
                    InvalidateMessageCache();
                }

                // Pinned count-u yenilə
                await LoadPinnedDirectMessageCount();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to unpin message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to unpin message: " + ex.Message);
        }
    }

    #endregion

    #region Pin Channel Message - Channel mesajını pinlə

    /// <summary>
    /// Channel mesajını pinlə.
    /// MessageBubble component-dən çağrılır.
    /// </summary>
    private async Task HandlePinChannelMessage(Guid messageId)
    {
        if (!selectedChannelId.HasValue) return;

        try
        {
            var result = await ChannelService.PinMessageAsync(selectedChannelId.Value, messageId);
            if (result.IsSuccess)
            {
                // Local mesaj state-ini yenilə
                var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    channelMessages[index] = message with { IsPinned = true, PinnedAtUtc = DateTime.UtcNow };
                    InvalidateMessageCache();
                }

                // Pinned count və list-i yenilə
                await LoadPinnedMessageCount();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to pin message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to pin message: " + ex.Message);
        }
    }

    /// <summary>
    /// Channel mesajının pinini sil.
    /// </summary>
    private async Task HandleUnpinChannelMessage(Guid messageId)
    {
        if (!selectedChannelId.HasValue) return;

        try
        {
            var result = await ChannelService.UnPinMessageAsync(selectedChannelId.Value, messageId);
            if (result.IsSuccess)
            {
                // Local mesaj state-ini yenilə
                var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    channelMessages[index] = message with { IsPinned = false, PinnedAtUtc = null };
                    InvalidateMessageCache();
                }

                // Pinned count və list-i yenilə
                await LoadPinnedMessageCount();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to unpin message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to unpin message: " + ex.Message);
        }
    }

    #endregion
}