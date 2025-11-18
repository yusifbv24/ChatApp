namespace ChatApp.Blazor.Client.Models.DirectMessages;

/// <summary>
/// Message reaction DTO
/// </summary>
public record DirectMessageReactionDto(
    Guid MessageId,
    Guid UserId,
    string Username,
    string Reaction
);
