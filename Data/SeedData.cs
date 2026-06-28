using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DLAttendance.Models;

namespace DLAttendance.Data;

public static class SeedData
{
    private static readonly string[] Roles = ["Admin", "HR", "Viewer"];

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userName = configuration["SeedAdmin:UserName"] ?? "admin";
        var password = configuration["SeedAdmin:Password"] ?? "Admin@12345";
        var admin = await userManager.FindByNameAsync(userName);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = userName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Unable to seed admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        await EnsureUserEmployeeCodeAsync(db);
        await EnsureDeviceTableAsync(db);
        await EnsurePayslipSchemaAsync(db);
        await SeedDevicesAsync(db);
        await SeedDummyAttendanceAsync(db);
    }

    private static async Task EnsureDeviceTableAsync(ApplicationDbContext db)
    {
        const string sql = """
            IF OBJECT_ID(N'[Devices]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Devices] (
                    [Id] int NOT NULL IDENTITY,
                    [DeviceName] nvarchar(100) NOT NULL,
                    [IpAddress] nvarchar(45) NOT NULL,
                    [SerialNumber] nvarchar(80) NOT NULL,
                    [Location] nvarchar(120) NOT NULL,
                    [Status] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Devices] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_Devices_SerialNumber] ON [Devices] ([SerialNumber]);
            END
            """;

        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task EnsurePayslipSchemaAsync(ApplicationDbContext db)
    {
        const string employeeColumnsSql = """
            IF COL_LENGTH('DlEmployees', 'Designation') IS NULL
                ALTER TABLE [DlEmployees] ADD [Designation] nvarchar(80) NULL;
            IF COL_LENGTH('DlEmployees', 'Location') IS NULL
                ALTER TABLE [DlEmployees] ADD [Location] nvarchar(120) NULL;
            IF COL_LENGTH('DlEmployees', 'PanNumber') IS NULL
                ALTER TABLE [DlEmployees] ADD [PanNumber] nvarchar(20) NULL;
            IF COL_LENGTH('DlEmployees', 'PfNumber') IS NULL
                ALTER TABLE [DlEmployees] ADD [PfNumber] nvarchar(40) NULL;
            """;

        const string payslipTableSql = """
            IF OBJECT_ID(N'[DlPayslips]', N'U') IS NULL
            BEGIN
                CREATE TABLE [DlPayslips] (
                    [Id] int NOT NULL IDENTITY,
                    [DlEmployeeId] int NOT NULL,
                    [Year] int NOT NULL,
                    [Month] int NOT NULL,
                    [BasicRate] decimal(18,2) NOT NULL,
                    [HraRate] decimal(18,2) NOT NULL,
                    [SpecialAllowanceRate] decimal(18,2) NOT NULL,
                    [BasicEarnings] decimal(18,2) NOT NULL,
                    [HraEarnings] decimal(18,2) NOT NULL,
                    [SpecialAllowanceEarnings] decimal(18,2) NOT NULL,
                    [PfDeduction] decimal(18,2) NOT NULL,
                    [VpfDeduction] decimal(18,2) NOT NULL,
                    [IncomeTaxDeduction] decimal(18,2) NOT NULL,
                    [ProfessionTaxDeduction] decimal(18,2) NOT NULL,
                    [EsicDeduction] decimal(18,2) NOT NULL,
                    [DaysWorked] int NOT NULL,
                    [LopDetails] nvarchar(max) NULL,
                    [GeneratedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_DlPayslips] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_DlPayslips_DlEmployees_DlEmployeeId] FOREIGN KEY ([DlEmployeeId]) REFERENCES [DlEmployees] ([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_DlPayslips_DlEmployeeId_Year_Month] ON [DlPayslips] ([DlEmployeeId], [Year], [Month]);
            END
            """;

        await db.Database.ExecuteSqlRawAsync(employeeColumnsSql);
        await db.Database.ExecuteSqlRawAsync(payslipTableSql);
    }

    private static async Task SeedDevicesAsync(ApplicationDbContext db)
    {
        if (await db.Devices.AnyAsync())
        {
            return;
        }

        var locations = new[]
        {
            "Main Gate", "Assembly Line 1", "Assembly Line 2", "Packing Area", "Stores",
            "Canteen", "Admin Office", "Security Cabin", "Warehouse", "Maintenance"
        };

        var devices = Enumerable.Range(1, 30).Select(index => new Device
        {
            DeviceName = $"DL Attendance Device {index:00}",
            IpAddress = $"192.168.10.{50 + index}",
            SerialNumber = $"DL-DEV-{DateTime.Today:yyyy}-{index:0000}",
            Location = locations[(index - 1) % locations.Length],
            Status = index % 6 == 0 ? DeviceStatus.Inactive : DeviceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        db.Devices.AddRange(devices);
        await db.SaveChangesAsync();
    }

    private static async Task SeedDummyAttendanceAsync(ApplicationDbContext db)
    {
        if (await db.DlAttendanceRecords.AnyAsync())
        {
            return;
        }

        if (!await db.DlEmployees.AnyAsync())
        {
            db.DlEmployees.AddRange(CreateDummyEmployees());
            await db.SaveChangesAsync();
        }

        var employees = await db.DlEmployees
            .OrderBy(x => x.EmployeeCode)
            .Take(60)
            .ToListAsync();

        var random = new Random(20260620);
        var today = DateTime.Today;
        var startDate = today.AddMonths(-3).AddDays(1);
        var shifts = new[]
        {
            new { Name = "A", In = new TimeSpan(6, 0, 0), Out = new TimeSpan(14, 0, 0) },
            new { Name = "B", In = new TimeSpan(14, 0, 0), Out = new TimeSpan(22, 0, 0) },
            new { Name = "C", In = new TimeSpan(22, 0, 0), Out = new TimeSpan(6, 0, 0) }
        };

        var records = new List<DlAttendanceRecord>();
        for (var date = startDate.Date; date <= today; date = date.AddDays(1))
        {
            foreach (var employee in employees)
            {
                var roll = random.Next(100);
                var status = roll < 84
                    ? AttendanceStatus.Present
                    : roll < 94
                        ? AttendanceStatus.Absent
                        : AttendanceStatus.Leave;
                var shift = shifts[random.Next(shifts.Length)];

                records.Add(new DlAttendanceRecord
                {
                    DlEmployeeId = employee.Id,
                    DlCode = employee.EmployeeCode,
                    FullName = employee.Name,
                    Department = employee.BuName,
                    Contractor = employee.Vendor,
                    WorkDate = date,
                    Shift = shift.Name,
                    Status = status,
                    InTime = status == AttendanceStatus.Present ? shift.In : null,
                    OutTime = status == AttendanceStatus.Present ? shift.Out : null,
                    Remarks = status == AttendanceStatus.Present ? "Dummy 8 hours attendance" : "Dummy attendance data",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        db.DlAttendanceRecords.AddRange(records);
        await db.SaveChangesAsync();
    }

    private static IEnumerable<DlEmployee> CreateDummyEmployees()
    {
        var names = new[]
        {
            "A Soniya", "V Hemavathi", "T Thulasi", "S A Aarthi", "T Deepika",
            "D Nirmala", "V Sravani", "D Rizwana", "E Chandrakala", "G Guravamma",
            "H Mani", "D Sumalatha", "B Sai Kumari", "N Bhavani", "K Subhashini",
            "M Madhavi", "H S Jhansi", "V Keerthi Priya", "E Subhash", "D Hyma",
            "J Jayachitra", "K Asha Latha", "V Swapna", "K Muni Lakshmi"
        };

        return names.Select((name, index) => new DlEmployee
        {
            EmployeeCode = $"DUMMY{index + 1:000}",
            Name = name,
            BuName = "UTNPL - Apex",
            Gender = index % 5 == 0 ? "Male" : "Female",
            Age = 22 + index % 16,
            JoinDate = DateTime.Today.AddMonths(-18).AddDays(index),
            Vendor = index % 3 == 0 ? "Apex Labour" : "Blue Ocean",
            Category = index % 4 == 0 ? "CL" : "CW",
            MobileNumber = $"900000{index + 1:0000}",
            Aadhaar = $"99990000{index + 1:0000}",
            Uan = $"10170000{index + 1:0000}",
            EsicNumber = $"620900{index + 1:0000}",
            BankName = index % 2 == 0 ? "STATE BANK OF INDIA" : "UNION BANK OF INDIA",
            AccountNumber = $"100200300{index + 1:0000}",
            IfscCode = index % 2 == 0 ? "SBIN0016530" : "UBIN0821446",
            Designation = index % 4 == 0 ? "OPERATOR" : "HELPER",
            Location = "Tirupati - Andhra Pradesh",
            PfNumber = $"BGBNG21672190000010{index + 1:000}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private static async Task EnsureUserEmployeeCodeAsync(ApplicationDbContext db)
    {
        const string sql = """
            IF COL_LENGTH('AspNetUsers', 'EmployeeCode') IS NULL
                ALTER TABLE [AspNetUsers] ADD [EmployeeCode] nvarchar(30) NULL;

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_AspNetUsers_EmployeeCode'
                  AND object_id = OBJECT_ID('AspNetUsers')
            )
            BEGIN
                CREATE UNIQUE INDEX [IX_AspNetUsers_EmployeeCode]
                ON [AspNetUsers] ([EmployeeCode])
                WHERE [EmployeeCode] IS NOT NULL;
            END
            """;
        await db.Database.ExecuteSqlRawAsync(sql);
    }

}
