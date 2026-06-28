using DLAttendance.Data;
using DLAttendance.Models;
using DLAttendance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DLAttendance.Controllers;

[Authorize]
public class DevicesController : Controller
{
    private readonly ApplicationDbContext _context;

    public DevicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = Paging.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = FilterDevices(search);
        var totalCount = await query.CountAsync();
        var devices = await query
            .OrderBy(x => x.DeviceName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(new DeviceIndexViewModel
        {
            Search = search,
            PagedDevices = new PagedResult<Device>
            {
                Items = devices,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Device());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Device device)
    {
        if (!ModelState.IsValid)
        {
            return View(device);
        }

        device.CreatedAt = DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        return device is null ? NotFound() : View(device);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Device device)
    {
        if (id != device.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(device);
        }

        var existing = await _context.Devices.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.DeviceName = device.DeviceName;
        existing.IpAddress = device.IpAddress;
        existing.SerialNumber = device.SerialNumber;
        existing.Location = device.Location;
        existing.Status = device.Status;
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        return device is null ? NotFound() : View(device);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is not null)
        {
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private IQueryable<Device> FilterDevices(string? search)
    {
        var query = _context.Devices.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.DeviceName.Contains(search) ||
                x.IpAddress.Contains(search) ||
                x.SerialNumber.Contains(search) ||
                x.Location.Contains(search));
        }

        return query;
    }
}
