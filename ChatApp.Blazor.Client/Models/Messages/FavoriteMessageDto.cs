namespace ChatApp.Blazor.Client.Models.Messages
{
    public record FavoriteDirectMessageDto(
        Guid Id,
        Guid ConversationId,
        Guid SenderId,
        string SenderUsername,
        string SenderDisplayName,
        string? SenderAvatarUrl,
        string Content,
        bool IsDeleted,
        DateTime CreatedAtUtc,
        DateTime FavoritedAtUtc,
        string? FileId);

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
        DateTime FavoritedAtUtc,
        string? FileId);
}
