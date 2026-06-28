using DLAttendance.Data;
using DLAttendance.Models;
using DLAttendance.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DLAttendance.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(x => x.UserName).ToListAsync();
        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Roles = string.Join(", ", await _userManager.GetRolesAsync(user)),
                EmployeeCode = user.EmployeeCode
            });
        }

        return View(model);
    }

    public async Task<IActionResult> Create() => View(await BuildModelAsync(new UserEditViewModel()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserEditViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Password is required.");

        // Validate EmployeeCode uniqueness
        if (!string.IsNullOrWhiteSpace(model.EmployeeCode))
        {
            var existing = await _userManager.Users
                .AnyAsync(u => u.EmployeeCode == model.EmployeeCode.Trim());
            if (existing)
                ModelState.AddModelError(nameof(model.EmployeeCode), "This Employee ID is already linked to another user.");
        }

        if (!ModelState.IsValid)
            return View(await BuildModelAsync(model));

        var user = new ApplicationUser
        {
            UserName = model.UserName.Trim(),
            EmailConfirmed = true,
            EmployeeCode = string.IsNullOrWhiteSpace(model.EmployeeCode) ? null : model.EmployeeCode.Trim()
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (result.Succeeded && model.SelectedRoles.Count > 0)
            result = await _userManager.AddToRolesAsync(user, model.SelectedRoles);

        AddErrors(result);
        return result.Succeeded ? RedirectToAction(nameof(Index)) : View(await BuildModelAsync(model));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        return View(await BuildModelAsync(new UserEditViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            SelectedRoles = [.. await _userManager.GetRolesAsync(user)],
            EmployeeCode = user.EmployeeCode
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await BuildModelAsync(model));

        var user = await _userManager.FindByIdAsync(model.Id ?? string.Empty);
        if (user is null) return NotFound();

        // Validate EmployeeCode uniqueness (exclude current user)
        if (!string.IsNullOrWhiteSpace(model.EmployeeCode))
        {
            var existing = await _userManager.Users
                .AnyAsync(u => u.EmployeeCode == model.EmployeeCode.Trim() && u.Id != user.Id);
            if (existing)
            {
                ModelState.AddModelError(nameof(model.EmployeeCode), "This Employee ID is already linked to another user.");
                return View(await BuildModelAsync(model));
            }
        }

        user.UserName = model.UserName.Trim();
        user.Email = null;
        user.EmailConfirmed = true;
        user.EmployeeCode = string.IsNullOrWhiteSpace(model.EmployeeCode) ? null : model.EmployeeCode.Trim();

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var existingRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, existingRoles);
            result = await _userManager.AddToRolesAsync(user, model.SelectedRoles);
        }

        if (result.Succeeded && !string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            result = await _userManager.ResetPasswordAsync(user, token, model.Password);
        }

        AddErrors(result);
        return result.Succeeded ? RedirectToAction(nameof(Index)) : View(await BuildModelAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is not null)
            await _userManager.DeleteAsync(user);

        return RedirectToAction(nameof(Index));
    }

    private async Task<UserEditViewModel> BuildModelAsync(UserEditViewModel model)
    {
        var roles = await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync();
        model.AvailableRoles = roles.Select(role => new SelectListItem
        {
            Value = role.Name,
            Text = role.Name,
            Selected = model.SelectedRoles.Contains(role.Name ?? string.Empty)
        }).ToList();

        // Load employees that are not yet linked to any user (or linked to current user)
        var linkedCodes = await _userManager.Users
            .Where(u => u.EmployeeCode != null && (model.Id == null || u.Id != model.Id))
            .Select(u => u.EmployeeCode!)
            .ToListAsync();

        var employees = await _context.DlEmployees
            .Where(e => !linkedCodes.Contains(e.EmployeeCode))
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new { e.EmployeeCode, e.Name })
            .ToListAsync();

        model.AvailableEmployees = employees.Select(e => new SelectListItem
        {
            Value = e.EmployeeCode,
            Text = $"{e.EmployeeCode} – {e.Name}",
            Selected = e.EmployeeCode == model.EmployeeCode
        }).ToList();

        // If editing and current employee not in list (already linked to this user), add it
        if (!string.IsNullOrWhiteSpace(model.EmployeeCode) &&
            !model.AvailableEmployees.Any(x => x.Value == model.EmployeeCode))
        {
            var emp = await _context.DlEmployees
                .FirstOrDefaultAsync(e => e.EmployeeCode == model.EmployeeCode);
            if (emp is not null)
            {
                model.AvailableEmployees.Insert(0, new SelectListItem
                {
                    Value = emp.EmployeeCode,
                    Text = $"{emp.EmployeeCode} – {emp.Name}",
                    Selected = true
                });
            }
        }

        return model;
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }
}
