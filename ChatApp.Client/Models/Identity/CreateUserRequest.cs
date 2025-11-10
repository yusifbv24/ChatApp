namespace ChatApp.Client.Models.Identity
{
    public record CreateUserRequest
    {
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
        public string? Notes { get; init; }
        public bool IsAdmin { get; init; }
    }
}