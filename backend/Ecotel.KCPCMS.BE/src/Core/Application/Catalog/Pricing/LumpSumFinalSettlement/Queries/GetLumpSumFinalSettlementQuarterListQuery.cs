using Application.Catalog.Pricing.Common;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

public record GetLumpSumFinalSettlementQuarterListQuery(string Quarter, string Year, string ProcessGroupId) : IRequest<LumpSumFinalSettlementQuarterResponseDto>;

public class GetLumpSumFinalSettlementQuarterListQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLumpSumFinalSettlementQuarterListQuery, LumpSumFinalSettlementQuarterResponseDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();

    public async Task<LumpSumFinalSettlementQuarterResponseDto> Handle(GetLumpSumFinalSettlementQuarterListQuery request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Quarter, out var quarter) || !int.TryParse(request.Year, out var year))
        {
            throw new BadRequestException("Invalid quarter or year");
        }

        if (quarter < 1 || quarter > 4)
        {
            throw new BadRequestException("Quarter must be from 1 to 4");
        }

        var quarterStartMonth = (quarter - 1) * 3 + 1;
        var quarterEndMonth = quarterStartMonth + 2;
        var hasProcessGroupFilter = Guid.TryParse(request.ProcessGroupId, out var processGroupId);

        var productionOutputs = await _productionOutputRepository.GetAllAsync(
            predicate: po => po.StartMonth.Year == year
                && po.StartMonth.Month >= quarterStartMonth
                && po.StartMonth.Month <= quarterEndMonth
                && po.AcceptanceReport != null,
            include: q => q.AsSplitQuery()
                .Include(po => po.AcceptanceReport)
                .Include(po => po.ProductionOutputProcessGroups)
                    .ThenInclude(pg => pg.ProductionOutputProducts),
            disableTracking: true);

        var actualByProductMonth = productionOutputs
            .SelectMany(po => po.ProductionOutputProcessGroups.Select(pg => new
            {
                ProcessGroup = pg,
                Month = po.StartMonth.Month
            }))
            .Where(x => !hasProcessGroupFilter || x.ProcessGroup.ProcessGroupId == processGroupId)
            .SelectMany(x => x.ProcessGroup.ProductionOutputProducts.Select(p => new
            {
                ProcessGroupId = x.ProcessGroup.ProcessGroupId,
                p.ProductId,
                x.Month,
                p.ProductionMeters
            }))
            .GroupBy(x => new { x.ProcessGroupId, x.ProductId, x.Month })
            .ToDictionary(g => (g.Key.ProcessGroupId, g.Key.ProductId, g.Key.Month), g => g.Sum(x => x.ProductionMeters));

        var productUnitPrices = await _productUnitPriceRepository.GetAllAsync(
            predicate: p => p.ScenarioType == ProductUnitPriceScenarioType.Plan
                && (!hasProcessGroupFilter || p.Product!.ProcessGroupId == processGroupId),
            include: p => p.AsSplitQuery()
                .Include(p => p.Product).ThenInclude(pr => pr!.Code)
                .Include(p => p.Product).ThenInclude(pr => pr!.ProcessGroup)
                .Include(p => p.Product).ThenInclude(pr => pr!.ProcessGroup).ThenInclude(pr => pr!.Code)
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedMaterialCost)
                        .ThenInclude(pmc => pmc!.MaterialUnitPrice)
                            .ThenInclude(mup => mup.MaterialUnitPriceAssignmentCodes)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedMaterialCost)
                        .ThenInclude(pmc => pmc!.SlideUnitPriceAssignmentCode)
                            .ThenInclude(mupac => mupac.Material)
                                .ThenInclude(m => m!.Costs)
                .Include(p => p.Outputs)
                    .ThenInclude(o => o.PlannedMaterialCost)
                        .ThenInclude(pmc => pmc!.NormFactor)
                            .ThenInclude(nf => nf.NormFactorAssignmentCodes)
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

        var allQuarterPlannedMaterialCosts = productUnitPrices
            .SelectMany(p => p.Outputs)
            .Where(o => o.OutputType == OutputType.PlanOutput
                && o.StartMonth.Year == year
                && o.StartMonth.Month >= quarterStartMonth
                && o.StartMonth.Month <= quarterEndMonth)
            .Where(o => o.PlannedMaterialCost != null)
            .Select(o => o.PlannedMaterialCost!)
            .ToList();

        var currentTunnelMaterials = allQuarterPlannedMaterialCosts
            .Where(c => c.NormFactor?.TargetHardnessId.HasValue == true && c.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice)
            .Select(c => (TunnelExcavationMaterialUnitPrice)c.MaterialUnitPrice!)
            .ToList();

        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = new List<TunnelExcavationMaterialUnitPrice>();
        if (currentTunnelMaterials.Any())
        {
            var targetHardnessIds = allQuarterPlannedMaterialCosts
                .Where(c => c.NormFactor?.TargetHardnessId.HasValue == true)
                .Select(c => c.NormFactor!.TargetHardnessId!.Value)
                .Distinct()
                .ToList();
            var processIds = currentTunnelMaterials.Select(x => x.ProcessId).Distinct().ToList();
            var passportIds = currentTunnelMaterials.Select(x => x.PassportId).Distinct().ToList();
            var insertItemIds = currentTunnelMaterials.Select(x => x.InsertItemId).Distinct().ToList();
            var supportStepIds = currentTunnelMaterials.Select(x => x.SupportStepId).Distinct().ToList();

            tunnelMaterials = await _tunnelMaterialUnitPriceRepository.GetAll()
                .Where(x => targetHardnessIds.Contains(x.HardnessId)
                    && processIds.Contains(x.ProcessId)
                    && passportIds.Contains(x.PassportId)
                    && insertItemIds.Contains(x.InsertItemId)
                    && supportStepIds.Contains(x.SupportStepId))
                .Include(x => x.MaterialUnitPriceAssignmentCodes)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        var plannedMaterialUnitCostById = PlannedMaterialCostCalculator.CalculateUnitPricesByCostId(
            allQuarterPlannedMaterialCosts,
            tunnelMaterials);

        var groupedProductUnitPrices = productUnitPrices
            .GroupBy(p => new
            {
                Id = p.Product?.ProcessGroupId ?? Guid.Empty,
                Code = p.Product?.ProcessGroup?.Code?.Value ?? string.Empty,
                Name = p.Product?.ProcessGroup?.Name ?? string.Empty
            })
            .OrderBy(g => g.Key.Code)
            .ThenBy(g => g.Key.Name)
            .ToList();

        var result = new List<LumpSumFinalSettlementDto>();

        foreach (var processGroup in groupedProductUnitPrices)
        {
            foreach (var productUnitPrice in processGroup)
            {
                var filteredOutputs = productUnitPrice.Outputs
                    .Where(o => o.OutputType == OutputType.PlanOutput
                        && o.StartMonth.Year == year
                        && o.StartMonth.Month == quarterEndMonth)
                    .ToList();
                if (!filteredOutputs.Any())
                {
                    continue;
                }

                var plannedQuantity = filteredOutputs.Sum(o => o.ProductionMeters);

                var monthKey = (productUnitPrice.Product!.ProcessGroupId, productUnitPrice.ProductId, quarterEndMonth);
                var actualQuantity = productUnitPrice.ProductId != Guid.Empty && actualByProductMonth.TryGetValue(monthKey, out var productActual)
                    ? productActual
                    : 0;

                var materialUnitPrice = 0.0;
                var materialTotalAmount = 0.0;

                var plannedMaterialCosts = filteredOutputs
                    .Where(o => o.PlannedMaterialCost != null)
                    .Select(o => o.PlannedMaterialCost!)
                    .ToList();

                if (plannedMaterialCosts.Any())
                {
                    materialUnitPrice = plannedMaterialCosts.Sum(p => plannedMaterialUnitCostById.GetValueOrDefault(p.Id, 0));
                    materialTotalAmount = Math.Round(materialUnitPrice * actualQuantity, 3);
                }

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
                    ProcessGroupId = processGroup.Key.Id,
                    ProcessGroupCode = processGroup.Key.Code,
                    ProcessGroupName = processGroup.Key.Name,
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
        }

        var revenuesByMonth = new List<LumpSumQuarterRevenueByMonthDto>();
        for (var currentMonth = quarterStartMonth; currentMonth <= quarterEndMonth; currentMonth++)
        {
            var monthMaterialTotal = 0.0;
            var monthMaintainTotal = 0.0;
            var monthElectricityTotal = 0.0;

            foreach (var processGroup in groupedProductUnitPrices)
            {
                foreach (var productUnitPrice in processGroup)
                {
                    var monthOutputs = productUnitPrice.Outputs
                        .Where(o => o.OutputType == OutputType.PlanOutput
                            && o.StartMonth.Year == year
                            && o.StartMonth.Month == currentMonth)
                        .ToList();

                    if (!monthOutputs.Any())
                    {
                        continue;
                    }

                    var monthKey = (processGroup.Key.Id, productUnitPrice.ProductId, currentMonth);
                    var monthActualQuantity = actualByProductMonth.TryGetValue(monthKey, out var value)
                        ? value
                        : 0;

                    if (monthActualQuantity <= 0)
                    {
                        continue;
                    }

                    var materialUnitPrice = monthOutputs
                        .Where(o => o.PlannedMaterialCost != null)
                        .Select(o => plannedMaterialUnitCostById.GetValueOrDefault(o.PlannedMaterialCost!.Id, 0))
                        .Sum();

                    var maintainUnitPrice = monthOutputs
                        .Where(o => o.PlannedMaintainCost != null)
                        .Select(o => o.PlannedMaintainCost!.GetPlannedTotalPrice())
                        .Sum();

                    var electricityUnitPrice = monthOutputs
                        .Where(o => o.PlannedElectricityCost != null)
                        .Select(o => o.PlannedElectricityCost!.GetPlannedTotalPrice())
                        .Sum();

                    monthMaterialTotal += Math.Round(materialUnitPrice * monthActualQuantity, 3);
                    monthMaintainTotal += maintainUnitPrice * monthActualQuantity;
                    monthElectricityTotal += electricityUnitPrice * monthActualQuantity;
                }
            }

            revenuesByMonth.Add(new LumpSumQuarterRevenueByMonthDto
            {
                Month = currentMonth,
                Materials = new LumpSumCostDetailDto { TotalAmount = monthMaterialTotal },
                Maintains = new LumpSumCostDetailDto { TotalAmount = monthMaintainTotal },
                Electricities = new LumpSumCostDetailDto { TotalAmount = monthElectricityTotal },
                TotalAmount = monthMaterialTotal + monthMaintainTotal + monthElectricityTotal
            });
        }

        var quarterOutputsWithAcceptanceReport = await _productionOutputRepository.GetAllAsync(
            predicate: po => po.StartMonth.Year == year
                && po.StartMonth.Month >= quarterStartMonth
                && po.StartMonth.Month <= quarterEndMonth
                && po.AcceptanceReport != null,
            include: q => q.AsSplitQuery()
                .Include(po => po.ProductionOutputProcessGroups)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.AcceptanceReportItems)
                        .ThenInclude(i => i.Material)
                            .ThenInclude(m => m!.Costs)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.AcceptanceReportItems)
                        .ThenInclude(i => i.Part)
                            .ThenInclude(p => p!.Costs)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.AcceptanceReportItems)
                        .ThenInclude(i => i.ShippedDetails)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.AcceptanceReportItems)
                        .ThenInclude(i => i.AcceptanceReportItemLogs)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.ActualElectricityCost)
                        .ThenInclude(aec => aec!.ActualEletricityEquipment)
                            .ThenInclude(aee => aee.Equipment)
                                .ThenInclude(e => e!.Costs),
            disableTracking: true);
        var transferredCostsByMonth = new List<LumpSumQuarterTransferredCostDto>();
        for (var currentMonth = quarterStartMonth; currentMonth <= quarterEndMonth; currentMonth++)
        {
            var transferredMaterial = 0m;
            var transferredMaintain = 0m;
            var transferredElectricity = 0m;

            var monthOutputs = quarterOutputsWithAcceptanceReport
                .Where(po => po.StartMonth.Month == currentMonth)
                .ToList();

            foreach (var output in monthOutputs)
            {
                if (hasProcessGroupFilter
                    && !output.ProductionOutputProcessGroups.Any(pg => pg.ProcessGroupId == processGroupId))
                {
                    continue;
                }

                var report = output.AcceptanceReport;
                if (report == null)
                {
                    continue;
                }

                var sectionAItems = report.AcceptanceReportItems
                    .Where(i => i.MaterialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None)
                    .Where(i => !hasProcessGroupFilter || i.ProcessGroupId == processGroupId)
                    .ToList();

                foreach (var item in sectionAItems.Where(i => i.MaterialId.HasValue && i.Material != null))
                {
                    var unitPrice = GetPlannedUnitPrice(item.Material!.Costs, output.StartMonth);
                    var exportedToProductionQty = item.ShippedDetails
                        .Where(d => d.Type == ShippedQuantityType.XuatChoSanXuat)
                        .Sum(d => d.Quantity);
                    transferredMaterial += (decimal)exportedToProductionQty * unitPrice;
                }

                foreach (var item in sectionAItems.Where(i => i.PartId.HasValue && i.Part != null))
                {
                    var logsOfCurrentReport = item.AcceptanceReportItemLogs
                        .Where(l => l.AcceptanceReportId == report.Id);
                    transferredMaintain += logsOfCurrentReport.Sum(l => l.AccountedValueThisPeriod);
                }

                if (report.ActualElectricityCost != null)
                {
                    transferredElectricity += (decimal)report.ActualElectricityCost.ActualEletricityEquipment.Sum(equipment =>
                    {
                        var unitPrice = GetPlannedUnitPrice(
                            equipment.Equipment?.Costs ?? Array.Empty<Cost>(),
                            output.StartMonth);
                        return (double)unitPrice * equipment.ActualElectricityConsumption;
                    });
                }
            }

            var transferredMaterialDouble = (double)transferredMaterial;
            var transferredMaintainDouble = (double)transferredMaintain;
            var transferredElectricityDouble = (double)transferredElectricity;

            transferredCostsByMonth.Add(new LumpSumQuarterTransferredCostDto
            {
                Month = currentMonth,
                Materials = new LumpSumCostDetailDto { TotalAmount = transferredMaterialDouble },
                Maintains = new LumpSumCostDetailDto { TotalAmount = transferredMaintainDouble },
                Electricities = new LumpSumCostDetailDto { TotalAmount = transferredElectricityDouble },
                TotalAmount = transferredMaterialDouble + transferredMaintainDouble + transferredElectricityDouble
            });
        }

        var monthList = GetMonthListByQuarter(quarter);
        var customCosts = await _customCostRepository.GetAllAsync(
            predicate: x => monthList.Contains(x.Month)
                && x.Year == year
                && (!hasProcessGroupFilter || x.ProcessGroupId == processGroupId),
            disableTracking: true);

        return new LumpSumFinalSettlementQuarterResponseDto
        {
            Items = result,
            RevenuesByMonth = revenuesByMonth,
            TransferredCosts = transferredCostsByMonth,
            CustomCosts = customCosts
                .OrderBy(x => x.CreatedOn)
                .Select(x => new LumpSumQuarterCustomCostDto
                {
                    Id = x.Id,
                    Month = x.Month,
                    Year = x.Year,
                    ProcessGroupId = x.ProcessGroupId,
                    CustomName = x.CustomName,
                    ActualQuantity = x.ActualQuantity,
                    MaterialUnitPrice = x.MaterialUnitPrice,
                    MaintainUnitPrice = x.MaintainUnitPrice,
                    ElectricityUnitPrice = x.ElectricityUnitPrice
                })
                .ToList()
        };
    }

    private static decimal GetPlannedUnitPrice(IReadOnlyCollection<Cost> costs, DateOnly month)
    {
        var cost = costs.FirstOrDefault(c => c.StartMonth <= month && c.EndMonth >= month);
        return cost == null ? 0 : (decimal)cost.Amount;
    }

    public static List<int> GetMonthListByQuarter(int quarter)
    {
        switch (quarter)
        {
            case 1:
                return [1, 2, 3];
            case 2:
                return [4, 5, 6];
            case 3:
                return [7, 8, 9];
            case 4:
                return [10, 11, 12];
            default:
                throw new BadRequestException("Invalid quarter or year");
                break;
        }
    }
}
