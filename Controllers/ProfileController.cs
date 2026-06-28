using DLAttendance.Data;
using DLAttendance.Models;
using DLAttendance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DLAttendance.Controllers;

[Authorize(Roles = "Viewer")]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser?.EmployeeCode is null)
        {
            ViewBag.Error = "Your account is not linked to an employee record. Please contact your administrator.";
            return View("ProfileError");
        }

        var employee = await _context.DlEmployees
            .FirstOrDefaultAsync(e => e.EmployeeCode == appUser.EmployeeCode);

        if (employee is null)
        {
            ViewBag.Error = "Employee record not found. Please contact your administrator.";
            return View("ProfileError");
        }

        return View(employee);
    }

    public async Task<IActionResult> MyAttendance(string? fromDate, string? toDate, string? status, int page = 1, int pageSize = Paging.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var appUser = await _userManager.GetUserAsync(User);
        if (appUser?.EmployeeCode is null)
        {
            ViewBag.Error = "Your account is not linked to an employee record. Please contact your administrator.";
            return View("ProfileError");
        }

        var query = _context.DlAttendanceRecords
            .Where(r => r.DlCode == appUser.EmployeeCode);

        if (DateTime.TryParse(fromDate, out var fd))
            query = query.Where(r => r.WorkDate >= fd);
        if (DateTime.TryParse(toDate, out var td))
            query = query.Where(r => r.WorkDate <= td);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AttendanceStatus>(status, out var s))
            query = query.Where(r => r.Status == s);

        var totalCount = await query.CountAsync();
        var records = await query
            .OrderByDescending(r => r.WorkDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var paged = new PagedResult<DlAttendanceRecord>
        {
            Items = records,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Status = status;

        return View(new AttendanceIndexViewModel
        {
            Search = null,
            FromDate = string.IsNullOrWhiteSpace(fromDate) ? null : DateTime.TryParse(fromDate, out var fddt) ? fddt : null,
            ToDate = string.IsNullOrWhiteSpace(toDate) ? null : DateTime.TryParse(toDate, out var tddt) ? tddt : null,
            Status = status,
            PagedRecords = paged
        });
    }
}
