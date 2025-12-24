using ChatApp.Modules.Channels.Domain.Enums;

namespace ChatApp.Modules.Channels.Application.DTOs.Responses
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
        string? LastMessageContent = null,
        string? LastMessageSenderName = null,
        DateTime? LastMessageAtUtc = null,
        int UnreadCount = 0,
        DateTime? CurrentMemberLastReadAtUtc = null,
        Guid? LastReadLaterMessageId = null
    );
}