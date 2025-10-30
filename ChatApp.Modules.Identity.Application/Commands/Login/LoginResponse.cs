namespace ChatApp.Modules.Identity.Application.Commands.Login
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }=string.Empty;
        public string RefreshToken {  get; set; }=string.Empty;
        public int ExpiresIn { get; set;  }
    }
}