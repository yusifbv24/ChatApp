namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Channel details DTO with full member list
/// </summary>
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
