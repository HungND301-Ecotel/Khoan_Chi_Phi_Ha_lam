using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Domain.Entities.Identity;

namespace Application.Catalog.Permissions;

public class PermissionCatalogSynchronizer(
    IUnitOfWork unitOfWork,
    IPermissionDefinitionScanner scanner)
    : IPermissionCatalogSynchronizer
{
    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var definitions = scanner.Scan();

        if (definitions.Count == 0)
        {
            return;
        }

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
            if (!moduleMap.TryGetValue(group.Key, out var module))
            {
                module = Module.Create(group.First().ModuleName, group.Key, null, moduleMap.Count);
                await moduleRepo.InsertAsync(module);
                moduleMap[group.Key] = module;
                hasChanges = true;
            }

            foreach (var definition in group)
            {
                var subModuleKey = (module.Id, definition.SubModuleCode);
                activeSubModuleKeys.Add(subModuleKey);

                if (!subModuleMap.ContainsKey(subModuleKey))
                {
                    var subModule = SubModule.Create(
                        module.Id,
                        definition.SubModuleName,
                        definition.SubModuleCode,
                        string.Empty,
                        subModuleMap.Count);

                    await subModuleRepo.InsertAsync(subModule);
                    subModuleMap[subModuleKey] = subModule;
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