using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Domain.Entities.Identity;
using Shared.Authorization;

namespace Application.Catalog.Permissions;

public class PermissionCatalogSynchronizer(
    IUnitOfWork unitOfWork,
    IPermissionDefinitionScanner scanner)
    : IPermissionCatalogSynchronizer
{
    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        // Mã nào được [HasPermission] kiểm tra mà không khai báo trong AppPermissions thì không ai được gán → dừng sớm.
        var undeclaredCodes = scanner.ScanEnforcedCodes()
            .Where(code => !PermissionRegistry.Contains(code))
            .ToList();

        if (undeclaredCodes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Các mã quyền sau chưa được khai báo trong AppPermissions: {string.Join(", ", undeclaredCodes)}");
        }

        var definitions = PermissionRegistry.All;

        var moduleRepo = unitOfWork.GetRepository<Module>();
        var subModuleRepo = unitOfWork.GetRepository<SubModule>();

        var existingModules = await moduleRepo.GetAllAsync(disableTracking: false);
        var existingSubModules = await subModuleRepo.GetAllAsync(disableTracking: false);

        var moduleMap = existingModules.ToDictionary(x => x.Code, x => x);
        var subModuleMap = existingSubModules.ToDictionary(x => (x.ModuleId, x.Code), x => x);

        var hasChanges = false;

        var activeModuleCodes = definitions.Select(x => x.ModuleCode).ToHashSet();
        var activeSubModuleKeys = new HashSet<(Guid ModuleId, string SubModuleCode)>();

        foreach (var group in definitions.GroupBy(x => x.ModuleCode))
        {
            var moduleName = group.First().ModuleName;

            if (!moduleMap.TryGetValue(group.Key, out var module))
            {
                module = Module.Create(moduleName, group.Key, null, moduleMap.Count);
                await moduleRepo.InsertAsync(module);
                moduleMap[group.Key] = module;
                hasChanges = true;
            }
            else if (module.Name != moduleName)
            {
                module.SetName(moduleName);
                hasChanges = true;
            }

            foreach (var resource in group.GroupBy(x => x.ResourceCode))
            {
                var resourceName = resource.First().ResourceName;
                var subModuleKey = (module.Id, resource.Key);
                activeSubModuleKeys.Add(subModuleKey);

                if (!subModuleMap.TryGetValue(subModuleKey, out var subModule))
                {
                    subModule = SubModule.Create(
                        module.Id,
                        resourceName,
                        resource.Key,
                        string.Empty,
                        subModuleMap.Count);

                    await subModuleRepo.InsertAsync(subModule);
                    subModuleMap[subModuleKey] = subModule;
                    hasChanges = true;
                }
                else if (subModule.Name != resourceName)
                {
                    subModule.SetName(resourceName);
                    hasChanges = true;
                }
            }
        }

        // Cleanup obsolete SubModules
        foreach (var existingSubModule in existingSubModules)
        {
            var key = (existingSubModule.ModuleId, existingSubModule.Code);
            if (!activeSubModuleKeys.Contains(key))
            {
                subModuleRepo.Delete(existingSubModule);
                hasChanges = true;
            }
        }

        // Cleanup obsolete Modules
        foreach (var existingModule in existingModules)
        {
            if (!activeModuleCodes.Contains(existingModule.Code))
            {
                moduleRepo.Delete(existingModule);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }
}
