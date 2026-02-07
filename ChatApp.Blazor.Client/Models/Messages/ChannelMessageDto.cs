using ChatApp.Blazor.Client.Models.Files;
using ChatApp.Shared.Kernel;

namespace ChatApp.Blazor.Client.Models.Messages
{
    public record ChannelMessageDto(
        Guid Id,
        Guid ChannelId,
        Guid SenderId,
        string SenderEmail,
        string SenderFullName,
        string? SenderAvatarUrl,
        string Content,
        string? FileId,
        string? FileName,
        string? FileContentType,
        long? FileSizeInBytes,
        string? FileUrl,           // Statik fayl URL-i (API call əvəzinə)
        string? ThumbnailUrl,      // Şəkil thumbnail URL-i
        bool IsEdited,
        bool IsDeleted,
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
        int ReadByCount = 0,
        int TotalMemberCount = 0,
        List<Guid>? ReadBy = null,
        List<ChannelMessageReactionDto>? Reactions = null,
        List<ChannelMessageMentionDto>? Mentions = null,
        MessageStatus Status = MessageStatus.Sent,
        Guid? TempId = null,
        // File upload state (for optimistic UI)
        UploadState? FileUploadState = null,
        int FileUploadProgress = 0,
        CancellationTokenSource? FileUploadCts = null)
    {
        // Mutable properties for real-time updates
        public int ReadByCount { get; set; } = ReadByCount;
        public List<Guid>? ReadBy { get; set; } = ReadBy;
        public MessageStatus Status { get; set; } = Status;
        public List<ChannelMessageReactionDto> Reactions { get; init; } = Reactions ?? [];
        public List<ChannelMessageMentionDto> Mentions { get; init; } = Mentions ?? [];
        // Mutable upload state
        public UploadState? FileUploadState { get; set; } = FileUploadState;
        public int FileUploadProgress { get; set; } = FileUploadProgress;
        public CancellationTokenSource? FileUploadCts { get; set; } = FileUploadCts;
    }

    public record ChannelMessageMentionDto(
        Guid? UserId, // Null for @All
        string UserFullName,
        bool IsAllMention);
}