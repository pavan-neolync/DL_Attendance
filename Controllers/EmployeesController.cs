using DLAttendance.Data;
using DLAttendance.Models;
using DLAttendance.Services;
using DLAttendance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DLAttendance.Controllers;

[Authorize]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelEmployeeService _excel;

    public EmployeesController(ApplicationDbContext context, ExcelEmployeeService excel)
    {
        _context = context;
        _excel = excel;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = Paging.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = FilterEmployees(search);
        var totalCount = await query.CountAsync();
        var employees = await query
            .OrderBy(x => x.EmployeeCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(new EmployeeIndexViewModel
        {
            Search = search,
            PagedEmployees = new PagedResult<DlEmployee>
            {
                Items = employees,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        });
    }

    [Authorize(Roles = "Admin,HR")]
    public IActionResult Create() => View(new DlEmployee());

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DlEmployee employee)
    {
        if (!ModelState.IsValid)
        {
            return View(employee);
        }

        employee.CreatedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;
        _context.Add(employee);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _context.DlEmployees.FindAsync(id);
        return employee is null ? NotFound() : View(employee);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DlEmployee employee)
    {
        if (id != employee.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(employee);
        }

        employee.UpdatedAt = DateTime.UtcNow;
        _context.Update(employee);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.DlEmployees.FindAsync(id);
        return employee is null ? NotFound() : View(employee);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var employee = await _context.DlEmployees.FindAsync(id);
        if (employee is not null)
        {
            _context.DlEmployees.Remove(employee);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Message"] = "Choose the DL employee master Excel file.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var imported = _excel.Import(stream)
            .GroupBy(x => x.EmployeeCode)
            .Select(x => x.Last())
            .ToList();
        var codes = imported.Select(x => x.EmployeeCode).ToList();
        var existing = await _context.DlEmployees
            .Where(x => codes.Contains(x.EmployeeCode))
            .ToDictionaryAsync(x => x.EmployeeCode);

        var created = 0;
        var updated = 0;
        foreach (var employee in imported)
        {
            if (existing.TryGetValue(employee.EmployeeCode, out var current))
            {
                Copy(employee, current);
                updated++;
            }
            else
            {
                _context.DlEmployees.Add(employee);
                created++;
            }
        }

        await _context.SaveChangesAsync();
        TempData["Message"] = $"{created} employees added and {updated} employees updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Export(string? search)
    {
        var employees = await FilterEmployees(search).OrderBy(x => x.EmployeeCode).ToListAsync();
        var content = _excel.Export(employees, User.IsInRole("Admin"));
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "dl-employees.xlsx");
    }

    private IQueryable<DlEmployee> FilterEmployees(string? search)
    {
        var query = _context.DlEmployees.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.EmployeeCode.Contains(search) ||
                x.Name.Contains(search) ||
                (x.BuName != null && x.BuName.Contains(search)) ||
                (x.Vendor != null && x.Vendor.Contains(search)) ||
                (x.Category != null && x.Category.Contains(search)));
        }

        return query;
    }

    private static void Copy(DlEmployee source, DlEmployee target)
    {
        target.Name = source.Name;
        target.BuName = source.BuName;
        target.Gender = source.Gender;
        target.BirthDate = source.BirthDate;
        target.Age = source.Age;
        target.JoinDate = source.JoinDate;
        target.Aadhaar = source.Aadhaar;
        target.Uan = source.Uan;
        target.EsicNumber = source.EsicNumber;
        target.BankName = source.BankName;
        target.AccountNumber = source.AccountNumber;
        target.IfscCode = source.IfscCode;
        target.Vendor = source.Vendor;
        target.Category = source.Category;
        target.MobileNumber = source.MobileNumber;
        target.Designation = source.Designation;
        target.Location = source.Location;
        target.PanNumber = source.PanNumber;
        target.PfNumber = source.PfNumber;
        target.UpdatedAt = DateTime.UtcNow;
    }
}