namespace ChatApp.Client.Models.Identity
{
    public record CreateRoleRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public List<Guid> PermissionIds { get; init; } = new();
    }
}