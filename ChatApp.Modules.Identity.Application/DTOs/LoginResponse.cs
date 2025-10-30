namespace ChatApp.Modules.Identity.Application.DTOs
{
    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn);
}