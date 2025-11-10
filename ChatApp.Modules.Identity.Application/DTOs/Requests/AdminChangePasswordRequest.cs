namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record AdminChangePasswordRequest
    {
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmNewPassword { get; init; } = string.Empty;
    }
}