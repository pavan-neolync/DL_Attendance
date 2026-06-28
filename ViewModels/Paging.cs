namespace DLAttendance.ViewModels;

public static class Paging
{
    public static readonly int[] AllowedPageSizes = [10, 25, 50, 100];

    public const int DefaultPageSize = 25;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = AllowedPageSizes.Contains(pageSize) ? pageSize : DefaultPageSize;
        return (page, pageSize);
    }
}
