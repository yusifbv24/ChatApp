namespace ChatApp.Modules.DirectMessages.Application.DTOs.Request;

public record BatchDeleteRequest
{
    public List<Guid> MessageIds { get; init; } = [];
}
