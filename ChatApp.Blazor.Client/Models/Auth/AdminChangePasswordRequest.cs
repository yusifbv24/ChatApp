using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Auth
{
    public record AdminChangePasswordRequest
    {
        [Required(ErrorMessage ="New password is required")]
        [StringLength(100, MinimumLength =8,ErrorMessage ="Password must be at least 8 characters")]
        public string NewPassword { get; set; } = string.Empty;



        [Required(ErrorMessage ="Confirm password is required")]
        [Compare(nameof(NewPassword),ErrorMessage ="Passwords do not match")]
        public string ConfirmPassword { get; set; }=string.Empty;
    }
}