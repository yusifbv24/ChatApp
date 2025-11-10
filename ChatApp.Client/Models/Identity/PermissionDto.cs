namespace ChatApp.Client.Models.Identity
{
    public record PermissionDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string Module { get; init; } = string.Empty;
    }
}