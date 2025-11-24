namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Response model from creating a user
/// </summary>
public record CreateUserResponse(
    Guid UserId,
    string? Message);