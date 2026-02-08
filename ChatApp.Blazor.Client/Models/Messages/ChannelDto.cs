namespace ChatApp.Blazor.Client.Models.Messages
{
    public record ChannelDto(
        Guid Id,
        string Name,
        string? Description,
        ChannelType Type,
        Guid CreatedBy,
        int MemberCount,
        bool IsArchived,
        DateTime CreatedAtUtc,
        DateTime? ArchivedAtUtc,
        string? AvatarUrl = null,
        string? LastMessageContent = null,
        DateTime? LastMessageAtUtc = null,
        int UnreadCount = 0,
        bool HasUnreadMentions = false,
        Guid? LastReadLaterMessageId = null,
        Guid? LastMessageId = null,
        Guid? LastMessageSenderId = null,
        string? LastMessageStatus = null,
        string? LastMessageSenderAvatarUrl = null,
        string? LastMessageSenderFullName = null,
        Guid? FirstUnreadMessageId = null,
        bool IsPinned = false,
        bool IsMuted = false,
        bool IsMarkedReadLater = false);



    public enum ChannelType
    {
        Public=1,
        Private=2
    }
}