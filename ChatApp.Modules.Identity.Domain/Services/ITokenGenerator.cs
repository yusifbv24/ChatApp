using ChatApp.Modules.Identity.Domain.Entities;

namespace ChatApp.Modules.Identity.Domain.Services
{
    public interface ITokenGenerator
    {
        string GenerateAccessToken(User user, List<string?> permissions);
        string GenerateRefreshToken();
    }
}