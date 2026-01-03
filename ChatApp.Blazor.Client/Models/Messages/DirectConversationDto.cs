namespace ChatApp.Blazor.Client.Models.Messages
{
    public record DirectConversationDto(
        Guid Id,
        Guid OtherUserId,
        string OtherUserUsername,
        string OtherUserDisplayName,
        string? OtherUserAvatarUrl,
        string? LastMessageContent,
        DateTime LastMessageAtUtc,
        int UnreadCount,
        Guid? LastReadLaterMessageId = null,
        Guid? LastMessageSenderId = null,
        string? LastMessageStatus = null,
        Guid? LastMessageId = null);
}