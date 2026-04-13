using System.Globalization;
using System.Text;
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

public record GetLumpSumFinalSettlementQuarterListQuery(string Quarter, string Year, string? ProcessGroupId, string? DepartmentId) : IRequest<LumpSumFinalSettlementQuarterResponseDto>;

public class GetLumpSumFinalSettlementQuarterListQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLumpSumFinalSettlementQuarterListQuery, LumpSumFinalSettlementQuarterResponseDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();
    private readonly IWriteRepository<SavingsRateConfig> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfig>();

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
        var hasDepartmentFilter = Guid.TryParse(request.DepartmentId, out var departmentId);

        var productionOutputs = await _productionOutputRepository.GetAllAsync(
            predicate: po => po.StartMonth.Year == year
                && po.StartMonth.Month >= quarterStartMonth
                && po.StartMonth.Month <= quarterEndMonth
                && (!hasDepartmentFilter || po.DepartmentId == departmentId)
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
                && (!hasProcessGroupFilter || p.Product!.ProcessGroupId == processGroupId)
                && (!hasDepartmentFilter || p.DepartmentId == departmentId),
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
            .Where(c => c.NormFactor != null
                && c.NormFactor.NormFactorAssignmentCodes.Any(nfa => nfa.TargetHardnessId.HasValue)
                && c.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice)
            .Select(c => (TunnelExcavationMaterialUnitPrice)c.MaterialUnitPrice!)
            .ToList();

        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = new List<TunnelExcavationMaterialUnitPrice>();
        if (currentTunnelMaterials.Any())
        {
            var targetHardnessIds = allQuarterPlannedMaterialCosts
                .Where(c => c.NormFactor != null)
                .SelectMany(c => c.NormFactor!.NormFactorAssignmentCodes
                    .Where(nfa => nfa.TargetHardnessId.HasValue)
                    .Select(nfa => nfa.TargetHardnessId!.Value))
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
                        && o.StartMonth.Month >= quarterStartMonth
                        && o.StartMonth.Month <= quarterEndMonth)
                    .ToList();
                if (!filteredOutputs.Any())
                {
                    continue;
                }

                var plannedQuantity = filteredOutputs.Sum(o => o.ProductionMeters);

                var actualQuantity = 0.0;
                for (var currentMonth = quarterStartMonth; currentMonth <= quarterEndMonth; currentMonth++)
                {
                    var monthKey = (productUnitPrice.Product!.ProcessGroupId, productUnitPrice.ProductId, currentMonth);
                    if (productUnitPrice.ProductId != Guid.Empty && actualByProductMonth.TryGetValue(monthKey, out var productActual))
                    {
                        actualQuantity += productActual;
                    }
                }

                var materialTotalAmount = 0.0;
                var maintainTotalAmount = 0.0;
                var electricityTotalAmount = 0.0;

                for (var currentMonth = quarterStartMonth; currentMonth <= quarterEndMonth; currentMonth++)
                {
                    var monthOutputs = filteredOutputs
                        .Where(o => o.StartMonth.Month == currentMonth)
                        .ToList();

                    if (!monthOutputs.Any())
                    {
                        continue;
                    }

                    var monthKey = (productUnitPrice.Product!.ProcessGroupId, productUnitPrice.ProductId, currentMonth);
                    var monthActualQuantity = productUnitPrice.ProductId != Guid.Empty && actualByProductMonth.TryGetValue(monthKey, out var monthActual)
                        ? monthActual
                        : 0;

                    if (monthActualQuantity <= 0)
                    {
                        continue;
                    }

                    var materialUnitPriceByMonth = monthOutputs
                        .Where(o => o.PlannedMaterialCost != null)
                        .Select(o => plannedMaterialUnitCostById.GetValueOrDefault(o.PlannedMaterialCost!.Id, 0))
                        .Sum();

                    var maintainUnitPriceByMonth = monthOutputs
                        .Where(o => o.PlannedMaintainCost != null)
                        .Select(o => o.PlannedMaintainCost!.GetPlannedTotalPrice())
                        .Sum();

                    var electricityUnitPriceByMonth = monthOutputs
                        .Where(o => o.PlannedElectricityCost != null)
                        .Select(o => o.PlannedElectricityCost!.GetPlannedTotalPrice())
                        .Sum();

                    materialTotalAmount += Math.Round(materialUnitPriceByMonth * monthActualQuantity, 3);
                    maintainTotalAmount += maintainUnitPriceByMonth * monthActualQuantity;
                    electricityTotalAmount += electricityUnitPriceByMonth * monthActualQuantity;
                }

                var materialUnitPrice = actualQuantity > 0
                    ? materialTotalAmount / actualQuantity
                    : 0;
                var maintainUnitPrice = actualQuantity > 0
                    ? maintainTotalAmount / actualQuantity
                    : 0;
                var electricityUnitPrice = actualQuantity > 0
                    ? electricityTotalAmount / actualQuantity
                    : 0;

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

        result = AggregateByProduct(result);

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
                && (!hasDepartmentFilter || po.DepartmentId == departmentId)
                && po.AcceptanceReport != null,
            include: q => q.AsSplitQuery()
                .Include(po => po.ProductionOutputProcessGroups)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.AcceptanceReportItems)
                        .ThenInclude(i => i.Material)
                            .ThenInclude(m => m!.Costs)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.AcceptanceReportItems)
                        .ThenInclude(i => i.MaintainUnitPriceEquipment).ThenInclude(m => m.Part)
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

                foreach (var item in sectionAItems.Where(i => i.MaintainUnitPriceEquipmentId.HasValue && i.MaintainUnitPriceEquipment?.Part != null))
                {
                    var logsOfCurrentReport = item.AcceptanceReportItemLogs
                        .Where(l => l.AcceptanceReportId == report.Id);
                    transferredMaintain += logsOfCurrentReport.Sum(l => l.AccountedValueThisPeriod);
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
                Electricities = new LumpSumCostDetailDto { TotalAmount = 0 },
                TotalAmount = transferredMaterialDouble + transferredMaintainDouble + transferredElectricityDouble
            });
        }

        var monthList = GetMonthListByQuarter(quarter);
        var customCosts = await _customCostRepository.GetAllAsync(
            predicate: x => monthList.Contains(x.Month)
                && x.Year == year
                && (!hasProcessGroupFilter || x.ProcessGroupId == processGroupId),
            disableTracking: true);

        var meterExcavationActualQuantity = GetActualQuantityByGroupAndUnit(result, "DL", IsMeterUnit);
        var meterCrosscutActualQuantity = GetActualQuantityByGroupAndUnit(result, "XL", IsMeterUnit);
        var totalExcavationActualQuantity = GetActualQuantityByGroup(result, "DL");
        var totalCrosscutActualQuantity = GetActualQuantityByGroup(result, "XL");

        if (meterExcavationActualQuantity <= 0 && totalExcavationActualQuantity > 0)
        {
            meterExcavationActualQuantity = totalExcavationActualQuantity;
        }

        if (meterCrosscutActualQuantity <= 0 && totalCrosscutActualQuantity > 0)
        {
            meterCrosscutActualQuantity = totalCrosscutActualQuantity;
        }

        var revenueMaterialTotal = revenuesByMonth.Sum(x => x.Materials.TotalAmount);
        var revenueMaintainTotal = revenuesByMonth.Sum(x => x.Maintains.TotalAmount);
        var revenueElectricityTotal = revenuesByMonth.Sum(x => x.Electricities.TotalAmount);

        var transferredMaterialTotal = transferredCostsByMonth.Sum(x => x.Materials.TotalAmount);
        var transferredMaintainTotal = transferredCostsByMonth.Sum(x => x.Maintains.TotalAmount);
        var transferredElectricityTotal = transferredCostsByMonth.Sum(x => x.Electricities.TotalAmount);

        var customMaterialTotal = customCosts.Sum(x => x.ActualQuantity * x.MaterialUnitPrice);
        var customMaintainTotal = customCosts.Sum(x => x.ActualQuantity * x.MaintainUnitPrice);
        var customElectricityTotal = customCosts.Sum(x => x.ActualQuantity * x.ElectricityUnitPrice);

        var acceptedSavingQuarter =
            (revenueMaterialTotal - (transferredMaterialTotal + customMaterialTotal))
            + (revenueMaintainTotal - (transferredMaintainTotal + customMaintainTotal))
            + (revenueElectricityTotal - (transferredElectricityTotal + customElectricityTotal));

        var savingsRateConfigs = await _savingsRateConfigRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var savingsValue = ResolveSavingsValue(acceptedSavingQuarter, savingsRateConfigs);

        return new LumpSumFinalSettlementQuarterResponseDto
        {
            Items = result,
            RevenuesByMonth = revenuesByMonth,
            TransferredCosts = transferredCostsByMonth,
            CoalExcavationActualQuantity = 0,
            CoalCrosscutActualQuantity = 0,
            MeterExcavationActualQuantity = meterExcavationActualQuantity,
            MeterCrosscutActualQuantity = meterCrosscutActualQuantity,
            AcceptedSavingQuarter = acceptedSavingQuarter,
            SavingsValue = savingsValue,
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

    private static List<LumpSumFinalSettlementDto> AggregateByProduct(IEnumerable<LumpSumFinalSettlementDto> items)
    {
        return items
            .GroupBy(x => new
            {
                x.ProcessGroupId,
                x.ProcessGroupCode,
                x.ProcessGroupName,
                x.ProductCode,
                x.ProductName,
                x.UnitOfMeasureId,
                x.UnitOfMeasureName
            })
            .Select(group =>
            {
                var plannedQuantity = group.Sum(x => x.PlannedQuantity);
                var actualQuantity = group.Sum(x => x.ActualQuantity);
                var materialTotal = group.Sum(x => x.Materials?.TotalAmount ?? 0);
                var maintainTotal = group.Sum(x => x.Maintains?.TotalAmount ?? 0);
                var electricityTotal = group.Sum(x => x.Electricities?.TotalAmount ?? 0);

                return new LumpSumFinalSettlementDto
                {
                    Id = group.Select(x => x.Id).FirstOrDefault(),
                    ProcessGroupId = group.Key.ProcessGroupId,
                    ProcessGroupCode = group.Key.ProcessGroupCode,
                    ProcessGroupName = group.Key.ProcessGroupName,
                    ProductCode = group.Key.ProductCode,
                    ProductName = group.Key.ProductName,
                    UnitOfMeasureId = group.Key.UnitOfMeasureId,
                    UnitOfMeasureName = group.Key.UnitOfMeasureName,
                    PlannedQuantity = plannedQuantity,
                    ActualQuantity = actualQuantity,
                    Materials = new LumpSumCostDetailDto
                    {
                        UnitPrice = actualQuantity > 0 ? materialTotal / actualQuantity : 0,
                        TotalAmount = materialTotal
                    },
                    Maintains = new LumpSumCostDetailDto
                    {
                        UnitPrice = actualQuantity > 0 ? maintainTotal / actualQuantity : 0,
                        TotalAmount = maintainTotal
                    },
                    Electricities = new LumpSumCostDetailDto
                    {
                        UnitPrice = actualQuantity > 0 ? electricityTotal / actualQuantity : 0,
                        TotalAmount = electricityTotal
                    },
                    TotalAmount = materialTotal + maintainTotal + electricityTotal
                };
            })
            .OrderBy(x => x.ProcessGroupCode)
            .ThenBy(x => x.ProcessGroupName)
            .ThenBy(x => x.ProductCode)
            .ThenBy(x => x.ProductName)
            .ToList();
    }

    private static double ResolveSavingsValue(
        double acceptedSavingQuarter,
        IReadOnlyCollection<SavingsRateConfig> configs)
    {
        var matchedConfig = configs
            .Where(x => IsRevenueInRange(acceptedSavingQuarter, x.MinRevenue, x.MaxRevenue))
            .OrderByDescending(x => x.MinRevenue ?? decimal.MinValue)
            .ThenBy(x => x.MaxRevenue ?? decimal.MaxValue)
            .ThenByDescending(x => x.CreatedOn)
            .FirstOrDefault();

        if (matchedConfig == null)
        {
            return 0;
        }

        var rawRate = matchedConfig.MaxSavingsRate ?? matchedConfig.MinSavingsRate;
        if (!rawRate.HasValue)
        {
            return 0;
        }

        var normalizedRate = rawRate.Value > 1 ? rawRate.Value / 100m : rawRate.Value;
        return (double)normalizedRate;
    }

    private static bool IsRevenueInRange(double revenue, decimal? minRevenue, decimal? maxRevenue)
    {
        var minMatch = !minRevenue.HasValue || revenue >= (double)minRevenue.Value;
        var maxMatch = !maxRevenue.HasValue || revenue <= (double)maxRevenue.Value;
        return minMatch && maxMatch;
    }

    private static double GetActualQuantityByGroupAndUnit(
        IEnumerable<LumpSumFinalSettlementDto> items,
        string processGroupCode,
        Func<string, bool> unitPredicate)
    {
        return items
            .Where(x => string.Equals(x.ProcessGroupCode, processGroupCode, StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.IsNullOrWhiteSpace(x.UnitOfMeasureName) && unitPredicate(x.UnitOfMeasureName))
            .Sum(x => x.ActualQuantity);
    }

    private static double GetActualQuantityByGroup(
        IEnumerable<LumpSumFinalSettlementDto> items,
        string processGroupCode)
    {
        return items
            .Where(x => string.Equals(x.ProcessGroupCode, processGroupCode, StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.ActualQuantity);
    }

    private static bool IsMeterUnit(string unitName)
    {
        var normalized = NormalizeText(unitName);
        return normalized is "met" or "m";
    }

    private static string NormalizeText(string input)
    {
        var decomposed = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace(" ", string.Empty);
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

