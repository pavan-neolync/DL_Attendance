using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using DLAttendance.Models;

namespace DLAttendance.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DlEmployee> DlEmployees => Set<DlEmployee>();

    public DbSet<DlAttendanceRecord> DlAttendanceRecords => Set<DlAttendanceRecord>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<DlPayslip> DlPayslips => Set<DlPayslip>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<DlEmployee>()
            .HasIndex(x => x.EmployeeCode)
            .IsUnique();
        builder.Entity<Device>()
            .HasIndex(x => x.SerialNumber)
            .IsUnique();
        builder.Entity<DlPayslip>()
            .HasIndex(x => new { x.DlEmployeeId, x.Year, x.Month })
            .IsUnique();
        // Ensure no two users share the same EmployeeCode
        builder.Entity<ApplicationUser>()
            .HasIndex(x => x.EmployeeCode)
            .IsUnique()
            .HasFilter("[EmployeeCode] IS NOT NULL");
    }
}
