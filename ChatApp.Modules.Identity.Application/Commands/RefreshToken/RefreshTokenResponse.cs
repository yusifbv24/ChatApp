namespace ChatApp.Modules.Identity.Application.Commands.RefreshToken
{
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; }=string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}