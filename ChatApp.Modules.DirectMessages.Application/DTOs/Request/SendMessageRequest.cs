namespace ChatApp.Modules.DirectMessages.Application.DTOs.Request
{
    public record SendMessageRequest(string Content, string? FileId);
}