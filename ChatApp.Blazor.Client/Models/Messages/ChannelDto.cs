namespace ChatApp.Blazor.Client.Models.Messages
{
    public record ChannelDto(
        Guid Id,
        string Name,
        string? Description,
        ChannelType Type,
        Guid CreatedBy,
        int MemberCount,
        bool IsArchived,
        DateTime CreatedAtUtc,
        DateTime? ArchivedAtUtc,
        string? LastMessageContent = null,
        string? LastMessageSenderName = null,
        DateTime? LastMessageAtUtc = null,
        int UnreadCount = 0);



    public enum ChannelType
    {
        Public=1,
        Private=2
    }
}