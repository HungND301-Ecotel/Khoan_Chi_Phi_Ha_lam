using Application.Catalog.Permissions;
using Application.Utility;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using Domain.Entities.Index;
using EfCore.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace EfCore.Persistence.Initialization;

internal class ApplicationDbSeeder(
    CustomSeederRunner seederRunner,
    IPermissionEnumSeeder permissionEnumSeeder,
    IPermissionCatalogSynchronizer permissionCatalogSynchronizer)
{
    public async Task SeedDatabaseAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        await seederRunner.RunSeedersAsync(cancellationToken);
        await SeedRolesAsync(dbContext);
        await SeedPositionsAsync(dbContext);
        await SeedDepartmentsAsync(dbContext);
        await SeedUsersAsync(dbContext);
        await SeedEmployeesAsync(dbContext);

        // Seed bảng Permission từ enum PermissionCode
        await permissionEnumSeeder.SeedAsync(cancellationToken);

        // Sync Module/SubModule từ [HasPermission] attributes trên Controller
        await permissionCatalogSynchronizer.SynchronizeAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (context.Roles.Any())
        {
            return;
        }

        var roles = new List<Role>
        {
            Role.Create(RoleType.SystemAdmin, "admin"),
            Role.Create(RoleType.User, "user")
        };

        foreach (var role in roles)
        {
            role.NormalizedName = role.Name.ToUpperInvariant();
        }

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPositionsAsync(ApplicationDbContext context)
    {
        if (context.Positions.Any())
        {
            return;
        }

        var positions = new List<Position>
        {
            Position.Create("Quản trị hệ thống",0, "System Administrator"),
            Position.Create("Nhân viên",0, "Employee")
        };

        context.Positions.AddRange(positions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDepartmentsAsync(ApplicationDbContext context)
    {
        if (context.Departments.Any())
        {
            return;
        }

        var department = Department.Create("Phòng IT", "IT");

        context.Departments.Add(department);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        string defaultPasswordHash = Utils.ComputeHash(AppConsts.DefaultPassword);

        var roleMap = await context.Roles
            .ToDictionaryAsync(r => r.RoleType, r => r.Id);

        var admin = new User("admin", "admin@company.com", "0312040047");
        admin.SetPassword(defaultPasswordHash);
        admin.VerifyEmail();
        admin.VerifyPhone();
        admin.SetRegisterProvider("Seed");
        admin.AddRole(roleMap[RoleType.SystemAdmin], RoleType.SystemAdmin);

        var user = new User("user", "user@company.com", "0312040048");
        user.SetPassword(defaultPasswordHash);
        user.SetRegisterProvider("Seed");
        user.AddRole(roleMap[RoleType.User], RoleType.User);

        context.Users.AddRange(admin, user);
        await context.SaveChangesAsync();
    }

    private static async Task SeedEmployeesAsync(ApplicationDbContext context)
    {
        if (context.Employees.Any())
        {
            return;
        }

        var adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.UserName == "admin");

        if (adminUser == null)
        {
            return;
        }

        var position = await context.Positions
            .FirstOrDefaultAsync(p => p.Name == "Quản trị hệ thống");

        var department = await context.Departments
            .FirstOrDefaultAsync();

        if (position == null || department == null)
        {
            return;
        }

        var adminEmployee = Employee.Create(
            fullName: "System Administrator",
            userId: adminUser.Id,
            positionId: position.Id,
            departmentId: department.Id,
            avatarUrl: string.Empty,
            dob: null,
            gender: null,
            cccd: "000000000000",
            province: "Quảng Ninh",
            district: null,
            ward: null,
            streetAddress: null);

        context.Employees.Add(adminEmployee);
        await context.SaveChangesAsync();
    }
}