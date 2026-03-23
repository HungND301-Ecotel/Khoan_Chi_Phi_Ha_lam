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
                .Include(a => a.AcceptanceReportItems).ThenInclude(m => m.Part).ThenInclude(m => m.Code)
                .Include(a => a.AcceptanceReportItems).ThenInclude(m => m.Part).ThenInclude(m => m.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems).ThenInclude(m => m.Part).ThenInclude(m => m.Costs)
                .Include(a => a.AcceptanceReportItems).ThenInclude(a => a.IssuedDetails)
                .Include(a => a.AcceptanceReportItems).ThenInclude(a => a.ShippedDetails),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var productionOutput = acceptanceReport.ProductionOutput;

        var items = acceptanceReport.AcceptanceReportItems.Select(item => new AcceptanceReportDetailItemDto
        {
            Id = item.Id,
            AcceptanceReportId = item.AcceptanceReportId,
            ProductionOrderId = item.ProductionOrderId,
            MaterialId = item.MaterialId,
            MaintainUnitPriceEquipmentId = item.PartId,
            MaterialCode = item.Material?.Code?.Value,
            MaterialName = item.Material?.Name,
            PartCode = item?.Part?.Code?.Value,
            PartName = item?.Part?.Name,
            UnitOfMeasureName = item.Material?.UnitOfMeasure?.Name ?? item?.Part?.UnitOfMeasure?.Name,
            Type = item.MaterialId.HasValue ? AcceptanceReportItemType.Material : AcceptanceReportItemType.Part,
            MaterialsIncludedInContractRevenue = item.MaterialsIncludedInContractRevenue,
            ProcessGroupId = item.ProcessGroupId,
            ProcessGroupCode = item.ProcessGroup?.Code?.Value,
            ProcessGroupName = item.ProcessGroup?.Name,
            MaterialsIncludedInContractRevenueQuantity = item.MaterialsIncludedInContractRevenueQuantity,
            AdditionalCost = item.AdditionalCost,
            AdditionalCostQuantity = item.AdditionalCostQuantity,
            QuotaBasedMaterial = item.QuotaBasedMaterial,
            QuotaBasedMaterialType = item.QuotaBasedMaterialType,
            QuotaBasedMaterialQuantity = item.QuotaBasedMaterialQuantity,
            Asset = item.Asset,
            AssetMaterialQuantity = item.AssetMaterialQuantity,
            IssuedQuantity = item.IssuedQuantity,
            ShippedQuantity = item.ShippedQuantity,
            PlanCost = GetPlanCost(item, productionOutput),
            ActualCost = GetActualCost(item, productionOutput),
            ItemType = item.ItemType,
        }).ToList();

        return new GetAcceptanceReportDetailDto
        {
            Id = acceptanceReport.Id,
            ProductionOutputId = acceptanceReport.ProductionOutputId,
            FilePath = acceptanceReport.FilePath,
            Items = items.OrderBy(i => i.MaterialCode).ThenBy(i => i.PartCode).ToList()
        };
    }

    private decimal GetPlanCost(AcceptanceReportItem item, ProductionOutput productionOutput)
    {
        if (productionOutput == null)
        {
            return 0;
        }

        decimal planCost = 0;

        if (item.MaterialId.HasValue && item.Material?.Costs != null)
        {
            // Get cost for Material that matches the time period
            var matchingCost = item.Material.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Material &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            if (matchingCost != null)
            {
                planCost = (decimal)matchingCost.Amount;
            }
        }
        else if (item.PartId.HasValue &&
                 item?.Part?.Costs != null)
        {
            // Get cost for Part that matches the time period
            var matchingCost = item.Part.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Part &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            if (matchingCost != null)
            {
                planCost = (decimal)matchingCost.Amount;
            }
        }

        return planCost;
    }

    private decimal GetActualCost(AcceptanceReportItem item, ProductionOutput productionOutput)
    {
        if (productionOutput == null)
        {
            return 0;
        }

        decimal acutalCost = 0;

        if (item.MaterialId.HasValue && item.Material?.Costs != null)
        {
            // Get cost for Material that matches the time period
            var matchingCost = item.Material.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Material &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            if (matchingCost != null)
            {
                acutalCost = (decimal)matchingCost.ActualAmount;
            }
        }
        else if (item.PartId.HasValue &&
                 item?.Part?.Costs != null)
        {
            // Get cost for Part that matches the time period
            var matchingCost = item.Part.Costs.FirstOrDefault(c =>
                c.CostType == CostType.Part &&
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            if (matchingCost != null)
            {
                //acutalCost = (decimal)matchingCost.Amount;
                acutalCost = 0;
            }
        }

        return acutalCost;
    }
}
