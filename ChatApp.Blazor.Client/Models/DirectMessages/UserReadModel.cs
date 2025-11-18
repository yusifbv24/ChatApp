namespace ChatApp.Blazor.Client.Models.DirectMessages;

/// <summary>
/// User information for direct messages
/// </summary>
public record UserReadModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
