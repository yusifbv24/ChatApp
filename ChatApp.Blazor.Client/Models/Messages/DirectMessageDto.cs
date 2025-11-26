namespace ChatApp.Blazor.Client.Models.Messages;

/// <summary>
/// DTO representing a direct message
/// </summary>
public record DirectMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderUsername,
    string SenderDisplayName,
    string? SenderAvatarUrl,
    Guid ReceiverId,
    string Content,
    string? FileId,
    bool IsEdited,
    bool IsDeleted,
    bool IsRead,
    int ReactionCount,
    DateTime CreatedAtUtc,
    DateTime? EditedAtUtc,
    DateTime? ReadAtUtc);
