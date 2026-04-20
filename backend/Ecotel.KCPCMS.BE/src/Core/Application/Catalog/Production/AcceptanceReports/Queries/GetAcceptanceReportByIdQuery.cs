using Application.Catalog.MasterData.FixedKeys;
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
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Material).ThenInclude(m => m.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Material).ThenInclude(m => m.Costs)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Part).ThenInclude(p => p.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Part).ThenInclude(p => p.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.Part).ThenInclude(p => p.Costs)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.MaintainUnitPriceEquipment).ThenInclude(m => m.Part).ThenInclude(p => p.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.MaintainUnitPriceEquipment).ThenInclude(m => m.Part).ThenInclude(p => p.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.MaintainUnitPriceEquipment).ThenInclude(m => m.Part).ThenInclude(p => p.Costs)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.MaterialsIncludedInContractRevenueFixedKey)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.AdditionalCostFixedKey)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.OtherMaterialDetailFixedKey)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.QuotaBasedMaterialFixedKey)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.QuotaBasedMaterialTypeFixedKey)
                .Include(a => a.AcceptanceReportItems).ThenInclude(i => i.AssetFixedKey)
                .Include(a => a.AcceptanceReportItems).ThenInclude(a => a.IssuedDetails)
                    .ThenInclude(d => d.FixedKey)
                .Include(a => a.AcceptanceReportItems).ThenInclude(a => a.ShippedDetails)
                    .ThenInclude(d => d.FixedKey)
                .Include(a => a.AcceptanceReportItems).ThenInclude(a => a.QuotaBasedMaterialQuantities)
                    .ThenInclude(d => d.FixedKey),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var productionOutput = acceptanceReport.ProductionOutput;

        var items = acceptanceReport.AcceptanceReportItems.Select(item =>
        {
            var materialsIncludedInContractRevenue = FixedKeyCodeMapper.ResolveMaterialsIncludedInContractRevenue(
                item.MaterialsIncludedInContractRevenueFixedKey);
            var additionalCost = FixedKeyCodeMapper.ResolveAdditionalCost(item.AdditionalCostFixedKey);
            var otherMaterialDetail = FixedKeyCodeMapper.ResolveOtherMaterialDetail(item.OtherMaterialDetailFixedKey);
            var quotaBasedMaterial = FixedKeyCodeMapper.ResolveQuotaBasedMaterial(item.QuotaBasedMaterialFixedKey);
            QuotaBasedMaterialType? quotaBasedMaterialType = item.QuotaBasedMaterialTypeFixedKey != null
                ? FixedKeyCodeMapper.ResolveQuotaBasedMaterialType(item.QuotaBasedMaterialTypeFixedKey)
                : item.QuotaBasedMaterialQuantities.Select(q => q.FixedKey).FirstOrDefault(q => q != null) is { } quantityFixedKey
                    ? FixedKeyCodeMapper.ResolveQuotaBasedMaterialType(quantityFixedKey)
                    : null;
            var asset = FixedKeyCodeMapper.ResolveAsset(item.AssetFixedKey);

            return new AcceptanceReportDetailItemDto
            {
                Id = item.Id,
                AcceptanceReportId = item.AcceptanceReportId,
                CategoryProductionOrderId = item.ProductionOrderId,
                CategoryEquipmentId = item.EquipmentId,
                AdditionalCostProductionOrderId = item.AdditionalCostProductionOrderId,
                AdditionalCostEquipmentId = item.AdditionalCostEquipmentId,
                MaterialId = item.MaterialId,
                PartId = item.PartId,
                MaintainUnitPriceEquipmentId = item.MaintainUnitPriceEquipmentId,
                MaterialCode = item.Material?.Code?.Value,
                MaterialName = item.Material?.Name,
                PartCode = item.Part?.Code?.Value ?? item.MaintainUnitPriceEquipment?.Part?.Code?.Value,
                PartName = item.Part?.Name ?? item.MaintainUnitPriceEquipment?.Part?.Name,
                PartType = item.Part?.Type ?? item.MaintainUnitPriceEquipment?.Part?.Type,
                UnitOfMeasureName = item.Material?.UnitOfMeasure?.Name
                                  ?? item.Part?.UnitOfMeasure?.Name
                                  ?? item.MaintainUnitPriceEquipment?.Part?.UnitOfMeasure?.Name,
                Type = item.MaterialId.HasValue ? AcceptanceReportItemType.Material : AcceptanceReportItemType.Part,
                MaterialsIncludedInContractRevenue = materialsIncludedInContractRevenue,
                MaterialsIncludedInContractRevenueFixedKeyId = item.MaterialsIncludedInContractRevenueFixedKeyId,
                ProcessGroupId = item.ProcessGroupId,
                ProcessGroupCode = item.ProcessGroup?.Code?.Value,
                ProcessGroupName = item.ProcessGroup?.Name,
                MaterialsIncludedInContractRevenueQuantity = item.MaterialsIncludedInContractRevenueQuantity,
                AdditionalCost = additionalCost,
                AdditionalCostFixedKeyId = item.AdditionalCostFixedKeyId,
                OtherMaterialDetail = otherMaterialDetail,
                OtherMaterialDetailFixedKeyId = item.OtherMaterialDetailFixedKeyId,
                AdditionalCostQuantity = item.AdditionalCostQuantity,
                QuotaBasedMaterial = quotaBasedMaterial,
                QuotaBasedMaterialFixedKeyId = item.QuotaBasedMaterialFixedKeyId,
                QuotaBasedMaterialType = quotaBasedMaterialType,
                QuotaBasedMaterialTypeFixedKeyId = item.QuotaBasedMaterialTypeFixedKeyId,
                QuotaBasedMaterialQuantities = quotaBasedMaterial != QuotaBasedMaterial.None
                    ? item.QuotaBasedMaterialQuantities
                        .Select(q => new QuotaBasedMaterialQuantityDto
                        {
                            Type = FixedKeyCodeMapper.ResolveQuotaBasedMaterialType(q.FixedKey),
                            FixedKeyId = q.FixedKeyId,
                            Quantity = q.Quantity,
                        })
                        .ToList()
                    : null,
                Asset = asset,
                AssetFixedKeyId = item.AssetFixedKeyId,
                AssetMaterialQuantity = item.AssetMaterialQuantity,
                IssuedQuantity = item.IssuedQuantity,
                ShippedQuantity = item.ShippedQuantity,
                IssuedDetails = item.IssuedDetails
                    .Select(d => new IssuedDetailDto
                    {
                        Type = FixedKeyCodeMapper.ResolveIssuedQuantityType(d.FixedKey),
                        FixedKeyId = d.FixedKeyId,
                        Quantity = d.Quantity,
                    })
                    .ToList(),
                ShippedDetails = item.ShippedDetails
                    .Select(d => new ShippedDetailDto
                    {
                        Type = FixedKeyCodeMapper.ResolveShippedQuantityType(d.FixedKey),
                        FixedKeyId = d.FixedKeyId,
                        Quantity = d.Quantity,
                    })
                    .ToList(),
                PlanCost = GetPlanCost(item, productionOutput),
                ActualCost = GetActualCost(item, productionOutput),
                ItemType = item.ItemType,
            };
        }).ToList();

        return new GetAcceptanceReportDetailDto
        {
            Id = acceptanceReport.Id,
            ProductionOutputId = acceptanceReport.ProductionOutputId,
            FilePath = acceptanceReport.FilePath,
            Items = items.OrderBy(i => i.MaterialCode).ThenBy(i => i.PartCode).ToList()
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

        if (item.MaintainUnitPriceEquipmentId.HasValue && item.MaintainUnitPriceEquipment?.Part?.Costs != null)
        {
            var matchingCost = item.MaintainUnitPriceEquipment.Part.Costs.FirstOrDefault(c =>
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

        if (item.MaintainUnitPriceEquipmentId.HasValue && item.MaintainUnitPriceEquipment?.Part?.Costs != null)
        {
            var matchingCost = item.MaintainUnitPriceEquipment.Part.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Part &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            return matchingCost != null ? (decimal)matchingCost.ActualAmount : 0;
        }

        return 0;
    }
}
