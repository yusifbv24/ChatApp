namespace ChatApp.Blazor.Client.Models.Messages;

/// <summary>
/// DTO representing a channel
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
    DateTime? ArchivedAtUtc);

/// <summary>
/// Channel type enumeration
/// </summary>
public enum ChannelType
{
    Public = 0,
    Private = 1
}
