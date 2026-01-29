namespace ChatApp.Modules.DirectMessages.Application.DTOs.Request
{
    public record UserReadModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }

        // Computed property (not mapped to database)
        public string FullName => $"{FirstName} {LastName}";
    }
}