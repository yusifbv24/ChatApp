namespace ChatApp.Modules.Search.Application.DTOs.Responses
{
    public record UserReadModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? AvatarUrl { get; set; }

        // Computed property (not mapped to database)
        public string FullName => $"{FirstName} {LastName}";
    }
}