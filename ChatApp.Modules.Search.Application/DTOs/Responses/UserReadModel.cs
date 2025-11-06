namespace ChatApp.Modules.Search.Application.DTOs.Responses
{
    public record UserReadModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }
}