namespace ChatApp.Modules.DirectMessages.Application.DTOs.Response
{
    public record DirectMessageDto(
        Guid Id,
        Guid ConversationId,
        Guid SenderId,
        string SenderUsername,
        string SenderDisplayName,
        string? SenderAvatarUrl,
        Guid ReceiverId,
        string Content,
        string? FileId,
        bool IsEdited,
        bool IsDeleted,
        bool IsRead,
        int ReactionCount,
        DateTime CreatedAtUtc,
        DateTime? EditedAtUtc,
        DateTime? ReadAtUtc,
        Guid? ReplyToMessageId,
        string? ReplyToContent,
        string? ReplyToSenderName,
        bool IsForwarded,
        List<DirectMessageReactionDto>? Reactions = null);

    public record DirectMessageReactionDto(
        string Emoji,
        int Count,
        List<Guid> UserIds);
}