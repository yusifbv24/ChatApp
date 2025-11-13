namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Channel summary DTO
/// </summary>
public record ChannelDto(
    Guid Id,
    string Name,
    string? Description,
    ChannelType Type,
    Guid CreatedBy,
    int MemberCount,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime? ArchivedAtUtc
);
