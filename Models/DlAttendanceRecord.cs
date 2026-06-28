using System.ComponentModel.DataAnnotations;

namespace DLAttendance.Models;

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Leave = 3,
    HalfDay = 4
}

public class DlAttendanceRecord
{
    public int Id { get; set; }

    [Display(Name = "DL Employee")]
    public int? DlEmployeeId { get; set; }

    public DlEmployee? DlEmployee { get; set; }

    [Required, StringLength(30), Display(Name = "DL Code")]
    public string DlCode { get; set; } = string.Empty;

    [Required, StringLength(120), Display(Name = "DL Name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Department { get; set; }

    [StringLength(80)]
    public string? Contractor { get; set; }

    [DataType(DataType.Date), Display(Name = "Attendance Date")]
    public DateTime WorkDate { get; set; } = DateTime.Today;

    [StringLength(20)]
    public string? Shift { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    [DataType(DataType.Time), Display(Name = "In Time")]
    public TimeSpan? InTime { get; set; }

    [DataType(DataType.Time), Display(Name = "Out Time")]
    public TimeSpan? OutTime { get; set; }

    [StringLength(250)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
