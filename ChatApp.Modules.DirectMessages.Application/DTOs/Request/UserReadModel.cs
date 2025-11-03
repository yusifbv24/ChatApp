namespace ChatApp.Modules.DirectMessages.Application.DTOs.Request
{
    public record UserReadModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
    }
}