namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    // Read-only model for cross-module queries
    public class UserReadModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? AvatarUrl { get; set; }

        // Computed property (not mapped to database)
        public string FullName => $"{FirstName} {LastName}";
    }
}