using Application.Common.Exceptions;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Commands;

internal static class AcceptanceReportCommandItemHelper
{
    internal static IList<(Guid ProcessGroupId, double Quantity, IList<Guid> AssignmentCodeIds)>? MapCategoryAllocations(
        List<AcceptanceReportCategoryAllocationDto>? dtos)
        => dtos?.Select(x => (
            x.ProcessGroupId,
            x.Quantity,
            (IList<Guid>)(x.AssignmentCodeIds.Any() ? x.AssignmentCodeIds.ToList() : x.EquipmentIds.ToList())))
            .ToList();

    internal static (Guid? MaterialId, Guid? PartId) ResolveTrackedItemIds(
        AcceptanceReportItemType itemType,
        Guid? trackedMaterialId,
        Guid? materialId,
        Guid? partId,
        IList<Material> allMaterials,
        IList<Part> allParts)
    {
        if (IsMaterialItem(itemType))
        {
            var materialInputId = materialId ?? trackedMaterialId;
            if (!materialInputId.HasValue)
            {
                throw new NotFoundException("MaterialId is required for material item");
            }

            if (allMaterials.All(m => m.Id != materialInputId.Value))
            {
                throw new NotFoundException($"Material with Id '{materialInputId.Value}' not found");
            }

            return (materialInputId.Value, null);
        }

        if (IsTrackedSctxItem(itemType))
        {
            var partInputId = partId ?? trackedMaterialId;
            if (!partInputId.HasValue)
            {
                throw new NotFoundException("PartId is required for SCTX item");
            }

            if (allParts.All(p => p.Id != partInputId.Value))
            {
                throw new NotFoundException($"Part with Id '{partInputId.Value}' not found");
            }

            return (null, partInputId.Value);
        }

        return (null, null);
    }

    internal static void ValidateProcessGroupIds(
        Guid? processGroupId,
        IList<(Guid ProcessGroupId, double Quantity, IList<Guid> AssignmentCodeIds)>? categoryAllocations,
        HashSet<Guid> processGroupIdsInPeriod)
    {
        var processGroupIdsToValidate = categoryAllocations != null && categoryAllocations.Any()
            ? categoryAllocations.Select(x => x.ProcessGroupId)
            : processGroupId.HasValue
                ? new[] { processGroupId.Value }
                : [];

        if (!processGroupIdsToValidate.Any() || processGroupIdsToValidate.Any(id => !processGroupIdsInPeriod.Contains(id)))
        {
            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
        }
    }

    internal static bool IsMaterialItem(AcceptanceReportItemType itemType)
        => itemType == AcceptanceReportItemType.Material;

    internal static bool IsTrackedSctxItem(AcceptanceReportItemType itemType)
        => itemType == AcceptanceReportItemType.Part;

    internal static bool RequiresProcessGroupValidation(
        AcceptanceReportItemType itemType,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue)
        => IsTrackedSctxItem(itemType)
            && materialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None;
}
