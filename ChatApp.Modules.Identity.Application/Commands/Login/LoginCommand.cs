namespace ChatApp.Modules.Identity.Application.Commands.Login
{
    public record LoginCommand
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; }= string.Empty;
    }
}