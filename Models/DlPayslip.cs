using System.ComponentModel.DataAnnotations;

namespace DLAttendance.Models;

public class DlPayslip
{
    public int Id { get; set; }

    public int DlEmployeeId { get; set; }

    public DlEmployee? DlEmployee { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    [Display(Name = "Basic Rate")]
    public decimal BasicRate { get; set; }

    [Display(Name = "HRA Rate")]
    public decimal HraRate { get; set; }

    [Display(Name = "Special Allowance Rate")]
    public decimal SpecialAllowanceRate { get; set; }

    public decimal BasicEarnings { get; set; }

    public decimal HraEarnings { get; set; }

    public decimal SpecialAllowanceEarnings { get; set; }

    [Display(Name = "P.F.")]
    public decimal PfDeduction { get; set; }

    [Display(Name = "V.P.F.")]
    public decimal VpfDeduction { get; set; }

    [Display(Name = "Income Tax")]
    public decimal IncomeTaxDeduction { get; set; }

    [Display(Name = "Profession Tax")]
    public decimal ProfessionTaxDeduction { get; set; }

    [Display(Name = "ESIC")]
    public decimal EsicDeduction { get; set; }

    [Display(Name = "Days Worked")]
    public int DaysWorked { get; set; }

    public string? LopDetails { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
