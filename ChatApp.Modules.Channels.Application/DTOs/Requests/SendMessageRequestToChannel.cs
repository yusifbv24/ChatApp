namespace ChatApp.Modules.Channels.Application.DTOs.Requests
{
    public record SendMessageRequestToChannel(
        string Content,
        string? FileId,
        Guid? ReplyToMessageId = null,
        bool IsForwarded = false,
        List<MentionRequest>? Mentions = null);
}