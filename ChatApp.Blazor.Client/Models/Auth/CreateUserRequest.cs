using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Request model for creating a new user
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 100 characters")]
    public string DisplayName { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; }

    public bool IsAdmin { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Notes { get; set; }
}
