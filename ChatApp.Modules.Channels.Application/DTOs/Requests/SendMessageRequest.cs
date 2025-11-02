namespace ChatApp.Modules.Channels.Application.DTOs.Requests
{
    public record SendMessageRequest(string Content, string? FileId);
}