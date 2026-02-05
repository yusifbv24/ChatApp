namespace ChatApp.Modules.DirectMessages.Application.DTOs.Request
{
    public record MentionRequest(Guid UserId, string UserFullName);
}