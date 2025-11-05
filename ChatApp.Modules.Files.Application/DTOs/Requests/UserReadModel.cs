namespace ChatApp.Modules.Files.Application.DTOs.Requests
{
    public record UserReadModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }
}