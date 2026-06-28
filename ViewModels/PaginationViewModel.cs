namespace DLAttendance.ViewModels;

public class PaginationViewModel
{
    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public int StartRecord => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;

    public int EndRecord => Math.Min(Page * PageSize, TotalCount);

    public string Controller { get; init; } = string.Empty;

    public string Action { get; init; } = "Index";

    public Dictionary<string, string?> RouteValues { get; init; } = new();
}
