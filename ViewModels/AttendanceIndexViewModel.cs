using DLAttendance.Models;

namespace DLAttendance.ViewModels;

public class AttendanceIndexViewModel
{
    public string? Search { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Status { get; set; }

    public PagedResult<DlAttendanceRecord> PagedRecords { get; set; } = new();
}
