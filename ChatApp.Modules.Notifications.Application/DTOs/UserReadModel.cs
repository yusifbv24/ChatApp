namespace ChatApp.Modules.Notifications.Application.DTOs
{
    public record UserReadModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}