namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    public record ChannelMessageReactionDto(
        string Emoji,
        int Count,
        List<Guid> UserIds,
        List<string> UserFullNames,
        List<string?> UserAvatarUrls
    );
}
