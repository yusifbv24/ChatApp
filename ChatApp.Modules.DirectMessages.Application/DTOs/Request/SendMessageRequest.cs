namespace ChatApp.Modules.DirectMessages.Application.DTOs.Request
{
    public record SendMessageRequest(
        string Content,
        string? FileId,
        Guid? ReplyToMessageId = null,
        bool IsForwarded = false,
        List<MentionRequest>? Mentions = null);
}