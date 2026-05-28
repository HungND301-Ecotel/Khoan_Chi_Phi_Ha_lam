using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record GetAcceptanceReportByIdQuery(Guid Id) : IRequest<GetAcceptanceReportDetailDto>;

public class GetAcceptanceReportByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAcceptanceReportByIdQuery, GetAcceptanceReportDetailDto>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<GetAcceptanceReportDetailDto> Handle(GetAcceptanceReportByIdQuery request, CancellationToken cancellationToken)
    {
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: q => q
                .Include(a => a.ProductionOutput)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.ProcessGroup).ThenInclude(pg => pg.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Equipment).ThenInclude(e => e.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.ProductionOrder).ThenInclude(po => po.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.CategoryAllocations).ThenInclude(c => c.ProcessGroup).ThenInclude(pg => pg.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.CategoryAllocations).ThenInclude(c => c.Equipments)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Material).ThenInclude(m => m.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Material).ThenInclude(m => m.Costs)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Part).ThenInclude(p => p.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Part).ThenInclude(p => p.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Part).ThenInclude(p => p.Costs)
                .Include(a => a.AcceptanceReportItems).ThenInclude(a => a.IssuedDetails)
                .Include(a => a.AcceptanceReportItems).ThenInclude(a => a.ShippedDetails)
                .Include(a => a.AcceptanceReportItems).ThenInclude(a => a.QuotaBasedMaterialQuantities),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var productionOutput = acceptanceReport.ProductionOutput;

        var items = acceptanceReport.AcceptanceReportItems
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedOn)
            .Select(item => new AcceptanceReportDetailItemDto
        {
            Id = item.Id,
            AcceptanceReportId = item.AcceptanceReportId,
            CategoryProductionOrderId = item.ProductionOrderId,
            CategoryProductionOrderLabel = ResolveCategoryProductionOrderLabel(item),
            CategoryAssignmentCodeId = item.CategoryAssignmentCodeId,
            CategoryAssignmentCodeLabel = ResolveCategoryAssignmentCodeLabel(item),
            AdditionalCostProductionOrderId = item.AdditionalCostProductionOrderId,
            AdditionalCostAssignmentCodeId = item.AdditionalCostAssignmentCodeId,
            MaterialId = item.TrackedMaterialId,
            PartId = item.PartId,
            TrackedMaterialId = item.TrackedMaterialId,
            UsageTime = item.UsageTime,
            MaterialCode = ResolveTrackedMaterialCode(item),
            MaterialName = ResolveTrackedMaterialName(item),
            PartCode = ResolvePartCode(item),
            PartName = ResolvePartName(item),
            TrackedMaterialCode = ResolveTrackedMaterialCode(item),
            TrackedMaterialName = ResolveTrackedMaterialName(item),
            PartType = item.Part?.MaterialType == MaterialType.MaterialOutContract ? PartType.OtherPart : PartType.Part,
            UnitOfMeasureName = item.Material?.UnitOfMeasure?.Name
                              ?? item.Part?.UnitOfMeasure?.Name,
            Type = item.IsMaterialItem ? AcceptanceReportItemType.Material : AcceptanceReportItemType.Part,
            MaterialsIncludedInContractRevenueType = item.MaterialsIncludedInContractRevenueType,
            MaterialsIncludedInContractRevenue = item.MaterialsIncludedInContractRevenue,
            IsLongTermTracking = item.IsLongTermTracking,
            ProcessGroupId = item.ProcessGroupId,
            ProcessGroupCode = item.ProcessGroup?.FixedKey?.Key,
            ProcessGroupName = item.ProcessGroup?.Name,
            MaterialsIncludedInContractRevenueQuantity = item.MaterialsIncludedInContractRevenueQuantity,
            CategoryAllocations = item.CategoryAllocations
                .Select(allocation => new AcceptanceReportCategoryAllocationDetailDto
                {
                    ProcessGroupId = allocation.ProcessGroupId,
                    ProcessGroupCode = allocation.ProcessGroup?.FixedKey?.Key,
                    ProcessGroupName = allocation.ProcessGroup?.Name,
                    Quantity = allocation.Quantity,
                    AssignmentCodeIds = allocation.AssignmentCodeIds.ToList(),
                })
                .ToList(),
            AdditionalCostClassification = item.AdditionalCostClassification,
            AdditionalCost = item.AdditionalCost,
            OtherMaterialDetail = item.OtherMaterialDetail,
            AdditionalCostQuantity = item.AdditionalCostQuantity,
            QuotaBasedMaterial = item.QuotaBasedMaterial,
            QuotaBasedMaterialType = item.QuotaBasedMaterialType,
            QuotaBasedMaterialQuantities = item.QuotaBasedMaterial != QuotaBasedMaterial.None
                ? item.QuotaBasedMaterialQuantities
                    .Select(q => new QuotaBasedMaterialQuantityDto { Type = q.Type, Quantity = q.Quantity })
                    .ToList()
                : null,
            Asset = item.Asset,
            AssetMaterialQuantity = item.AssetMaterialQuantity,
            IssuedQuantity = item.IssuedQuantity,
            ShippedQuantity = item.ShippedQuantity,
            IssuedDetails = item.IssuedDetails
                .Select(d => new IssuedDetailDto { Type = d.Type, Quantity = d.Quantity })
                .ToList(),
            ShippedDetails = item.ShippedDetails
                .Select(d => new ShippedDetailDto { Type = d.Type, Quantity = d.Quantity })
                .ToList(),
            PlanCost = GetPlanCost(item, productionOutput),
            ActualCost = GetActualCost(item, productionOutput),
            ItemType = item.ItemType,
        }).ToList();

        return new GetAcceptanceReportDetailDto
        {
            Id = acceptanceReport.Id,
            ProductionOutputId = acceptanceReport.ProductionOutputId,
            FilePath = acceptanceReport.FilePath,
            Items = items.ToList()
        };
    }

    private static decimal GetPlanCost(AcceptanceReportItem item, ProductionOutput productionOutput)
    {
        if (productionOutput == null)
        {
            return 0;
        }

        if (item.IsMaterialItem && item.Material?.Costs != null)
        {
            var matchingCost = item.Material.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Material &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            return matchingCost != null ? (decimal)matchingCost.Amount : 0;
        }

        if (item.IsTrackedSctxItem && item.Part?.Costs != null)
        {
            var matchingCost = item.Part.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Part &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            return matchingCost != null ? (decimal)matchingCost.Amount : 0;
        }

        return 0;
    }

    private static decimal GetActualCost(AcceptanceReportItem item, ProductionOutput productionOutput)
    {
        if (productionOutput == null)
        {
            return 0;
        }

        if (item.IsMaterialItem && item.Material?.Costs != null)
        {
            var matchingCost = item.Material.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Material &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            return matchingCost != null ? (decimal)matchingCost.ActualAmount : 0;
        }

        if (item.IsTrackedSctxItem && item.Part?.Costs != null)
        {
            var matchingCost = item.Part.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Part &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            return matchingCost != null ? (decimal)matchingCost.ActualAmount : 0;
        }

        return 0;
    }

    private static string? ResolveTrackedMaterialCode(AcceptanceReportItem item)
        => item.IsTrackedSctxItem
            ? item.Part?.Code?.Value ?? item.Material?.Code?.Value
            : item.Material?.Code?.Value ?? item.Part?.Code?.Value;

    private static string? ResolveTrackedMaterialName(AcceptanceReportItem item)
        => item.IsTrackedSctxItem
            ? item.Part?.Name ?? item.Material?.Name
            : item.Material?.Name ?? item.Part?.Name;

    private static string? ResolvePartCode(AcceptanceReportItem item)
        => item.Part?.Code?.Value ?? item.Material?.Code?.Value;

    private static string? ResolvePartName(AcceptanceReportItem item)
        => item.Part?.Name ?? item.Material?.Name;

    private static string ResolveCategoryAssignmentCodeLabel(AcceptanceReportItem item)
        => item.Equipment == null
            ? AcceptanceReportDetailItemDto.NoneCategoryAssignmentCodeLabel
            : $"[Nhóm vật tư, tài sản] {item.Equipment.Code?.Value} - {item.Equipment.Name}";

    private static string ResolveCategoryProductionOrderLabel(AcceptanceReportItem item)
        => item.ProductionOrder == null
            ? AcceptanceReportDetailItemDto.NoneCategoryProductionOrderLabel
            : $"[Lệnh sản xuất] {item.ProductionOrder.Code?.Value} - {item.ProductionOrder.Name}";
}
