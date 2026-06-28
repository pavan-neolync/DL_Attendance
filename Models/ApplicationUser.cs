using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DLAttendance.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(30), Display(Name = "Employee ID")]
    public string? EmployeeCode { get; set; }
}
