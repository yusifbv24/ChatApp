namespace ChatApp.Modules.Search.Application.DTOs.Responses
{
    public record DirectMessageReadModel
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}