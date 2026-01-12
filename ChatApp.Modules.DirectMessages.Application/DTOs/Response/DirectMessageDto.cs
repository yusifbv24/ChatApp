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
        string? FileName,
        string? FileContentType,
        long? FileSizeInBytes,
        bool IsEdited,
        bool IsDeleted,
        bool IsRead,
        bool IsPinned,
        int ReactionCount,
        DateTime CreatedAtUtc,
        DateTime? EditedAtUtc,
        DateTime? PinnedAtUtc,
        Guid? ReplyToMessageId,
        string? ReplyToContent,
        string? ReplyToSenderName,
        string? ReplyToFileId,
        string? ReplyToFileName,
        string? ReplyToFileContentType,
        bool IsForwarded,
        List<DirectMessageReactionDto>? Reactions = null,
        List<MessageMentionDto>? Mentions = null);

    public record DirectMessageReactionDto(
        string Emoji,
        int Count,
        List<Guid> UserIds);

    public record MessageMentionDto(
        Guid UserId,
        string UserName);
}