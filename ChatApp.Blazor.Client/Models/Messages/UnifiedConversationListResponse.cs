namespace ChatApp.Blazor.Client.Models.Messages;

/// <summary>
/// Unified conversation list response from backend.
/// Contains DM conversations, channels, and department users in a single paginated list.
/// </summary>
public record UnifiedConversationListResponse(
    List<UnifiedChatItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalConversations,
    int TotalChannels,
    int TotalDepartmentUsers,
    bool HasNextPage
);

public record UnifiedChatItemDto(
    Guid Id,
    UnifiedChatItemType Type,
    string Name,
    string? AvatarUrl,
    string? LastMessage,
    DateTime? LastMessageAtUtc,
    int UnreadCount,
    bool HasUnreadMentions,
    bool IsPinned,
    bool IsMuted,
    bool IsMarkedReadLater,
    bool IsNotes,
    Guid? LastReadLaterMessageId,
    Guid? FirstUnreadMessageId,
    Guid? LastMessageSenderId,
    string? LastMessageStatus,
    Guid? LastMessageId,
    Guid? OtherUserId,
    string? OtherUserEmail,
    int? MemberCount,
    string? ChannelType,
    string? LastMessageSenderAvatarUrl,
    Guid? CreatedBy,
    string? ChannelDescription,
    DateTime? CreatedAtUtc,
    string? Email,
    string? PositionName,
    string? DepartmentName
);

public enum UnifiedChatItemType
{
    Conversation,
    Channel,
    DepartmentUser
}
