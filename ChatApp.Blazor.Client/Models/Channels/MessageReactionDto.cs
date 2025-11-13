namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Message reaction DTO
/// </summary>
public record MessageReactionDto(
    Guid UserId,
    string Username,
    string Reaction,
    DateTime CreatedAtUtc
);
