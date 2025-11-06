namespace ChatApp.Modules.Search.Application.DTOs.Responses
{
    public record ChannelReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }
}