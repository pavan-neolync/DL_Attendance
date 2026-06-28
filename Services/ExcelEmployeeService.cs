using ClosedXML.Excel;
using DLAttendance.Models;

namespace DLAttendance.Services;

public class ExcelEmployeeService
{
    private static readonly string[] Headers =
    [
        "Employee ID", "Name", "BU Name", "Gender", "BirthDT", "AGE", "JoinDT",
        "Aadhaar", "UAN", "ESIC Number", "Bank Name", "Account Number",
        "IFSC Code", "Vendor", "Category", "Mobile Number"
    ];

    public byte[] Export(IEnumerable<DlEmployee> employees, bool includeSensitiveData = true)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("DL Employees");
        for (var i = 0; i < Headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = Headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var employee in employees)
        {
            sheet.Cell(row, 1).Value = employee.EmployeeCode;
            sheet.Cell(row, 2).Value = employee.Name;
            sheet.Cell(row, 3).Value = employee.BuName;
            sheet.Cell(row, 4).Value = employee.Gender;
            sheet.Cell(row, 5).Value = employee.BirthDate;
            sheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(row, 6).Value = employee.Age;
            sheet.Cell(row, 7).Value = employee.JoinDate;
            sheet.Cell(row, 7).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(row, 8).Value = includeSensitiveData ? employee.Aadhaar : "*****";
            sheet.Cell(row, 9).Value = includeSensitiveData ? employee.Uan : "*****";
            sheet.Cell(row, 10).Value = employee.EsicNumber;
            sheet.Cell(row, 11).Value = employee.BankName;
            sheet.Cell(row, 12).Value = employee.AccountNumber;
            sheet.Cell(row, 13).Value = employee.IfscCode;
            sheet.Cell(row, 14).Value = employee.Vendor;
            sheet.Cell(row, 15).Value = employee.Category;
            sheet.Cell(row, 16).Value = employee.MobileNumber;
            row++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<DlEmployee> Import(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1) ?? [];
        var employees = new List<DlEmployee>();

        foreach (var row in rows)
        {
            var employeeCode = row.Cell(2).GetString().Trim();
            var name = row.Cell(3).GetString().Trim();
            if (string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            employees.Add(new DlEmployee
            {
                EmployeeCode = employeeCode,
                Name = name,
                BuName = EmptyToNull(row.Cell(4).GetString()),
                Gender = EmptyToNull(row.Cell(5).GetString()),
                BirthDate = ReadDate(row.Cell(6)),
                Age = ReadInt(row.Cell(7)),
                JoinDate = ReadDate(row.Cell(8)),
                Aadhaar = EmptyToNull(ReadText(row.Cell(9))),
                Uan = EmptyToNull(ReadText(row.Cell(10))),
                EsicNumber = EmptyToNull(ReadText(row.Cell(11))),
                BankName = EmptyToNull(row.Cell(12).GetString()),
                AccountNumber = EmptyToNull(ReadText(row.Cell(13))),
                IfscCode = EmptyToNull(row.Cell(14).GetString()),
                Vendor = EmptyToNull(row.Cell(15).GetString()),
                Category = EmptyToNull(row.Cell(16).GetString()),
                MobileNumber = EmptyToNull(ReadText(row.Cell(17))),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return employees;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ReadText(IXLCell cell)
    {
        var text = cell.GetFormattedString();
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text.Trim();
        }

        return cell.GetString().Trim();
    }

    private static DateTime? ReadDate(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var date))
        {
            return date.Date;
        }

        if (cell.TryGetValue<double>(out var serial))
        {
            return DateTime.FromOADate(serial).Date;
        }

        return DateTime.TryParse(cell.GetString(), out date) ? date.Date : null;
    }

    private static int? ReadInt(IXLCell cell)
    {
        if (cell.TryGetValue<int>(out var number))
        {
            return number;
        }

        return int.TryParse(cell.GetString(), out number) ? number : null;
    }
}
