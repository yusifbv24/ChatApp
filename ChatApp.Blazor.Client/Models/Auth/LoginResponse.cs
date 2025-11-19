namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Response model for successful login
/// </summary>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn
);