namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        bool RememberMe);
}