namespace ChatApp.Modules.Channels.Application.DTOs.Requests
{
    public record SendMessageRequestToChannel(string Content, string? FileId);
}