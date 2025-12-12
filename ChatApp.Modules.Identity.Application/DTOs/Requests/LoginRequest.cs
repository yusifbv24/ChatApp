namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record LoginRequest(string Username, string Password, bool RememberMe);
}