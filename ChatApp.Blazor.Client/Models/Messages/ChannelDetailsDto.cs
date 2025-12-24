namespace ChatApp.Blazor.Client.Models.Messages
{
    /// <summary>
    /// DTO representing detailed channel information including members
    /// </summary>
    public record ChannelDetailsDto(
        Guid Id,
        string Name,
        string? Description,
        ChannelType Type,
        Guid CreatedBy,
        string CreatedByUsername,
        bool IsArchived,
        int MemberCount,
        List<ChannelMemberDto> Members,
        DateTime CreatedAtUtc);



    /// <summary>
    /// DTO representing a channel member
    /// </summary>
    public record ChannelMemberDto(
        Guid UserId,
        string Username,
        string DisplayName,
        string? AvatarUrl,
        ChannelMemberRole Role,
        DateTime JoinedAtUtc,
        bool IsActive,
        Guid? LastReadLaterMessageId);


    public enum ChannelMemberRole
    {
        Member=1,
        Admin=2,
        Owner=3
    }
}