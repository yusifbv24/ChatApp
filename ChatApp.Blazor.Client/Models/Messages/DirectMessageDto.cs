using ChatApp.Blazor.Client.Models.Files;
using ChatApp.Shared.Kernel;

namespace ChatApp.Blazor.Client.Models.Messages
{
    public record DirectMessageDto(
        Guid Id,
        Guid ConversationId,
        Guid SenderId,
        string SenderEmail,
        string SenderFullName,
        string? SenderAvatarUrl,
        Guid ReceiverId,
        string Content,
        string? FileId,
        string? FileName,
        string? FileContentType,
        long? FileSizeInBytes,
        string? FileUrl,           // Statik fayl URL-i (API call əvəzinə)
        string? ThumbnailUrl,      // Şəkil thumbnail URL-i
        bool IsEdited,
        bool IsDeleted,
        bool IsRead,
        bool IsPinned,
        int ReactionCount,
        DateTime CreatedAtUtc,
        DateTime? EditedAtUtc,
        DateTime? PinnedAtUtc,
        Guid? ReplyToMessageId = null,
        string? ReplyToContent = null,
        string? ReplyToSenderName = null,
        string? ReplyToFileId = null,
        string? ReplyToFileName = null,
        string? ReplyToFileContentType = null,
        string? ReplyToFileUrl = null,        // Reply mesajının fayl URL-i
        string? ReplyToThumbnailUrl = null,   // Reply mesajının thumbnail URL-i
        bool IsForwarded = false,
        List<MessageReactionDto>? Reactions = null,
        List<MessageMentionDto>? Mentions = null,
        MessageStatus Status = MessageStatus.Sent,
        Guid? TempId = null,
        // File upload state (for optimistic UI)
        UploadState? FileUploadState = null,
        int FileUploadProgress = 0,
        CancellationTokenSource? FileUploadCts = null);

    public record MessageReactionDto(
        string Emoji,
        int Count,
        List<Guid> UserIds);

    public record MessageMentionDto(
        Guid UserId,
        string UserFullName);
}