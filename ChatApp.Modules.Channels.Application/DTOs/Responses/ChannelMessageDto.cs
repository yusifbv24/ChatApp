namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    public record ChannelMessageDto(
        Guid Id,
        Guid ChannelId,
        Guid SenderId,
        string SenderUsername,
        string SenderDisplayName,
        string? SenderAvatarUrl,
        string Content,
        string? FileId,
        bool IsEdited,
        bool IsDeleted,
        bool IsPinned,
        int ReactionCount,
        DateTime CreatedAtUtc,
        DateTime? EditedAtUtc,
        DateTime? PinnedAtUtc,
        Guid? ReplyToMessageId,
        string? ReplyToContent,
        string? ReplyToSenderName,
        bool IsForwarded,
        int ReadByCount = 0,
        int TotalMemberCount = 0,
        List<Guid>? ReadBy = null
    );
}