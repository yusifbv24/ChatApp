using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Request model for updating user information
/// </summary>
public class UpdateUserRequest
{
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? Email { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 100 characters")]
    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Notes { get; set; }
}
