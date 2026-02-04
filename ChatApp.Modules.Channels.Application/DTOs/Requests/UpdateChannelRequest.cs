using ChatApp.Modules.Channels.Domain.Enums;

namespace ChatApp.Modules.Channels.Application.DTOs.Requests
{
    public record UpdateChannelRequest(
        string? Name,
        string? Description,
        ChannelType? Type,
        string? AvatarUrl = null
    );
}