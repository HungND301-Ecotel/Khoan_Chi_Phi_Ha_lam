using Application.Utility;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using EfCore.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace EfCore.Persistence.Initialization;

internal class ApplicationDbSeeder(CustomSeederRunner seederRunner)
{
    public async Task SeedDatabaseAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        await seederRunner.RunSeedersAsync(cancellationToken);
        await SeedRolesAsync(dbContext);
        await SeedUsersAsync(dbContext);
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

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }
        string defaultPasswordHash = Utils.ComputeHash(AppConsts.DefaultPassword);

        // Load roleId từ DB để tránh hard-code
        var roleMap = await context.Roles
            .ToDictionaryAsync(r => r.RoleType, r => r.Id);

        var users = new List<User>();

        // ================= ADMIN =================
        var admin = new User("admin", "admin@company.com", "System Administrator");
        admin.SetPassword(defaultPasswordHash);
        admin.VerifyEmail();
        admin.VerifyPhone();
        admin.SetRegisterProvider("Seed");
        admin.AddRole(roleMap[RoleType.SystemAdmin], RoleType.SystemAdmin);
        users.Add(admin);

        // ================= USER =================
        var user = new User("user", "user@company.com", "Normal User");
        user.SetPassword(defaultPasswordHash);
        user.SetRegisterProvider("Seed");
        user.AddRole(roleMap[RoleType.User], RoleType.User);
        users.Add(user);

        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
}