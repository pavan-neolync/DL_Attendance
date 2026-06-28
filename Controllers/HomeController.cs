using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DLAttendance.Models;
using DLAttendance.Data;
using DLAttendance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DLAttendance.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-6);
        var weekRecords = await _context.DlAttendanceRecords
            .AsNoTracking()
            .Where(x => x.WorkDate >= weekStart && x.WorkDate <= today)
            .GroupBy(x => x.WorkDate.Date)
            .Select(x => new { Date = x.Key, Count = x.Count() })
            .ToListAsync();
        var max = Math.Max(weekRecords.Select(x => x.Count).DefaultIfEmpty(0).Max(), 1);

        var model = new DashboardViewModel
        {
            EmployeeCount = await _context.DlEmployees.CountAsync(),
            TodayPresent = await _context.DlAttendanceRecords.CountAsync(x => x.WorkDate == today && x.Status == AttendanceStatus.Present),
            TodayAbsent = await _context.DlAttendanceRecords.CountAsync(x => x.WorkDate == today && x.Status == AttendanceStatus.Absent),
            TodayLeave = await _context.DlAttendanceRecords.CountAsync(x => x.WorkDate == today && x.Status == AttendanceStatus.Leave),
            WeekAttendance = Enumerable.Range(0, 7).Select(i =>
            {
                var date = weekStart.AddDays(i);
                var count = weekRecords.FirstOrDefault(x => x.Date == date)?.Count ?? 0;
                return new DailyAttendancePoint
                {
                    Date = date,
                    Day = date.ToString("ddd"),
                    Count = count,
                    HeightPercent = Math.Max(6, (int)Math.Round(count * 100.0 / max))
                };
            }).ToList()
        };

        return View("Dashboard", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
