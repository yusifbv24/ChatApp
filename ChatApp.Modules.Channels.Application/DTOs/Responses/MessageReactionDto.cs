namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    public record MessageReactionDto(
        Guid UserId,
        string Username,
        string Reaction,
        DateTime CreatedAtUtc
    );
}