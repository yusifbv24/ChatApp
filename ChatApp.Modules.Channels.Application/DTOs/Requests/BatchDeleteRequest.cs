namespace ChatApp.Modules.Channels.Application.DTOs.Requests;

public record BatchDeleteRequest
{
    public List<Guid> MessageIds { get; init; } = [];
}
