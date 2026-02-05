namespace ChatApp.Modules.Channels.Application.DTOs.Requests
{
    public record MentionRequest(Guid? UserId, string UserFullName, bool IsAllMention = false);
}
