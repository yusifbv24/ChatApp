using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Request model for creating a new role
/// </summary>
public class CreateRoleRequest
{
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
