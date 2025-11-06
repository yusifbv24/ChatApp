namespace ChatApp.Modules.Search.Application.DTOs.Requests
{
    public record SearchResultsDto(
        List<SearchResultDto> Results,
        int TotalCount,
        int PageNumber,
        int PageSize,
        bool HasNextPage);
}