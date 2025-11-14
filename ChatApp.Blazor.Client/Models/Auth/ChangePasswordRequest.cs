using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Auth
{
    public record ChangePasswordRequest
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage ="Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;


        [Required(ErrorMessage ="New password is required")]
        [StringLength(100,MinimumLength =6,ErrorMessage ="Password must be at least 6 characters")]
        public string NewPassword { get; set; }=string.Empty;


        [Required(ErrorMessage ="Confirm password is required")]
        [Compare(nameof(NewPassword),ErrorMessage ="Password do not match")]
        public string ConfirmPassword {  get; set; }=string.Empty;
    }
}