namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    // Read-only model for cross-module queries
    public class UserReadModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }
}