namespace ChatApp.Modules.DirectMessages.Application.DTOs.Response
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
        bool HasUnreadMentions = false,
        Guid? LastReadLaterMessageId = null,
        Guid? LastMessageSenderId = null,
        string? LastMessageStatus = null, // Sent, Read
        Guid? LastMessageId = null,
        Guid? FirstUnreadMessageId = null,
        bool IsNotes = false, // Notes conversation (self-conversation)
        bool IsPinned = false,
        bool IsMuted = false,
        bool IsMarkedReadLater = false);
}