using DLAttendance.Data;
using DLAttendance.Models;
using DLAttendance.Services;
using DLAttendance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DLAttendance.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelAttendanceService _excel;

    public AttendanceController(ApplicationDbContext context, ExcelAttendanceService excel)
    {
        _context = context;
        _excel = excel;
    }

    public async Task<IActionResult> Index(string? search, DateTime? fromDate, DateTime? toDate, string? status, int page = 1, int pageSize = Paging.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = Filter(search, fromDate, toDate, status);
        var totalCount = await query.CountAsync();
        var records = await query
            .OrderByDescending(x => x.WorkDate)
            .ThenBy(x => x.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new AttendanceIndexViewModel
        {
            Search = search,
            FromDate = fromDate,
            ToDate = toDate,
            Status = status,
            PagedRecords = new PagedResult<DlAttendanceRecord>
            {
                Items = records,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await LoadEmployeesAsync();
        return View(new DlAttendanceRecord { WorkDate = DateTime.Today });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DlAttendanceRecord record)
    {
        if (record.DlEmployeeId.HasValue)
        {
            ModelState.Remove(nameof(record.DlCode));
            ModelState.Remove(nameof(record.FullName));
            await ApplyEmployeeAsync(record);
        }

        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync(record.DlEmployeeId);
            return View(record);
        }

        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        _context.Add(record);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await _context.DlAttendanceRecords.FindAsync(id);
        await LoadEmployeesAsync(record?.DlEmployeeId);
        return record is null ? NotFound() : View(record);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DlAttendanceRecord record)
    {
        if (id != record.Id)
        {
            return BadRequest();
        }

        if (record.DlEmployeeId.HasValue)
        {
            ModelState.Remove(nameof(record.DlCode));
            ModelState.Remove(nameof(record.FullName));
            await ApplyEmployeeAsync(record);
        }

        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync(record.DlEmployeeId);
            return View(record);
        }

        var existing = await _context.DlAttendanceRecords.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.DlEmployeeId = record.DlEmployeeId;
        existing.DlCode = record.DlCode;
        existing.FullName = record.FullName;
        existing.Department = record.Department;
        existing.Contractor = record.Contractor;
        existing.WorkDate = record.WorkDate;
        existing.Shift = record.Shift;
        existing.Status = record.Status;
        existing.InTime = record.InTime;
        existing.OutTime = record.OutTime;
        existing.Remarks = record.Remarks;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _context.DlAttendanceRecords.FindAsync(id);
        return record is null ? NotFound() : View(record);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var record = await _context.DlAttendanceRecords.FindAsync(id);
        if (record is not null)
        {
            _context.DlAttendanceRecords.Remove(record);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Message"] = "Choose an Excel file to import.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var records = _excel.Import(stream);
        _context.DlAttendanceRecords.AddRange(records);
        await _context.SaveChangesAsync();
        TempData["Message"] = $"{records.Count} attendance records imported.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Export(string? search, DateTime? fromDate, DateTime? toDate, string? status)
    {
        var records = await Filter(search, fromDate, toDate, status)
            .OrderByDescending(x => x.WorkDate)
            .ThenBy(x => x.FullName)
            .ToListAsync();
        var content = _excel.Export(records);
        var fileName = $"dl-attendance-{DateTime.Today:yyyyMMdd}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private IQueryable<DlAttendanceRecord> Filter(string? search, DateTime? fromDate, DateTime? toDate, string? status)
    {
        var query = _context.DlAttendanceRecords.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.DlCode.Contains(search) ||
                x.FullName.Contains(search) ||
                (x.Department != null && x.Department.Contains(search)) ||
                (x.Contractor != null && x.Contractor.Contains(search)));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.WorkDate <= toDate.Value.Date);
        }

        if (Enum.TryParse<AttendanceStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(x => x.Status == parsed);
        }

        return query;
    }

    private async Task LoadEmployeesAsync(int? selectedId = null)
    {
        var employees = await _context.DlEmployees
            .AsNoTracking()
            .OrderBy(x => x.EmployeeCode)
            .Select(x => new { x.Id, Label = x.EmployeeCode + " - " + x.Name })
            .ToListAsync();
        ViewBag.Employees = new SelectList(employees, "Id", "Label", selectedId);
    }

    private async Task ApplyEmployeeAsync(DlAttendanceRecord record)
    {
        if (!record.DlEmployeeId.HasValue)
        {
            return;
        }

        var employee = await _context.DlEmployees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == record.DlEmployeeId);
        if (employee is null)
        {
            return;
        }

        record.DlCode = employee.EmployeeCode;
        record.FullName = employee.Name;
        record.Department = employee.BuName;
        record.Contractor = employee.Vendor;
    }
}