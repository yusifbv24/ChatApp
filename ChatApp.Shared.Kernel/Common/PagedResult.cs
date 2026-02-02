namespace ChatApp.Shared.Kernel.Common;

/// <summary>
/// Paginated result wrapper for any entity type.
/// Used across backend API responses.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public static PagedResult<T> Create(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}