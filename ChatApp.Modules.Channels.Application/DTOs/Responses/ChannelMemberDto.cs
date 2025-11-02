using ChatApp.Modules.Channels.Domain.Enums;

namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    public record ChannelMemberDto(
        Guid Id,
        Guid ChannelId,
        Guid UserId,
        string Username,
        string DisplayName,
        MemberRole Role,
        DateTime JoinedAtUtc,
        bool IsActive
    );
}