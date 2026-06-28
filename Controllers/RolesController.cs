using DLAttendance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DLAttendance.Controllers;

[Authorize(Roles = "Admin")]
public class RolesController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index() =>
        View(await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync());

    public IActionResult Create() => View(new RoleEditViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(model.Name.Trim()));
        AddErrors(result);
        return result.Succeeded ? RedirectToAction(nameof(Index)) : View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        return role is null ? NotFound() : View(new RoleEditViewModel { Id = role.Id, Name = role.Name ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var role = await _roleManager.FindByIdAsync(model.Id ?? string.Empty);
        if (role is null)
        {
            return NotFound();
        }

        role.Name = model.Name.Trim();
        var result = await _roleManager.UpdateAsync(role);
        AddErrors(result);
        return result.Succeeded ? RedirectToAction(nameof(Index)) : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is not null)
        {
            await _roleManager.DeleteAsync(role);
        }

        return RedirectToAction(nameof(Index));
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
