using DLAttendance.Models;

namespace DLAttendance.ViewModels;

public class PayslipIndexViewModel
{
    public string? Search { get; set; }

    public int? Month { get; set; }

    public int? Year { get; set; }

    public PagedResult<DlPayslip> PagedPayslips { get; set; } = new();
}
