namespace ChatApp.Shared.Kernel.Common;

/// <summary>
/// Unified conversation list response combining DMs, Channels, and Department users.
/// Backend orchestrates all 3 sources and returns exactly pageSize items.
/// Priority: Notes → Pinned → Active (by LastMessageAtUtc) → Department users (fill remaining slots).
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

/// <summary>
/// Unified item representing a DM conversation, Channel, or Department user.
/// Type field determines which properties are populated.
/// </summary>
public record UnifiedChatItemDto(
    Guid Id,
    UnifiedChatItemType Type,

    // Common fields
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

    // DM-specific
    Guid? OtherUserId,
    string? OtherUserEmail,
    string? OtherUserPosition,
    string? OtherUserRole,
    DateTime? OtherUserLastSeenAtUtc,

    // Channel-specific
    int? MemberCount,
    string? ChannelType,
    string? LastMessageSenderAvatarUrl,
    string? LastMessageSenderFullName,
    Guid? CreatedBy,
    string? ChannelDescription,
    DateTime? CreatedAtUtc,

    // DepartmentUser-specific
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