namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Channel message DTO
/// </summary>
public record ChannelMessageDto(
    Guid Id,
    Guid ChannelId,
    Guid SenderId,
    string SenderUsername,
    string SenderDisplayName,
    string? SenderAvatarUrl,
    string Content,
    string? FileId,
    bool IsEdited,
    bool IsDeleted,
    bool IsPinned,
    int ReactionCount,
    DateTime CreatedAtUtc,
    DateTime? EditedAtUtc,
    DateTime? PinnedAtUtc
);
