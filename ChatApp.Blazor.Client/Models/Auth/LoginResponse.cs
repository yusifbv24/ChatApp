namespace ChatApp.Blazor.Client.Models.Auth
{
    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn
    );
}