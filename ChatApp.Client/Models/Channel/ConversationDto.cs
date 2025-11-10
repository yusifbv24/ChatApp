namespace ChatApp.Client.Models.Channels
{
    public record ConversationDto
    {
        public Guid Id { get; init; }
        public Guid OtherUserId { get; init; }
        public string OtherUserUsername { get; init; } = string.Empty;
        public string OtherUserDisplayName { get; init; } = string.Empty;
        public string? OtherUserAvatarUrl { get; init; }
        public bool IsOnline { get; init; }
        public DateTime? LastMessageAtUtc { get; init; }
        public int UnreadCount { get; init; }
    }
}