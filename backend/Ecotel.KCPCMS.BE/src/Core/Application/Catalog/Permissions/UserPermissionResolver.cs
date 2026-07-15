using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Permissions;

public class UserPermissionResolver(IUnitOfWork unitOfWork) : IUserPermissionResolver
{
    public async Task<List<string>> ResolveAsync(int userId, CancellationToken cancellationToken = default)
    {
        var employeeRepo = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

        var employee = await employeeRepo.GetFirstOrDefaultAsync(
            predicate: e => e.UserId == userId,
            disableTracking: true);

        if (employee is null)
        {
            return [];
        }

        var departmentModulePermissionRepo = unitOfWork.GetRepository<DepartmentModulePermission>();
        var positionSubmodulePermissionRepo = unitOfWork.GetRepository<PositionSubmodulePermission>();
        var userPermissionOverrideRepo = unitOfWork.GetRepository<UserPermissionOverride>();

        var deptPerms = await departmentModulePermissionRepo.GetAllAsync(
            predicate: x => x.DepartmentId == employee.DepartmentId && x.IsGranted,
            include: x => x.Include(m => m.Module),
            disableTracking: true);

        var posPerms = await positionSubmodulePermissionRepo.GetAllAsync(
            predicate: x => x.PositionId == employee.PositionId && x.IsGranted,
            include: x => x.Include(s => s.SubModule).ThenInclude(sm => sm.Module)
                           .Include(s => s.Permission),
            disableTracking: true);

        var userOverrides = await userPermissionOverrideRepo.GetAllAsync(
            predicate: x => x.UserId == userId,
            include: x => x.Include(o => o.SubModule).ThenInclude(sm => sm.Module)
                           .Include(o => o.Permission),
            disableTracking: true);

        var allowedModules = deptPerms
            .Select(x => x.ModuleId)
            .ToHashSet();

        var tempResult = new Dictionary<Guid, PermissionAggregate>();

        foreach (var pos in posPerms)
        {
            if (!allowedModules.Contains(pos.SubModule.ModuleId))
            {
                continue;
            }

            if (!tempResult.TryGetValue(pos.SubModuleId, out var aggregate))
            {
                aggregate = new PermissionAggregate(pos.SubModule.Module.Code, pos.SubModule.Code, []);
                tempResult[pos.SubModuleId] = aggregate;
            }

            if (!aggregate.Permissions.Contains(pos.Permission.Code))
            {
                aggregate.Permissions.Add(pos.Permission.Code);
            }
        }

        foreach (var ovr in userOverrides)
        {
            if (!tempResult.TryGetValue(ovr.SubModuleId, out var aggregate))
            {
                if (!ovr.IsGranted)
                {
                    continue;
                }

                if (!allowedModules.Contains(ovr.SubModule.ModuleId))
                {
                    continue;
                }

                aggregate = new PermissionAggregate(ovr.SubModule.Module.Code, ovr.SubModule.Code, []);
                tempResult[ovr.SubModuleId] = aggregate;
            }

            if (ovr.IsGranted)
            {
                if (!aggregate.Permissions.Contains(ovr.Permission.Code))
                {
                    aggregate.Permissions.Add(ovr.Permission.Code);
                }
            }
            else
            {
                aggregate.Permissions.Remove(ovr.Permission.Code);
            }
        }

        return tempResult.Values
            .SelectMany(x => x.Permissions.Select(p => $"{x.ModuleCode.ToLower()}.{x.SubModuleCode.ToLower()}.{p.ToString().ToLower()}"))
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }

    private sealed record PermissionAggregate(string ModuleCode, string SubModuleCode, List<PermissionCode> Permissions);
}