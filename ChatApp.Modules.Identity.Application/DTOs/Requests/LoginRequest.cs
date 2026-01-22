namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record LoginRequest(string Email, string Password, bool RememberMe = false);
}