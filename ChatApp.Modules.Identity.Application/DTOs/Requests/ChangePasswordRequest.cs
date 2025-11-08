namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record ChangePasswordRequest(
        Guid UserId,
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword
    );
}