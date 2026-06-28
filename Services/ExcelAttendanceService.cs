using ClosedXML.Excel;
using DLAttendance.Models;

namespace DLAttendance.Services;

public class ExcelAttendanceService
{
    private static readonly string[] Headers =
    [
        "DL Code", "DL Name", "Department", "Contractor", "Attendance Date",
        "Shift", "Status", "In Time", "Out Time", "Remarks"
    ];

    public byte[] Export(IEnumerable<DlAttendanceRecord> records)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("DL Attendance");

        for (var i = 0; i < Headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = Headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var record in records)
        {
            sheet.Cell(row, 1).Value = record.DlCode;
            sheet.Cell(row, 2).Value = record.FullName;
            sheet.Cell(row, 3).Value = record.Department;
            sheet.Cell(row, 4).Value = record.Contractor;
            sheet.Cell(row, 5).Value = record.WorkDate;
            sheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(row, 6).Value = record.Shift;
            sheet.Cell(row, 7).Value = record.Status.ToString();
            sheet.Cell(row, 8).Value = record.InTime?.ToString(@"hh\:mm");
            sheet.Cell(row, 9).Value = record.OutTime?.ToString(@"hh\:mm");
            sheet.Cell(row, 10).Value = record.Remarks;
            row++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<DlAttendanceRecord> Import(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1) ?? [];
        var records = new List<DlAttendanceRecord>();

        foreach (var row in rows)
        {
            var dlCode = row.Cell(1).GetString().Trim();
            var fullName = row.Cell(2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(dlCode) || string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            records.Add(new DlAttendanceRecord
            {
                DlCode = dlCode,
                FullName = fullName,
                Department = EmptyToNull(row.Cell(3).GetString()),
                Contractor = EmptyToNull(row.Cell(4).GetString()),
                WorkDate = ReadDate(row.Cell(5)) ?? DateTime.Today,
                Shift = EmptyToNull(row.Cell(6).GetString()),
                Status = ReadStatus(row.Cell(7).GetString()),
                InTime = ReadTime(row.Cell(8)),
                OutTime = ReadTime(row.Cell(9)),
                Remarks = EmptyToNull(row.Cell(10).GetString()),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return records;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? ReadDate(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var date))
        {
            return date.Date;
        }

        return DateTime.TryParse(cell.GetString(), out date) ? date.Date : null;
    }

    private static TimeSpan? ReadTime(IXLCell cell)
    {
        if (cell.TryGetValue<TimeSpan>(out var time))
        {
            return time;
        }

        return TimeSpan.TryParse(cell.GetString(), out time) ? time : null;
    }

    private static AttendanceStatus ReadStatus(string status) =>
        Enum.TryParse<AttendanceStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : AttendanceStatus.Present;
}
