namespace ChatApp.Blazor.Client.Models.Messages
{
    public record ChannelMessageReactionDto(
        string Emoji,
        int Count,
        List<Guid> UserIds,
        List<string> UserFullNames,
        List<string?> UserAvatarUrls
    );
}