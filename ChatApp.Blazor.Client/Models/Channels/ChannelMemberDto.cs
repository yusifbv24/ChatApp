namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Channel member DTO
/// </summary>
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
