using ChatApp.Modules.Channels.Domain.Enums;

namespace ChatApp.Modules.Channels.Application.DTOs.Responses
{
    public record ChannelDetailsDto(
        Guid Id,
        string Name,
        string? Description,
        ChannelType Type,
        Guid CreatedBy,
        string CreatedByUsername,
        bool IsArchived,
        int MemberCount,
        List<ChannelMemberDto> Members,
        DateTime CreatedAtUtc
    );
}