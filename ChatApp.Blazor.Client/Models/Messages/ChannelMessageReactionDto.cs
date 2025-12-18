namespace ChatApp.Blazor.Client.Models.Messages
{
    public record ChannelMessageReactionDto(
        string Emoji,
        int Count,
        List<Guid> UserIds,
        List<string> UserDisplayNames,
        List<string?> UserAvatarUrls
    );
}
