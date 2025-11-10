namespace ChatApp.Client.Models.Channels
{
    /// <summary>
    /// Direct Message Data Transfer Object
    /// ===================================
    /// Represents a direct message between two users.
    /// </summary>
    public record DirectMessageDto
    {
        public Guid Id { get; init; }
        public Guid ConversationId { get; init; }
        public Guid SenderId { get; init; }
        public string SenderUsername { get; init; } = string.Empty;
        public string SenderDisplayName { get; init; } = string.Empty;
        public string? SenderAvatarUrl { get; init; }
        public string Content { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
        public DateTime? EditedAtUtc { get; init; }
        public bool IsEdited { get; init; }
        public bool IsDeleted { get; init; }
        public bool IsRead { get; init; }
        public DateTime? ReadAtUtc { get; init; }
        public List<Guid> AttachmentIds { get; init; } = new();
    }
}