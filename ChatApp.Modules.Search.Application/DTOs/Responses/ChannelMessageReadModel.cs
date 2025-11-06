namespace ChatApp.Modules.Search.Application.DTOs.Responses
{
    public record ChannelMessageReadModel
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}