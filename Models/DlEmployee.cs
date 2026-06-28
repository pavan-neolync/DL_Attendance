using System.ComponentModel.DataAnnotations;

namespace DLAttendance.Models;

public class DlEmployee
{
    public int Id { get; set; }

    [Required, StringLength(30), Display(Name = "Employee ID")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120), Display(Name = "BU Name")]
    public string? BuName { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [DataType(DataType.Date), Display(Name = "Birth Date")]
    public DateTime? BirthDate { get; set; }

    public int? Age { get; set; }

    [DataType(DataType.Date), Display(Name = "Join Date")]
    public DateTime? JoinDate { get; set; }

    [StringLength(20)]
    public string? Aadhaar { get; set; }

    [StringLength(30)]
    public string? Uan { get; set; }

    [StringLength(30), Display(Name = "ESIC Number")]
    public string? EsicNumber { get; set; }

    [StringLength(120), Display(Name = "Bank Name")]
    public string? BankName { get; set; }

    [StringLength(40), Display(Name = "Account Number")]
    public string? AccountNumber { get; set; }

    [StringLength(20), Display(Name = "IFSC Code")]
    public string? IfscCode { get; set; }

    [StringLength(120)]
    public string? Vendor { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    [StringLength(20), Display(Name = "Mobile Number")]
    public string? MobileNumber { get; set; }

    [StringLength(80)]
    public string? Designation { get; set; }

    [StringLength(120)]
    public string? Location { get; set; }

    [StringLength(20), Display(Name = "PAN Number")]
    public string? PanNumber { get; set; }

    [StringLength(40), Display(Name = "PF Number")]
    public string? PfNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
