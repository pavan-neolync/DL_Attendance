using DLAttendance.Models;

namespace DLAttendance.ViewModels;

public class EmployeeIndexViewModel
{
    public string? Search { get; set; }

    public PagedResult<DlEmployee> PagedEmployees { get; set; } = new();
}
