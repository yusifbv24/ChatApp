namespace ChatApp.Blazor.Client.Models.Search
{
    public record SearchResultDto(
        Guid MessageId,
        SearchResultType ResultType,
        string Content,
        string HighlightedContent,
        Guid SenderId,
        string SenderUsername,
        string SenderDisplayName,
        string? SenderAvatarUrl,
        DateTime CreatedAtUtc,
        Guid? ChannelId,
        string? ChannelName,
        Guid? ConversationId,
        Guid? OtherUserId,
        string? OtherUsername,
        string? OtherDisplayName);

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