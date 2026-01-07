namespace ChatApp.Blazor.Client.Models.Messages
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
        string? FileName,
        string? FileContentType,
        long? FileSizeInBytes,
        bool IsEdited,
        bool IsDeleted,
        bool IsPinned,
        int ReactionCount,
        DateTime CreatedAtUtc,
        DateTime? EditedAtUtc,
        DateTime? PinnedAtUtc,
        Guid? ReplyToMessageId = null,
        string? ReplyToContent = null,
        string? ReplyToSenderName = null,
        bool IsForwarded = false,
        int ReadByCount = 0,
        int TotalMemberCount = 0,
        List<Guid>? ReadBy = null,
        List<ChannelMessageReactionDto>? Reactions = null)
    {
        // Mutable properties for real-time updates
        public int ReadByCount { get; set; } = ReadByCount;
        public List<Guid>? ReadBy { get; set; } = ReadBy;
        public List<ChannelMessageReactionDto> Reactions { get; init; } = Reactions ?? [];
    }
}