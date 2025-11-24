using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Request model for admin changing user password
/// </summary>
public class AdminChangePasswordRequest
{
    // Property name must match backend: "Id" not "UserId"
    public Guid Id { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "New password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    // Property name must match backend: "ConfirmNewPassword" not "ConfirmPassword"
    public string ConfirmNewPassword { get; set; } = string.Empty;
}