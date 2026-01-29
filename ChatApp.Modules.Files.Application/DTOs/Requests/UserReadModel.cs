namespace ChatApp.Modules.Files.Application.DTOs.Requests
{
    public record UserReadModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;

        // Computed property (not mapped to database)
        public string FullName => $"{FirstName} {LastName}";
    }
}