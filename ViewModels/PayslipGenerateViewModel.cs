using System.ComponentModel.DataAnnotations;

namespace DLAttendance.ViewModels;

public class PayslipGenerateViewModel
{
    [Display(Name = "Employee")]
    public int? DlEmployeeId { get; set; }

    [Required, Display(Name = "Salary Month")]
    public int Month { get; set; } = DateTime.Today.Month;

    [Required, Display(Name = "Salary Year")]
    public int Year { get; set; } = DateTime.Today.Year;

    [Display(Name = "Generate for all employees")]
    public bool GenerateForAll { get; set; }

    [Display(Name = "Basic Rate")]
    public decimal BasicRate { get; set; }

    [Display(Name = "HRA Rate")]
    public decimal HraRate { get; set; }

    [Display(Name = "Special Allowance Rate")]
    public decimal SpecialAllowanceRate { get; set; }

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
}
