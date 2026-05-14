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
            CategoryEquipmentId = item.EquipmentId,
            AdditionalCostProductionOrderId = item.AdditionalCostProductionOrderId,
            AdditionalCostEquipmentId = item.AdditionalCostEquipmentId,
            MaterialId = item.MaterialId,
            PartId = item.PartId,
            UsageTime = item.UsageTime,
            MaterialCode = item.Material?.Code?.Value,
            MaterialName = item.Material?.Name,
            PartCode = item.Part?.Code?.Value,
            PartName = item.Part?.Name,
            PartType = item.Part?.Type,
            UnitOfMeasureName = item.Material?.UnitOfMeasure?.Name
                              ?? item.Part?.UnitOfMeasure?.Name,
            Type = item.MaterialId.HasValue ? AcceptanceReportItemType.Material : AcceptanceReportItemType.Part,
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
                    EquipmentIds = allocation.Equipments
                        .Select(equipment => equipment.EquipmentId)
                        .ToList(),
                })
                .ToList(),
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

        if (item.MaterialId.HasValue && item.Material?.Costs != null)
        {
            var matchingCost = item.Material.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Material &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            return matchingCost != null ? (decimal)matchingCost.Amount : 0;
        }

        if (item.PartId.HasValue && item.Part?.Costs != null)
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

        if (item.MaterialId.HasValue && item.Material?.Costs != null)
        {
            var matchingCost = item.Material.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Material &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            return matchingCost != null ? (decimal)matchingCost.ActualAmount : 0;
        }

        if (item.PartId.HasValue && item.Part?.Costs != null)
        {
            var matchingCost = item.Part.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Part &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            return matchingCost != null ? (decimal)matchingCost.ActualAmount : 0;
        }

        return 0;
    }
}
