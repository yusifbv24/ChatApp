namespace ChatApp.Modules.Search.Application.DTOs.Responses
{
    public record ConversationReadModel
    {
        public Guid Id { get; set; }
        public Guid User1Id { get; set; }
        public Guid User2Id { get; set; }
    }
}