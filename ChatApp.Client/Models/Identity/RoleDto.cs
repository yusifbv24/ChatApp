namespace ChatApp.Client.Models.Identity
{
    public record RoleDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public List<PermissionDto> Permissions { get; init; } = new();
    }
}