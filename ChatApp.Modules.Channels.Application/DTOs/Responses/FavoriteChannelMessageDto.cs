namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    public record FavoriteChannelMessageDto(
        Guid Id,
        Guid ChannelId,
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
