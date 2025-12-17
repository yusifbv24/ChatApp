namespace ChatApp.Blazor.Client.Models.Messages
{
    public record ChannelMessageReactionDto(
        string Emoji,
        int Count,
        List<Guid> UserIds
    );
}
