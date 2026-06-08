using Application.Common.Exceptions;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Commands;

internal static class AcceptanceReportCommandItemHelper
{
    internal static MaterialsIncludedInContractRevenue ResolveMaterialsIncludedInContractRevenue(
        AcceptanceReportItemType? materialsIncludedInContractRevenueType,
        MaterialsIncludedInContractRevenue legacyValue)
        => materialsIncludedInContractRevenueType switch
        {
            AcceptanceReportItemType.Material => MaterialsIncludedInContractRevenue.Material,
            AcceptanceReportItemType.Part => MaterialsIncludedInContractRevenue.Maintain,
            _ => legacyValue
        };

    internal static AdditionalCost ResolveAdditionalCost(
        AdditionalCost? additionalCostClassification,
        AdditionalCost legacyValue)
        => additionalCostClassification ?? legacyValue;

    internal static AcceptanceReportItemType ResolveTrackedItemType(
        AcceptanceReportItemType fallbackType,
        AcceptanceReportItemType? materialsIncludedInContractRevenueType,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue,
        AdditionalCost additionalCost)
    {
        if (materialsIncludedInContractRevenueType.HasValue)
        {
            return materialsIncludedInContractRevenueType.Value;
        }

        if (materialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Material)
        {
            return AcceptanceReportItemType.Material;
        }

        if (materialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain)
        {
            return AcceptanceReportItemType.Part;
        }

        return additionalCost switch
        {
            AdditionalCost.Material => AcceptanceReportItemType.Material,
            AdditionalCost.Maintain => AcceptanceReportItemType.Part,
            AdditionalCost.SafeAndWelfare => AcceptanceReportItemType.Material,
            _ => fallbackType
        };
    }

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
        IList<Material> allMaterials)
    {
        if (IsMaterialItem(itemType))
        {
            var trackedMaterialInputId = trackedMaterialId ?? materialId;
            if (!trackedMaterialInputId.HasValue)
            {
                throw new NotFoundException("TrackedMaterialId is required for material item");
            }

            if (allMaterials.All(m => m.Id != trackedMaterialInputId.Value))
            {
                throw new NotFoundException($"Material with Id '{trackedMaterialInputId.Value}' not found");
            }

            return (trackedMaterialInputId.Value, null);
        }

        if (IsTrackedSctxItem(itemType))
        {
            var trackedMaterialInputId = trackedMaterialId ?? partId;
            if (!trackedMaterialInputId.HasValue)
            {
                throw new NotFoundException("TrackedMaterialId is required for SCTX item");
            }

            if (allMaterials.All(m => m.Id != trackedMaterialInputId.Value))
            {
                throw new NotFoundException($"Tracked material with Id '{trackedMaterialInputId.Value}' not found");
            }

            return (null, trackedMaterialInputId.Value);
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

        if (!processGroupIdsToValidate.Any())
        {
            return;
        }

        if (processGroupIdsToValidate.Any(id => !processGroupIdsInPeriod.Contains(id)))
        {
            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
        }
    }

    internal static bool IsMaterialItem(AcceptanceReportItemType itemType)
        => itemType == AcceptanceReportItemType.Material;

    internal static bool IsTrackedSctxItem(AcceptanceReportItemType itemType)
        => itemType == AcceptanceReportItemType.Part;

    internal static bool ShouldValidateProcessGroup(
        AcceptanceReportItemType itemType,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue,
        Guid? processGroupId,
        IList<(Guid ProcessGroupId, double Quantity, IList<Guid> AssignmentCodeIds)>? categoryAllocations)
        => IsTrackedSctxItem(itemType)
            && materialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None
            && (processGroupId.HasValue || (categoryAllocations?.Any() ?? false));
}
