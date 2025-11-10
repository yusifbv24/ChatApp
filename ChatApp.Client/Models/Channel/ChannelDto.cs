namespace ChatApp.Client.Models.Channels
{
    public record ChannelDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public Guid CreatedBy { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public bool IsPrivate { get; init; }
        public int MemberCount { get; init; }
        public DateTime? LastMessageAtUtc { get; init; }
    }
}