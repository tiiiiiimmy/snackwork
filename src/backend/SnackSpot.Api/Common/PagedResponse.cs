namespace SnackSpot.Api.Common;

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

    public static PagedResponse<T> Create(IReadOnlyList<T> items, int page, int pageSize, int total) =>
        new() { Items = items, Page = page, PageSize = pageSize, Total = total };
}
