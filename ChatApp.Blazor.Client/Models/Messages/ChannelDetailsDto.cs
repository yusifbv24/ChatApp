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
        string CreatedByEmail,
        bool IsArchived,
        int MemberCount,
        List<ChannelMemberDto> Members,
        DateTime CreatedAtUtc);



    /// <summary>
    /// DTO representing a channel member
    /// </summary>
    public record ChannelMemberDto(
        Guid Id,
        Guid ChannelId,
        Guid UserId,
        string Email,
        string FullName,
        string? AvatarUrl,
        MemberRole Role,
        DateTime JoinedAtUtc,
        bool IsActive,
        Guid? LastReadLaterMessageId);


    public enum MemberRole
    {
        Member=1,
        Admin=2,
        Owner=3
    }
}