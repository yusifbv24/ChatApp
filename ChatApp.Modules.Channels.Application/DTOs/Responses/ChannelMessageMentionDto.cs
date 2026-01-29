namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    public record ChannelMessageMentionDto(
        Guid? UserId, // Null for @All
        string UserFullName,
        bool IsAllMention);
}