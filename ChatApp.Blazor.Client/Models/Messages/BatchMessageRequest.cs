namespace ChatApp.Blazor.Client.Models.Messages;

/// <summary>
/// Batch message sending request for multi-file uploads.
/// </summary>
public record BatchSendMessagesRequest
{
    public List<BatchMessageItem> Messages { get; init; } = [];
    public Guid? ReplyToMessageId { get; init; }
    public bool IsForwarded { get; init; }
    public List<MentionRequest> Mentions { get; init; } = [];
}

public record BatchMessageItem
{
    public string Content { get; init; } = string.Empty;
    public string? FileId { get; init; }
}