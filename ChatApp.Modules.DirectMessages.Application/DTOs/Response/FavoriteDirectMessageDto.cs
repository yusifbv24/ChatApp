namespace ChatApp.Modules.DirectMessages.Application.DTOs.Response
{
    public record FavoriteDirectMessageDto(
        Guid Id,
        Guid ConversationId,
        Guid SenderId,
        string SenderEmail,
        string SenderFullName,
        string? SenderAvatarUrl,
        string Content,
        bool IsDeleted,
        DateTime CreatedAtUtc,
        DateTime FavoritedAtUtc,
        string? FileId);
}
