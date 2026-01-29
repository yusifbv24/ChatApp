namespace ChatApp.Blazor.Client.Models.Search
{
    public record SearchResultDto(
        Guid MessageId,
        SearchResultType ResultType,
        string Content,
        string HighlightedContent,
        Guid SenderId,
        string SenderEmail,
        string SenderFullName,
        string? SenderAvatarUrl,
        DateTime CreatedAtUtc,
        Guid? ChannelId,
        string? ChannelName,
        Guid? ConversationId,
        Guid? OtherUserId,
        string? OtherEmail,
        string? OtherFullName);

    public record SearchResultsDto(
        List<SearchResultDto> Results,
        int TotalCount,
        int PageNumber,
        int PageSize,
        bool HasNextPage);

    public enum SearchResultType
    {
        ChannelMessage = 1,
        DirectMessage = 2
    }
}