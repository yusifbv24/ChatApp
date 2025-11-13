using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Request model for admin changing user password
/// </summary>
public class AdminChangePasswordRequest
{
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
