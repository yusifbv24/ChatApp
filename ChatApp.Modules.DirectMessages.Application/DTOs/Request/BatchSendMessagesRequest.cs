namespace ChatApp.Modules.DirectMessages.Application.DTOs.Request;

/// <summary>
/// PERFORMANCE: Batch message sending - reduces N API calls to 1.
/// Used for multi-file uploads (send 5 files = 1 request instead of 5).
/// </summary>
public record BatchSendMessagesRequest
{
    /// <summary>
    /// List of messages to send in batch.
    /// Each message can have content and/or fileId.
    /// </summary>
    public List<BatchMessageItem> Messages { get; init; } = [];

    /// <summary>
    /// Optional reply to message ID (applies to first message only).
    /// </summary>
    public Guid? ReplyToMessageId { get; init; }

    /// <summary>
    /// Whether messages are forwarded (applies to all messages).
    /// </summary>
    public bool IsForwarded { get; init; }

    /// <summary>
    /// Optional mentions (applies to first message with content only).
    /// </summary>
    public List<MentionRequest> Mentions { get; init; } = [];
}

/// <summary>
/// Single message item in batch.
/// </summary>
public record BatchMessageItem
{
    /// <summary>
    /// Message content (can be empty for file-only messages).
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// File ID if message contains file attachment.
    /// </summary>
    public string? FileId { get; init; }
}