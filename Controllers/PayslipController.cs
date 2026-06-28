using DLAttendance.Data;
using DLAttendance.Models;
using DLAttendance.Services;
using DLAttendance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DLAttendance.Controllers;

[Authorize(Roles = "Admin,HR")]
public class PayslipController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PayslipService _payslipService;

    public PayslipController(ApplicationDbContext context, PayslipService payslipService)
    {
        _context = context;
        _payslipService = payslipService;
    }

    public async Task<IActionResult> Index(string? search, int? month, int? year, int page = 1, int pageSize = Paging.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = _context.DlPayslips
            .AsNoTracking()
            .Include(x => x.DlEmployee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.DlEmployee!.EmployeeCode.Contains(search) ||
                x.DlEmployee.Name.Contains(search));
        }

        if (month.HasValue)
        {
            query = query.Where(x => x.Month == month.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(x => x.Year == year.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ThenBy(x => x.DlEmployee!.EmployeeCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(new PayslipIndexViewModel
        {
            Search = search,
            Month = month,
            Year = year,
            PagedPayslips = new PagedResult<DlPayslip>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        });
    }

    public async Task<IActionResult> Generate()
    {
        await LoadEmployeesAsync();
        return View(new PayslipGenerateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(PayslipGenerateViewModel model)
    {
        if (!model.GenerateForAll && !model.DlEmployeeId.HasValue)
        {
            ModelState.AddModelError(nameof(model.DlEmployeeId), "Select an employee or choose generate for all.");
        }

        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync(model.DlEmployeeId);
            return View(model);
        }

        var employees = model.GenerateForAll
            ? await _context.DlEmployees.OrderBy(x => x.EmployeeCode).ToListAsync()
            : await _context.DlEmployees.Where(x => x.Id == model.DlEmployeeId).ToListAsync();

        if (employees.Count == 0)
        {
            TempData["Message"] = "No employees found to generate payslips.";
            return RedirectToAction(nameof(Index));
        }

        var generated = 0;
        foreach (var employee in employees)
        {
            await _payslipService.GenerateAsync(employee, model);
            generated++;
        }

        TempData["Message"] = $"{generated} payslip(s) generated for {new DateTime(model.Year, model.Month, 1):MMMM yyyy}.";
        return RedirectToAction(nameof(Index), new { month = model.Month, year = model.Year });
    }

    public async Task<IActionResult> Print(int id)
    {
        var payslip = await _context.DlPayslips
            .AsNoTracking()
            .Include(x => x.DlEmployee)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (payslip?.DlEmployee is null)
        {
            return NotFound();
        }

        return View(_payslipService.ToViewModel(payslip, payslip.DlEmployee));
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
}
