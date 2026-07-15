using Application.Common.UnitOfWork;
using Domain.Common.Enums;
using Domain.Entities.Identity;

namespace Application.Catalog.Permissions;

public class PermissionEnumSeeder(IUnitOfWork unitOfWork) : IPermissionEnumSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var repo = unitOfWork.GetRepository<Permission>();
        var existing = await repo.GetAllAsync(disableTracking: false);
        var existingCodes = existing.Select(x => x.Code).ToHashSet();

        var hasChanges = false;

        foreach (var code in Enum.GetValues<PermissionCode>())
        {
            if (!existingCodes.Contains(code))
            {
                await repo.InsertAsync(Permission.Create(code, code.ToString(), null));
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }
}