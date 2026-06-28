namespace DLAttendance.ViewModels;

public class DashboardViewModel
{
    public int EmployeeCount { get; set; }
    public int TodayPresent { get; set; }
    public int TodayAbsent { get; set; }
    public int TodayLeave { get; set; }
    public IReadOnlyList<DailyAttendancePoint> WeekAttendance { get; set; } = [];
}

public class DailyAttendancePoint
{
    public string Day { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int HeightPercent { get; set; }
}
