using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DLAttendance.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
}

public class UserEditViewModel
{
    public string? Id { get; set; }

    [Required, StringLength(50), Display(Name = "User Name")]
    public string UserName { get; set; } = string.Empty;

    [DataType(DataType.Password), Display(Name = "Password / Reset Password")]
    public string? Password { get; set; }

    [Display(Name = "Roles")]
    public List<string> SelectedRoles { get; set; } = [];

    public List<SelectListItem> AvailableRoles { get; set; } = [];

    [StringLength(30), Display(Name = "Employee ID")]
    public string? EmployeeCode { get; set; }

    public List<SelectListItem> AvailableEmployees { get; set; } = [];
}

public class RoleEditViewModel
{
    public string? Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
}
