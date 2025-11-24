using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Auth.Services;

/// <summary>
/// Interface for authentication operations
/// </summary>
public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result<LoginResponse>> RefreshTokenAsync();
    Task<Result> LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
}