namespace ChatApp.Blazor.Client.Models.Messages
{
    public record DirectConversationDto(
        Guid Id,
        Guid OtherUserId,
        string OtherUserUsername,
        string OtherUserDisplayName,
        string? OtherUserAvatarUrl,
        string? LastMessageContent,
        DateTime LastMessageAtUtc,
        int UnreadCount,
        bool IsOtherUserOnline);
}