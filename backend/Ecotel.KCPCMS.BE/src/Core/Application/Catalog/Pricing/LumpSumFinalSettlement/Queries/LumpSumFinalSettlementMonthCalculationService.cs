using System.Globalization;
using System.Text;
using Application.Catalog.Pricing.Common;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Domain.Entities.Production;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

internal sealed class LumpSumFinalSettlementMonthCalculationService(IUnitOfWork unitOfWork)
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _lowValuePerishableSupplyUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();
    private readonly IWriteRepository<SavingsRateConfig> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfig>();
    private readonly IWriteRepository<RevenueCostAdjustmentConfig> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfig>();

    public async Task<LumpSumFinalSettlementMonthResponseDto> CalculateAsync(
        int month,
        int year,
        Guid? processGroupId,
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var hasProcessGroupFilter = processGroupId.HasValue;
        var hasDepartmentFilter = departmentId.HasValue;

        var productionOutputs = await _productionOutputRepository.GetAllAsync(
            predicate: po => po.StartMonth.Month == month
                && po.StartMonth.Year == year
                && (!hasDepartmentFilter || po.DepartmentId == departmentId)
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
            .GroupBy(p => new { p.ProductionOutputProcessGroup!.ProcessGroupId, p.ProductId })
            .ToDictionary(g => (g.Key.ProcessGroupId, g.Key.ProductId), g => g.Sum(x => x.ProductionMeters));

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
                        .ThenInclude(pmc => pmc!.ProductUnitPrice)
                            .ThenInclude(pup => pup!.Product)
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
                                        .ThenInclude(part => part!.Costs)
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

        var allMonthPlannedMaterialCosts = productUnitPrices
            .SelectMany(p => p.Outputs)
            .Where(o => o.OutputType == OutputType.PlanOutput && o.StartMonth.Month == month && o.StartMonth.Year == year)
            .Where(o => o.PlannedMaterialCost != null)
            .Select(o => o.PlannedMaterialCost!)
            .ToList();

        var dependencies = await PlannedMaterialCostCalculationDependencyLoader.LoadAsync(
            allMonthPlannedMaterialCosts,
            _tunnelMaterialUnitPriceRepository,
            _lowValuePerishableSupplyUnitPriceRepository,
            cancellationToken);

        var plannedMaterialUnitCostById = PlannedMaterialCostCalculator.CalculateUnitPricesByCostId(
            allMonthPlannedMaterialCosts,
            dependencies.TunnelMaterialUnitPrices,
            dependencies.LowValuePerishableSupplyUnitPrices);

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

        var items = new List<LumpSumFinalSettlementDto>();

        foreach (var processGroup in groupedProductUnitPrices)
        {
            foreach (var productUnitPrice in processGroup)
            {
                var filteredOutputs = productUnitPrice.Outputs
                    .Where(o => o.OutputType == OutputType.PlanOutput
                        && o.StartMonth.Month == month
                        && o.StartMonth.Year == year)
                    .ToList();

                if (!filteredOutputs.Any())
                {
                    continue;
                }

                var plannedQuantity = filteredOutputs.Sum(o => o.ProductionMeters);

                var key = (productUnitPrice.Product!.ProcessGroupId, productUnitPrice.ProductId);
                var actualQuantity = productUnitPrice.ProductId != Guid.Empty && actualByProduct.TryGetValue(key, out var productActual)
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

                items.Add(new LumpSumFinalSettlementDto
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
                    Materials = new() { UnitPrice = materialUnitPrice, TotalAmount = materialTotalAmount },
                    Maintains = new() { UnitPrice = maintainUnitPrice, TotalAmount = maintainTotalAmount },
                    Electricities = new() { UnitPrice = electricityUnitPrice, TotalAmount = electricityTotalAmount },
                    TotalAmount = materialTotalAmount + maintainTotalAmount + electricityTotalAmount
                });
            }
        }

        var revenueMaterialTotal = 0.0;
        var revenueMaintainTotal = 0.0;
        var revenueElectricityTotal = 0.0;
        foreach (var processGroup in groupedProductUnitPrices)
        {
            foreach (var productUnitPrice in processGroup)
            {
                var monthOutputs = productUnitPrice.Outputs
                    .Where(o => o.OutputType == OutputType.PlanOutput
                        && o.StartMonth.Year == year
                        && o.StartMonth.Month == month)
                    .ToList();

                if (!monthOutputs.Any())
                {
                    continue;
                }

                var monthKey = (processGroup.Key.Id, productUnitPrice.ProductId);
                var monthActualQuantity = actualByProduct.TryGetValue(monthKey, out var value) ? value : 0;
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

                revenueMaterialTotal += Math.Round(materialUnitPrice * monthActualQuantity, 3);
                revenueMaintainTotal += maintainUnitPrice * monthActualQuantity;
                revenueElectricityTotal += electricityUnitPrice * monthActualQuantity;
            }
        }

        var outputsWithAcceptanceReport = await _productionOutputRepository.GetAllAsync(
            predicate: po => po.StartMonth.Year == year
                && po.StartMonth.Month == month
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
                            .ThenInclude(part => part!.Costs)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.AcceptanceReportItems)
                        .ThenInclude(i => i.ShippedDetails)
                .Include(po => po.AcceptanceReport!)
                    .ThenInclude(ar => ar.AcceptanceReportItems)
                        .ThenInclude(i => i.AcceptanceReportItemLogs),
            disableTracking: true);

        var transferredMaterial = 0m;
        var transferredMaintain = 0m;
        foreach (var output in outputsWithAcceptanceReport)
        {
            if (hasProcessGroupFilter && !output.ProductionOutputProcessGroups.Any(pg => pg.ProcessGroupId == processGroupId))
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

        var customCosts = await _customCostRepository.GetAllAsync(
            predicate: x => x.Month == month
                && x.Year == year
                && (!hasProcessGroupFilter || x.ProcessGroupId == processGroupId)
                && x.CustomName != LumpSumFinalSettlementSpecialQuantityKeys.CoalExcavation
                && x.CustomName != LumpSumFinalSettlementSpecialQuantityKeys.CoalCrosscut
                && x.CustomName != LumpSumFinalSettlementSpecialQuantityKeys.SavingCarryForward,
            disableTracking: true);

        var specialQuantities = await _customCostRepository.GetAllAsync(
            predicate: x => x.Month == month
                && x.Year == year
                && (hasProcessGroupFilter
                    ? x.ProcessGroupId == processGroupId
                    : x.ProcessGroupId == null)
                && (x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.CoalExcavation
                    || x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.CoalCrosscut),
            disableTracking: true);

        var coalExcavationActualQuantity = specialQuantities
            .Where(x => x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.CoalExcavation)
            .Sum(x => x.ActualQuantity);
        var coalCrosscutActualQuantity = specialQuantities
            .Where(x => x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.CoalCrosscut)
            .Sum(x => x.ActualQuantity);

        var carryForwardValues = await _customCostRepository.GetAllAsync(
            predicate: x => x.Year == year
                && x.Month <= month
                && (hasProcessGroupFilter
                    ? x.ProcessGroupId == processGroupId
                    : x.ProcessGroupId == null)
                && x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.SavingCarryForward,
            disableTracking: true);
        var carryForwardByMonthMap = carryForwardValues
            .GroupBy(x => x.Month)
            .ToDictionary(x => x.Key, x => x.Sum(c => c.ActualQuantity));
        var savingCarryForwardByMonths = Enumerable.Range(1, month)
            .Select(m => new LumpSumSavingCarryForwardByMonthDto
            {
                Month = m,
                Value = carryForwardByMonthMap.GetValueOrDefault(m, 0)
            })
            .ToList();
        var savingCarryForwardToNextMonths = carryForwardByMonthMap.GetValueOrDefault(month, 0);

        var meterExcavationActualQuantity = GetActualQuantityByGroupAndUnit(items, "DL", IsMeterUnit);
        var meterCrosscutActualQuantity = GetActualQuantityByGroupAndUnit(items, "XL", IsMeterUnit);
        var totalExcavationActualQuantity = GetActualQuantityByGroup(items, "DL");
        var totalCrosscutActualQuantity = GetActualQuantityByGroup(items, "XL");
        if (meterExcavationActualQuantity <= 0 && totalExcavationActualQuantity > 0)
        {
            meterExcavationActualQuantity = totalExcavationActualQuantity;
        }
        if (meterCrosscutActualQuantity <= 0 && totalCrosscutActualQuantity > 0)
        {
            meterCrosscutActualQuantity = totalCrosscutActualQuantity;
        }

        var transferredMaterialTotal = (double)transferredMaterial;
        var transferredMaintainTotal = (double)transferredMaintain;
        var transferredElectricityTotal = 0d;

        var customMaterialTotal = customCosts.Sum(x => x.ActualQuantity * x.MaterialUnitPrice);
        var customMaintainTotal = customCosts.Sum(x => x.ActualQuantity * x.MaintainUnitPrice);
        var customElectricityTotal = customCosts.Sum(x => x.ActualQuantity * x.ElectricityUnitPrice);

        var costMaterialTotal = transferredMaterialTotal + customMaterialTotal;
        var costMaintainTotal = transferredMaintainTotal + customMaintainTotal;
        var costElectricityTotal = transferredElectricityTotal + customElectricityTotal;
        var costTotal = costMaterialTotal + costMaintainTotal + costElectricityTotal;

        var savingMaterialTotal = revenueMaterialTotal - costMaterialTotal;
        var savingMaintainTotal = revenueMaintainTotal - costMaintainTotal;
        var savingElectricityTotal = revenueElectricityTotal - costElectricityTotal;
        var savingTotal = savingMaterialTotal + savingMaintainTotal + savingElectricityTotal;

        var savingsRateConfigs = await _savingsRateConfigRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var savingsValue = ResolveSavingsValue(savingTotal, savingsRateConfigs);
        var acceptedSavingMonth = savingTotal * savingsValue;

        var revenueCostAdjustmentConfigs = await _revenueCostAdjustmentConfigRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var revenueAdjustmentRate = ResolveRevenueCostAdjustmentRate(acceptedSavingMonth, revenueCostAdjustmentConfigs);
        var savingAddedToIncomeMonth = acceptedSavingMonth * revenueAdjustmentRate;

        return new LumpSumFinalSettlementMonthResponseDto
        {
            Items = items,
            Revenue = new LumpSumQuarterRevenueByMonthDto
            {
                Month = month,
                Materials = new LumpSumCostDetailDto { TotalAmount = revenueMaterialTotal },
                Maintains = new LumpSumCostDetailDto { TotalAmount = revenueMaintainTotal },
                Electricities = new LumpSumCostDetailDto { TotalAmount = revenueElectricityTotal },
                TotalAmount = revenueMaterialTotal + revenueMaintainTotal + revenueElectricityTotal
            },
            Cost = new LumpSumQuarterRevenueByMonthDto
            {
                Month = month,
                Materials = new LumpSumCostDetailDto { TotalAmount = costMaterialTotal },
                Maintains = new LumpSumCostDetailDto { TotalAmount = costMaintainTotal },
                Electricities = new LumpSumCostDetailDto { TotalAmount = costElectricityTotal },
                TotalAmount = costTotal
            },
            Saving = new LumpSumQuarterRevenueByMonthDto
            {
                Month = month,
                Materials = new LumpSumCostDetailDto { TotalAmount = savingMaterialTotal },
                Maintains = new LumpSumCostDetailDto { TotalAmount = savingMaintainTotal },
                Electricities = new LumpSumCostDetailDto { TotalAmount = savingElectricityTotal },
                TotalAmount = savingTotal
            },
            TransferredCost = new LumpSumQuarterTransferredCostDto
            {
                Month = month,
                Materials = new LumpSumCostDetailDto { TotalAmount = transferredMaterialTotal },
                Maintains = new LumpSumCostDetailDto { TotalAmount = transferredMaintainTotal },
                Electricities = new LumpSumCostDetailDto { TotalAmount = transferredElectricityTotal },
                TotalAmount = transferredMaterialTotal + transferredMaintainTotal + transferredElectricityTotal
            },
            CoalExcavationActualQuantity = coalExcavationActualQuantity,
            CoalCrosscutActualQuantity = coalCrosscutActualQuantity,
            MeterExcavationActualQuantity = meterExcavationActualQuantity,
            MeterCrosscutActualQuantity = meterCrosscutActualQuantity,
            TotalSavingMonth = savingTotal,
            SavingsValue = savingsValue,
            AcceptedSavingMonth = acceptedSavingMonth,
            RevenueAdjustmentRate = revenueAdjustmentRate,
            SavingAddedToIncomeMonth = savingAddedToIncomeMonth,
            SavingCarryForwardByMonths = savingCarryForwardByMonths,
            SavingCarryForwardToNextMonths = savingCarryForwardToNextMonths,
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

    public static double ResolveSavingsValue(
        double value,
        IReadOnlyCollection<SavingsRateConfig> configs)
    {
        var matchedConfig = configs
            .Where(x => IsRevenueInRange(value, x.MinRevenue, x.MaxRevenue))
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

    public static double ResolveRevenueCostAdjustmentRate(
        double value,
        IReadOnlyCollection<RevenueCostAdjustmentConfig> configs)
    {
        var matchedConfig = configs
            .Where(x => IsProfitInRange(value, x.MinProfit, x.MaxProfit))
            .OrderByDescending(x => x.MinProfit ?? decimal.MinValue)
            .ThenBy(x => x.MaxProfit ?? decimal.MaxValue)
            .ThenByDescending(x => x.CreatedOn)
            .FirstOrDefault();
        if (matchedConfig == null)
        {
            return 0;
        }

        var rawRate = matchedConfig.Rate;
        var normalizedRate = rawRate > 1 ? rawRate / 100m : rawRate;
        return (double)normalizedRate;
    }

    private static bool IsRevenueInRange(double revenue, decimal? minRevenue, decimal? maxRevenue)
    {
        var minMatch = !minRevenue.HasValue || revenue >= (double)minRevenue.Value;
        var maxMatch = !maxRevenue.HasValue || revenue <= (double)maxRevenue.Value;
        return minMatch && maxMatch;
    }

    private static bool IsProfitInRange(double profit, decimal? minProfit, decimal? maxProfit)
    {
        var minMatch = !minProfit.HasValue || profit >= (double)minProfit.Value;
        var maxMatch = !maxProfit.HasValue || profit <= (double)maxProfit.Value;
        return minMatch && maxMatch;
    }

    private static decimal GetPlannedUnitPrice(IReadOnlyCollection<Cost> costs, DateOnly month)
    {
        var cost = costs.FirstOrDefault(c => c.StartMonth <= month && c.EndMonth >= month);
        return cost == null ? 0 : (decimal)cost.Amount;
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
}
