namespace DLAttendance.ViewModels;

public class PayslipViewModel
{
    public string CompanyName { get; set; } = string.Empty;

    public string CompanyAddress { get; set; } = string.Empty;

    public string StatementTitle { get; set; } = string.Empty;

    public string EmployeeCode { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string? Location { get; set; }

    public string? Gender { get; set; }

    public string? Designation { get; set; }

    public string? Department { get; set; }

    public string? PfNumber { get; set; }

    public string? AccountNumber { get; set; }

    public string? BankName { get; set; }

    public string? JoinDate { get; set; }

    public string? BirthDate { get; set; }

    public string? PanNumber { get; set; }

    public int DaysWorked { get; set; }

    public string? Uan { get; set; }

    public string? EsicNumber { get; set; }

    public decimal BasicRate { get; set; }

    public decimal BasicEarnings { get; set; }

    public decimal HraRate { get; set; }

    public decimal HraEarnings { get; set; }

    public decimal SpecialAllowanceRate { get; set; }

    public decimal SpecialAllowanceEarnings { get; set; }

    public decimal PfDeduction { get; set; }

    public decimal VpfDeduction { get; set; }

    public decimal IncomeTaxDeduction { get; set; }

    public decimal ProfessionTaxDeduction { get; set; }

    public decimal EsicDeduction { get; set; }

    public decimal GrossEarnings =>
        BasicEarnings + HraEarnings + SpecialAllowanceEarnings;

    public decimal TotalDeductions =>
        PfDeduction + VpfDeduction + IncomeTaxDeduction + ProfessionTaxDeduction + EsicDeduction;

    public decimal NetPay => GrossEarnings - TotalDeductions;

    public string NetPayInWords { get; set; } = string.Empty;

    public IReadOnlyList<string> LopDetails { get; set; } = [];
}
