namespace ChatApp.Client.Models.Identity
{
    public record UpdateRoleRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public List<Guid> PermissionIds { get; init; } = new();
    }
}