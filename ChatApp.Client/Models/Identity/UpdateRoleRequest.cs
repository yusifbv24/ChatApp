namespace ChatApp.Client.Models.Identity
{
    public record UpdateRoleRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}