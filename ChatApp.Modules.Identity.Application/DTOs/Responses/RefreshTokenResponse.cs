namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
    public record RefreshTokenResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn);
}