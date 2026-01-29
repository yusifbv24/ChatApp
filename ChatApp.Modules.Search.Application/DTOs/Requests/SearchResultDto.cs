using ChatApp.Modules.Search.Domain.Enums;

namespace ChatApp.Modules.Search.Application.DTOs.Requests
{
    public record SearchResultDto(
        Guid MessageId,
        SearchResultType ResultType,
        string Content,
        string HighlightedContent, // Content with search term highlighted
        Guid SenderId,
        string SenderEmail,
        string SenderFullName,
        string? SenderAvatarUrl,
        DateTime CreatedAtUtc,

        // For channel messages
        Guid? ChannelId,
        string? ChannelName,

        // For direct messages
        Guid? ConversationId,
        Guid? OtherUserId,
        string? OtherEmail,
        string? OtherFullName
    );
}