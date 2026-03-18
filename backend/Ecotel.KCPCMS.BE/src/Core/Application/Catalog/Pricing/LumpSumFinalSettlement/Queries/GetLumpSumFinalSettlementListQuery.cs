using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Domain.Common.Enums;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

public record GetLumpSumFinalSettlementListQuery(string Month, string Year, string ProcessGroupId) : IRequest<List<LumpSumFinalSettlementDto>>;

public class GetLumpSumFinalSettlementListQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLumpSumFinalSettlementListQuery, List<LumpSumFinalSettlementDto>>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();

    public async Task<List<LumpSumFinalSettlementDto>> Handle(GetLumpSumFinalSettlementListQuery request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Month, out var month) || !int.TryParse(request.Year, out var year))
        {
            throw new BadRequestException("Invalid month or year");
        }

        var hasProcessGroupFilter = Guid.TryParse(request.ProcessGroupId, out var processGroupId);

        var productionOutputs = await _productionOutputRepository.GetAllAsync(
            predicate: po => po.StartMonth.Month == month
                && po.StartMonth.Year == year
                && po.AcceptanceReport != null,
            include: q => q.AsSplitQuery()
                .Include(po => po.AcceptanceReport)
                .Include(po => po.ProductionOutputProcessGroups)
                    .ThenInclude(pg => pg.ProductionOutputProducts),
            disableTracking: true);

        var actualByProduct = productionOutputs
            .SelectMany(po => po.ProductionOutputProcessGroups)
            .Where(pg => !hasProcessGroupFilter || pg.ProcessGroupId == processGroupId)
            .SelectMany(pg => pg.ProductionOutputProducts)
            .GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ProductionMeters));

        // Query ProductUnitPrice by ProcessGroup
        var productUnitPrices = await _productUnitPriceRepository.GetAllAsync(
            predicate: p => !hasProcessGroupFilter || p.Product!.ProcessGroupId == processGroupId,
            include: p => p.AsSplitQuery()
                .Include(p => p.Product).ThenInclude(pr => pr!.Code)
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedMaterialCost)
                        .ThenInclude(pmc => pmc!.MaterialUnitPrice)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedMaterialCost)
                        .ThenInclude(pmc => pmc!.SlideUnitPriceAssignmentCode)
                                .ThenInclude(mupac => mupac.Material)
                                    .ThenInclude(m => m!.Costs)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedMaterialCost)
                        .ThenInclude(pmc => pmc!.StoneClampRatio)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedMaintainCost)
                        .ThenInclude(pmc => pmc!.PlannedMaintainCostAdjustmentFactors)
                            .ThenInclude(pmcaf => pmcaf.MaintainUnitPrice)
                                .ThenInclude(mup => mup!.MaintainUnitPriceEquipments)
                                    .ThenInclude(mupe => mupe.Part)
                                        .ThenInclude(p => p!.Costs)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedMaintainCost)
                        .ThenInclude(pmc => pmc!.PlannedMaintainCostAdjustmentFactors)
                            .ThenInclude(pmcaf => pmcaf.PlannedMaintainCostAdjustmentFactorDescriptions)
                                .ThenInclude(pmcafd => pmcafd.AdjustmentFactorDescription)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedElectricityCost)
                        .ThenInclude(pec => pec!.PlannedElectricityCostAdjustmentFactors)
                            .ThenInclude(pecaf => pecaf.ElectricityUnitPriceEquipment)
                                .ThenInclude(euep => euep!.Equipment)
                                    .ThenInclude(e => e!.Costs)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedElectricityCost)
                        .ThenInclude(pec => pec!.PlannedElectricityCostAdjustmentFactors)
                            .ThenInclude(pecaf => pecaf.PlannedElectricityCostAdjustmentFactorDescriptions)
                                .ThenInclude(pecafd => pecafd.AdjustmentFactorDescription),
            disableTracking: true);

        var result = new List<LumpSumFinalSettlementDto>();

        foreach (var productUnitPrice in productUnitPrices)
        {
            // Filter outputs by month and year (planned data source)
            var filteredOutputs = productUnitPrice.Outputs
                .Where(o => o.OutputType == OutputType.PlanOutput && o.StartMonth.Month == month && o.StartMonth.Year == year)
                .ToList();

            if (!filteredOutputs.Any())
            {
                continue;
            }

            // Calculate totals
            var plannedQuantity = filteredOutputs
                .Sum(o => o.ProductionMeters);

            var actualQuantity = productUnitPrice.ProductId != Guid.Empty && actualByProduct.TryGetValue(productUnitPrice.ProductId, out var productActual)
                ? productActual
                : 0;

            // Calculate material costs
            var materialUnitPrice = 0.0;
            var materialTotalAmount = 0.0;

            var plannedMaterialCosts = filteredOutputs
                .Where(o => o.PlannedMaterialCost != null)
                .Select(o => o.PlannedMaterialCost!)
                .ToList();

            if (plannedMaterialCosts.Any())
            {
                // Use the first planned material cost as unit price
                materialUnitPrice = plannedMaterialCosts.First().GetTotalPrice();
                materialTotalAmount = Math.Round(materialUnitPrice * actualQuantity, 3);
            }

            // Calculate maintain costs
            var maintainUnitPrice = 0.0;
            var maintainTotalAmount = 0.0;

            var plannedMaintainCosts = filteredOutputs
                .Where(o => o.PlannedMaintainCost != null)
                .Select(o => o.PlannedMaintainCost!)
                .ToList();

            if (plannedMaintainCosts.Any())
            {
                maintainUnitPrice = plannedMaintainCosts.Sum(p => p.GetPlannedTotalPrice());
                maintainTotalAmount = maintainUnitPrice * actualQuantity;
            }

            // Calculate electricity costs
            var electricityUnitPrice = 0.0;
            var electricityTotalAmount = 0.0;

            var plannedElectricityCosts = filteredOutputs
                .Where(o => o.PlannedElectricityCost != null)
                .Select(o => o.PlannedElectricityCost!)
                .ToList();

            if (plannedElectricityCosts.Any())
            {
                electricityUnitPrice = plannedElectricityCosts.Sum(p => p.GetPlannedTotalPrice());
                electricityTotalAmount = electricityUnitPrice * actualQuantity;
            }

            var totalAmount = materialTotalAmount + maintainTotalAmount + electricityTotalAmount;

            result.Add(new LumpSumFinalSettlementDto
            {
                Id = productUnitPrice.Id,
                ProductName = productUnitPrice.Product?.Name ?? string.Empty,
                ProductCode = productUnitPrice.Product?.Code?.Value ?? string.Empty,
                UnitOfMeasureId = productUnitPrice.UnitOfMeasureId ?? Guid.Empty,
                UnitOfMeasureName = productUnitPrice.UnitOfMeasure?.Name ?? string.Empty,
                PlannedQuantity = plannedQuantity,
                ActualQuantity = actualQuantity,
                Materials = new()
                {
                    UnitPrice = materialUnitPrice,
                    TotalAmount = materialTotalAmount
                },
                Maintains = new()
                {
                    UnitPrice = maintainUnitPrice,
                    TotalAmount = maintainTotalAmount
                },
                Electricities = new()
                {
                    UnitPrice = electricityUnitPrice,
                    TotalAmount = electricityTotalAmount
                },
                TotalAmount = totalAmount
            });
        }

        return result;
    }
}
