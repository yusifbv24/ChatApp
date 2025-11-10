namespace ChatApp.Client.Models.Identity
{
    public record UpdateUserRequest
    {
        public string? Email { get; init; }
        public string? DisplayName { get; init; }
        public string? AvatarUrl { get; init; }
        public string? Notes { get; init; }
    }
}