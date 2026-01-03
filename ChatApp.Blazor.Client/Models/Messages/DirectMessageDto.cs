namespace ChatApp.Blazor.Client.Models.Messages
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
        bool IsPinned,
        int ReactionCount,
        DateTime CreatedAtUtc,
        DateTime? EditedAtUtc,
        DateTime? PinnedAtUtc,
        Guid? ReplyToMessageId = null,
        string? ReplyToContent = null,
        string? ReplyToSenderName = null,
        bool IsForwarded = false,
        List<MessageReactionDto>? Reactions = null);

    public record MessageReactionDto(
        string Emoji,
        int Count,
        List<Guid> UserIds);
}