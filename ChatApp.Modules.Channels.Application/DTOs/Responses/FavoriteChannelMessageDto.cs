namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    public record FavoriteChannelMessageDto(
        Guid Id,
        Guid ChannelId,
        Guid SenderId,
        string SenderUsername,
        string SenderDisplayName,
        string? SenderAvatarUrl,
        string Content,
        bool IsDeleted,
        DateTime CreatedAtUtc,
        DateTime FavoritedAtUtc);
}
