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
        int UnreadCount = 0);



    public enum ChannelType
    {
        Public=0,
        Private=1
    }
}