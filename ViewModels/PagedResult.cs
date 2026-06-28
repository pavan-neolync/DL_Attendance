namespace DLAttendance.ViewModels;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = Paging.DefaultPageSize;

    public int TotalCount { get; init; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public int StartRecord => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;

    public int EndRecord => Math.Min(Page * PageSize, TotalCount);
}
