using DLAttendance.Data;
using DLAttendance.Models;
using DLAttendance.Options;
using DLAttendance.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DLAttendance.Services;

public class PayslipService
{
    private readonly ApplicationDbContext _context;
    private readonly CompanySettings _company;

    public PayslipService(ApplicationDbContext context, IOptions<CompanySettings> company)
    {
        _context = context;
        _company = company.Value;
    }

    public async Task<(int DaysWorked, string LopDetails)> CalculateAttendanceAsync(
        int employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var records = await _context.DlAttendanceRecords
            .AsNoTracking()
            .Where(x => x.DlEmployeeId == employeeId && x.WorkDate >= start && x.WorkDate <= end)
            .OrderBy(x => x.WorkDate)
            .ToListAsync(cancellationToken);

        var daysWorked = 0m;
        var lopLines = new List<string>();

        foreach (var record in records)
        {
            switch (record.Status)
            {
                case AttendanceStatus.Present:
                    daysWorked += 1;
                    break;
                case AttendanceStatus.HalfDay:
                    daysWorked += 0.5m;
                    break;
                case AttendanceStatus.Absent:
                    lopLines.Add($"{record.WorkDate:dd/MM/yyyy}–{record.WorkDate:dd/MM/yyyy} – Loss of Pay Days");
                    break;
            }
        }

        return ((int)Math.Round(daysWorked, MidpointRounding.AwayFromZero), string.Join(Environment.NewLine, lopLines));
    }

    public decimal Prorate(decimal rate, int daysWorked, int daysInMonth) =>
        daysInMonth == 0 ? 0 : Math.Round(rate * daysWorked / daysInMonth, 0, MidpointRounding.AwayFromZero);

    public async Task<DlPayslip> GenerateAsync(
        DlEmployee employee,
        PayslipGenerateViewModel input,
        CancellationToken cancellationToken = default)
    {
        var daysInMonth = DateTime.DaysInMonth(input.Year, input.Month);
        var (daysWorked, lopDetails) = await CalculateAttendanceAsync(employee.Id, input.Year, input.Month, cancellationToken);

        if (daysWorked == 0)
        {
            daysWorked = daysInMonth;
        }

        var payslip = new DlPayslip
        {
            DlEmployeeId = employee.Id,
            Year = input.Year,
            Month = input.Month,
            BasicRate = input.BasicRate,
            HraRate = input.HraRate,
            SpecialAllowanceRate = input.SpecialAllowanceRate,
            BasicEarnings = Prorate(input.BasicRate, daysWorked, daysInMonth),
            HraEarnings = Prorate(input.HraRate, daysWorked, daysInMonth),
            SpecialAllowanceEarnings = Prorate(input.SpecialAllowanceRate, daysWorked, daysInMonth),
            PfDeduction = input.PfDeduction,
            VpfDeduction = input.VpfDeduction,
            IncomeTaxDeduction = input.IncomeTaxDeduction,
            ProfessionTaxDeduction = input.ProfessionTaxDeduction,
            EsicDeduction = input.EsicDeduction,
            DaysWorked = daysWorked,
            LopDetails = string.IsNullOrWhiteSpace(lopDetails) ? null : lopDetails,
            GeneratedAt = DateTime.UtcNow
        };

        var existing = await _context.DlPayslips
            .FirstOrDefaultAsync(x => x.DlEmployeeId == employee.Id && x.Year == input.Year && x.Month == input.Month, cancellationToken);

        if (existing is null)
        {
            _context.DlPayslips.Add(payslip);
        }
        else
        {
            existing.BasicRate = payslip.BasicRate;
            existing.HraRate = payslip.HraRate;
            existing.SpecialAllowanceRate = payslip.SpecialAllowanceRate;
            existing.BasicEarnings = payslip.BasicEarnings;
            existing.HraEarnings = payslip.HraEarnings;
            existing.SpecialAllowanceEarnings = payslip.SpecialAllowanceEarnings;
            existing.PfDeduction = payslip.PfDeduction;
            existing.VpfDeduction = payslip.VpfDeduction;
            existing.IncomeTaxDeduction = payslip.IncomeTaxDeduction;
            existing.ProfessionTaxDeduction = payslip.ProfessionTaxDeduction;
            existing.EsicDeduction = payslip.EsicDeduction;
            existing.DaysWorked = payslip.DaysWorked;
            existing.LopDetails = payslip.LopDetails;
            existing.GeneratedAt = payslip.GeneratedAt;
            payslip = existing;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return payslip;
    }

    public PayslipViewModel ToViewModel(DlPayslip payslip, DlEmployee employee)
    {
        var monthName = new DateTime(payslip.Year, payslip.Month, 1).ToString("MMMM yyyy").ToUpperInvariant();
        var location = string.IsNullOrWhiteSpace(employee.Location)
            ? _company.City
            : employee.Location;

        return new PayslipViewModel
        {
            CompanyName = _company.Name,
            CompanyAddress = _company.FullAddress,
            StatementTitle = $"SALARY STATEMENT FOR {monthName}",
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.Name,
            Location = location,
            Gender = FormatGender(employee.Gender),
            Designation = employee.Designation ?? employee.Category,
            Department = employee.BuName,
            PfNumber = employee.PfNumber,
            AccountNumber = employee.AccountNumber,
            BankName = employee.BankName,
            JoinDate = employee.JoinDate?.ToString("dd/MM/yyyy"),
            BirthDate = employee.BirthDate?.ToString("dd/MM/yyyy"),
            PanNumber = employee.PanNumber,
            DaysWorked = payslip.DaysWorked,
            Uan = employee.Uan,
            EsicNumber = employee.EsicNumber,
            BasicRate = payslip.BasicRate,
            BasicEarnings = payslip.BasicEarnings,
            HraRate = payslip.HraRate,
            HraEarnings = payslip.HraEarnings,
            SpecialAllowanceRate = payslip.SpecialAllowanceRate,
            SpecialAllowanceEarnings = payslip.SpecialAllowanceEarnings,
            PfDeduction = payslip.PfDeduction,
            VpfDeduction = payslip.VpfDeduction,
            IncomeTaxDeduction = payslip.IncomeTaxDeduction,
            ProfessionTaxDeduction = payslip.ProfessionTaxDeduction,
            EsicDeduction = payslip.EsicDeduction,
            NetPayInWords = IndianNumberToWords.Convert(
                payslip.BasicEarnings + payslip.HraEarnings + payslip.SpecialAllowanceEarnings
                - payslip.PfDeduction - payslip.VpfDeduction - payslip.IncomeTaxDeduction
                - payslip.ProfessionTaxDeduction - payslip.EsicDeduction),
            LopDetails = string.IsNullOrWhiteSpace(payslip.LopDetails)
                ? []
                : payslip.LopDetails.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
        };
    }

    private static string? FormatGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return null;
        }

        return gender.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "M"
            : gender.StartsWith("F", StringComparison.OrdinalIgnoreCase) ? "F"
            : gender;
    }
}
