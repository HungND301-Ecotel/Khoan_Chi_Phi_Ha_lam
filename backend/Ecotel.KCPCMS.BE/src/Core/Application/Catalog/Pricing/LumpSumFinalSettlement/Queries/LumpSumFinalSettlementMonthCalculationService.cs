using System.Globalization;
using System.Text;
using Application.Catalog.Pricing.Common;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using Application.Dto.Catalog.TransportPlanLine;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Domain.Entities.Production;
using Microsoft.EntityFrameworkCore;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

internal sealed class LumpSumFinalSettlementMonthCalculationService(IUnitOfWork unitOfWork)
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _lowValuePerishableSupplyUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice> _mechanizedTransportOverheadUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();
    private readonly IWriteRepository<SavingsRateConfig> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfig>();
    private readonly IWriteRepository<RevenueCostAdjustmentConfig> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfig>();
    private readonly IWriteRepository<AkFactorConfig> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfig>();
    private readonly IWriteRepository<LongTermAnchorSeedItemLog> _longTermAnchorSeedItemLogRepository = unitOfWork.GetRepository<LongTermAnchorSeedItemLog>();
    private readonly IWriteRepository<TransportPlanLineEntity> _transportPlanLineRepository = unitOfWork.GetRepository<TransportPlanLineEntity>();

    private readonly record struct VehicleTransferredCost(decimal Material, decimal Maintain, decimal Electricity);

    private readonly record struct TransportLineKey(
        Guid ProcessGroupId,
        Guid ProductionProcessId,
        Guid? EquipmentId,
        string? EquipmentQuality,
        Guid? TransportRouteId,
        Guid? RouteDepartmentId,
        Guid? HaulDistanceId,
        Guid? CargoTypeId,
        Guid? ReceivingLocationId,
        Guid? DumpingLocationId);

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
                    .ThenInclude(pg => pg.ProductionOutputProducts)
                .Include(po => po.ProductionOutputProcessGroups)
                    .ThenInclude(pg => pg.ProductionOutputTransportLines),
            disableTracking: true);

        var actualByTransportLine = productionOutputs
            .SelectMany(po => po.ProductionOutputProcessGroups)
            .Where(pg => !hasProcessGroupFilter || pg.ProcessGroupId == processGroupId)
            .SelectMany(pg => pg.ProductionOutputTransportLines
                .Select(tl => new { pg.ProcessGroupId, Line = tl }))
            .GroupBy(x => new TransportLineKey(
                x.ProcessGroupId,
                x.Line.ProductionProcessId,
                x.Line.EquipmentId,
                x.Line.EquipmentQuality,
                x.Line.TransportRouteId,
                x.Line.RouteDepartmentId,
                x.Line.HaulDistanceId,
                x.Line.CargoTypeId,
                x.Line.ReceivingLocationId,
                x.Line.DumpingLocationId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Line.ProductionMeters));

        var actualByProduct = productionOutputs
            .SelectMany(po => po.ProductionOutputProcessGroups)
            .Where(pg => !hasProcessGroupFilter || pg.ProcessGroupId == processGroupId)
            .SelectMany(pg => pg.ProductionOutputProducts)
            .GroupBy(p => new { p.ProductionOutputProcessGroup!.ProcessGroupId, p.ProductId })
            .ToDictionary(g => (g.Key.ProcessGroupId, g.Key.ProductId), g => g.Sum(x => x.ProductionMeters));

        var actualAshContentByProduct = productionOutputs
            .SelectMany(po => po.ProductionOutputProcessGroups)
            .Where(pg => !hasProcessGroupFilter || pg.ProcessGroupId == processGroupId)
            .SelectMany(pg => pg.ProductionOutputProducts)
            .GroupBy(p => new { p.ProductionOutputProcessGroup!.ProcessGroupId, p.ProductId })
            .ToDictionary(
                g => (g.Key.ProcessGroupId, g.Key.ProductId),
                g => g.Select(x => x.ActualAshContent).FirstOrDefault(v => v != 0));

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
                Code = p.Product?.ProcessGroup?.FixedKey?.Key ?? string.Empty,
                Name = p.Product?.ProcessGroup?.Name ?? string.Empty
            })
            .OrderBy(g => g.Key.Code)
            .ThenBy(g => g.Key.Name)
            .ToList();

        var items = new List<LumpSumFinalSettlementDto>();

        var akFactorConfigs = await _akFactorConfigRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var akFactorConfigsByProcessGroup = akFactorConfigs
            .GroupBy(x => x.ProcessGroupId)
            .ToDictionary(g => g.Key, g => g.ToList());


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
                    materialTotalAmount = materialUnitPrice * actualQuantity;
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

                var planAshContent = filteredOutputs
                    .Select(o => o.PlanAshContent)
                    .FirstOrDefault(v => v != 0);
                var actualAshContent = productUnitPrice.ProductId != Guid.Empty && actualAshContentByProduct.TryGetValue(key, out var productActualAshContent)
                    ? productActualAshContent
                    : 0d;

                var ashContentDeltaPercent = 0.0;
                var ashContentAdjustmentRate = 0.0;
                if (planAshContent != 0 && actualAshContent != 0)
                {
                    ashContentDeltaPercent = planAshContent - actualAshContent;
                    var processGroupAkConfigs = akFactorConfigsByProcessGroup.GetValueOrDefault(processGroup.Key.Id, []);
                    var resolvedRate = AkFactorConfig.ResolveRate(processGroupAkConfigs, (decimal)ashContentDeltaPercent);
                    ashContentAdjustmentRate = ashContentDeltaPercent * (double)resolvedRate;
                }

                var ashContentMaterialAmount = materialTotalAmount * ashContentAdjustmentRate;
                var ashContentMaintainAmount = maintainTotalAmount * ashContentAdjustmentRate;
                var ashContentElectricityAmount = electricityTotalAmount * ashContentAdjustmentRate;

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
                    PlanAshContent = planAshContent,
                    ActualAshContent = actualAshContent,
                    AshContentDeltaPercent = ashContentDeltaPercent,
                    Materials = new() { UnitPrice = materialUnitPrice, TotalAmount = materialTotalAmount },
                    Maintains = new() { UnitPrice = maintainUnitPrice, TotalAmount = maintainTotalAmount },
                    Electricities = new() { UnitPrice = electricityUnitPrice, TotalAmount = electricityTotalAmount },
                    AshContentMaterials = new() { TotalAmount = ashContentMaterialAmount },
                    AshContentMaintains = new() { TotalAmount = ashContentMaintainAmount },
                    AshContentElectricities = new() { TotalAmount = ashContentElectricityAmount },
                    AshContentTotalAmount = ashContentMaterialAmount + ashContentMaintainAmount + ashContentElectricityAmount,
                    TotalAmount = materialTotalAmount + maintainTotalAmount + electricityTotalAmount
                });
            }
        }

        var outputsWithAcceptanceReport = await _productionOutputRepository.GetAll()
            .Where(po => po.StartMonth.Year == year
                && po.StartMonth.Month == month
                && (!hasDepartmentFilter || po.DepartmentId == departmentId)
                && po.AcceptanceReport != null)
            .AsSplitQuery()
            .Include(po => po.ProductionOutputProcessGroups)
            .Include(po => po.AcceptanceReport!)
                .ThenInclude(ar => ar.AcceptanceReportItems)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m!.Costs)
            .Include(po => po.AcceptanceReport!)
                .ThenInclude(ar => ar.AcceptanceReportItems)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(part => part!.Costs)
            .Include(po => po.AcceptanceReport!)
                .ThenInclude(ar => ar.AcceptanceReportItems)
                    .ThenInclude(i => i.ShippedDetails)
            .Include(po => po.AcceptanceReport!)
                .ThenInclude(ar => ar.AcceptanceReportItems)
                    .ThenInclude(i => i.AcceptanceReportItemLogs)
            .Include(po => po.AcceptanceReport!)
                .ThenInclude(ar => ar.AcceptanceReportItems)
                    .ThenInclude(i => i.CategoryAllocations)
                        .ThenInclude(ca => ca.Equipments)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var transferredMaterial = 0m;
        var transferredMaintain = 0m;
        var maintainExportedToProduction = 0m;
        var anchorTransferredMaintainByReportId = await BuildAnchorTransferredMaintainByReportIdAsync(
            outputsWithAcceptanceReport,
            processGroupId,
            cancellationToken);

        var vehicleTransferredCosts = new Dictionary<Guid, VehicleTransferredCost>();

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
                var amount = (decimal)exportedToProductionQty * unitPrice;
                transferredMaterial += amount;

                var vehicleId = item.EquipmentId ?? item.CategoryAllocations.FirstOrDefault()?.FirstAssignmentCodeId;
                if (vehicleId.HasValue)
                {
                    var current = vehicleTransferredCosts.GetValueOrDefault(vehicleId.Value, new VehicleTransferredCost(0m, 0m, 0m));
                    vehicleTransferredCosts[vehicleId.Value] = new VehicleTransferredCost(current.Material + amount, current.Maintain, current.Electricity);
                }
            }

            foreach (var item in sectionAItems.Where(i => i.PartId.HasValue && i.Part != null))
            {
                var maintainUnitPrice = GetPlannedUnitPrice(item.Part!.Costs, output.StartMonth);
                var exportedToProductionQty = item.ShippedDetails
                    .Where(d => d.Type == ShippedQuantityType.XuatChoSanXuat)
                    .Sum(d => d.Quantity);
                var expAmount = (decimal)exportedToProductionQty * maintainUnitPrice;
                maintainExportedToProduction += expAmount;

                var logsOfCurrentReport = item.AcceptanceReportItemLogs
                    .Where(l => l.AcceptanceReportId == report.Id);
                var logAmount = logsOfCurrentReport.Sum(l => l.AccountedValueThisPeriod);
                transferredMaintain += logAmount;

                var vehicleId = item.EquipmentId ?? item.CategoryAllocations.FirstOrDefault()?.FirstAssignmentCodeId;
                if (vehicleId.HasValue)
                {
                    var current = vehicleTransferredCosts.GetValueOrDefault(vehicleId.Value, new VehicleTransferredCost(0m, 0m, 0m));
                    vehicleTransferredCosts[vehicleId.Value] = new VehicleTransferredCost(current.Material, current.Maintain + expAmount + logAmount, current.Electricity);
                }
            }

            transferredMaintain += anchorTransferredMaintainByReportId.GetValueOrDefault(report.Id, 0m);
        }

        // VTL/VTCG — song song khối Khai thác ở trên, xem claude/plan-dong-bo-van-tai.md mục 5.2.
        // Không có khái niệm Ak (độ tro) nên không cộng thêm AshContent*.
        var transportItems = await BuildTransportItemsAsync(
            month, year, processGroupId, departmentId, actualByTransportLine, vehicleTransferredCosts, cancellationToken);
        items.AddRange(transportItems);

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

                revenueMaterialTotal += materialUnitPrice * monthActualQuantity;
                revenueMaintainTotal += maintainUnitPrice * monthActualQuantity;
                revenueElectricityTotal += electricityUnitPrice * monthActualQuantity;
            }
        }

        revenueMaterialTotal += items.Sum(x => x.AshContentMaterials.TotalAmount);
        revenueMaintainTotal += items.Sum(x => x.AshContentMaintains.TotalAmount);
        revenueElectricityTotal += items.Sum(x => x.AshContentElectricities.TotalAmount);

        revenueMaterialTotal += transportItems.Sum(x => x.Materials.TotalAmount);
        revenueMaintainTotal += transportItems.Sum(x => x.Maintains.TotalAmount);
        revenueElectricityTotal += transportItems.Sum(x => x.Electricities.TotalAmount);

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
        var maintainExportedToProductionTotal = (double)maintainExportedToProduction;

        var customMaterialTotal = customCosts.Sum(x => x.ActualQuantity * x.MaterialUnitPrice);
        var customMaintainTotal = customCosts.Sum(x => x.ActualQuantity * x.MaintainUnitPrice);
        var customElectricityTotal = customCosts.Sum(x => x.ActualQuantity * x.ElectricityUnitPrice);

        var costMaterialTotal = transferredMaterialTotal + customMaterialTotal;
        var costMaintainTotal = maintainExportedToProductionTotal + transferredMaintainTotal + customMaintainTotal;
        var costElectricityTotal = transferredElectricityTotal + customElectricityTotal;
        var costTotal = costMaterialTotal + costMaintainTotal + costElectricityTotal;

        var savingMaterialTotal = revenueMaterialTotal - costMaterialTotal;
        var savingMaintainTotal = revenueMaintainTotal - costMaintainTotal;
        var savingElectricityTotal = revenueElectricityTotal - costElectricityTotal;
        var savingTotal = savingMaterialTotal + savingMaintainTotal + savingElectricityTotal;

        var savingsRateConfigs = await _savingsRateConfigRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var revenueTotal = revenueMaterialTotal + revenueMaintainTotal + revenueElectricityTotal;
        var savingsValue = ResolveSavingsValue(revenueTotal, savingsRateConfigs);
        var quyetToanSavingsLimit = revenueTotal * savingsValue;
        var acceptedSavingMonth = Math.Min(savingTotal, quyetToanSavingsLimit);
        //var acceptedSavingMonth = savingTotal;

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
                Maintains = new LumpSumCostDetailDto { TotalAmount = costMaintainTotal },
                Electricities = new LumpSumCostDetailDto { TotalAmount = costElectricityTotal },
                TotalAmount = transferredMaterialTotal + costMaintainTotal + costElectricityTotal
            },
            CoalExcavationActualQuantity = coalExcavationActualQuantity,
            CoalCrosscutActualQuantity = coalCrosscutActualQuantity,
            MeterExcavationActualQuantity = meterExcavationActualQuantity,
            MeterCrosscutActualQuantity = meterCrosscutActualQuantity,
            TotalSavingMonth = savingTotal,
            SavingsValue = savingsValue,
            QuyetToanSavingsLimit = quyetToanSavingsLimit,
            AcceptedSavingMonth = acceptedSavingMonth,
            RevenueAdjustmentRate = revenueAdjustmentRate,
            SavingAddedToIncomeMonth = savingAddedToIncomeMonth,
            SavingCarryForwardByMonths = savingCarryForwardByMonths,
            SavingCarryForwardToNextMonths = savingCarryForwardToNextMonths,
            RevenueCostAdjustmentConfigs = revenueCostAdjustmentConfigs
                .OrderBy(x => x.MinProfit ?? decimal.MinValue)
                .Select(x => new RevenueCostAdjustmentConfigDto
                {
                    Id = x.Id,
                    ProfitConditionDisplay = x.ProfitConditionDisplay,
                    MinProfit = x.MinProfit,
                    MaxProfit = x.MaxProfit,
                    RateDisplay = x.RateDisplay,
                    Rate = x.Rate,
                    Description = x.Description
                })
                .ToList(),
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

    // VTL/VTCG — song song khối "items" Khai thác ở CalculateAsync, dùng chung 1 dòng =
    // (ProcessGroupId, ProductionProcessId, TransportRouteId/RouteDepartmentId hoặc
    // EquipmentId/EquipmentQuality) thay cho (ProcessGroupId, ProductId). Đơn giá + K1/K2 lấy
    // nguyên từ Kế hoạch ban đầu (TransportPlanLine), TH lấy từ ProductionOutputTransportLine.
    private async Task<List<LumpSumFinalSettlementDto>> BuildTransportItemsAsync(
        int month,
        int year,
        Guid? processGroupId,
        Guid? departmentId,
        Dictionary<TransportLineKey, double> actualByTransportLine,
        Dictionary<Guid, VehicleTransferredCost> vehicleTransferredCosts,
        CancellationToken cancellationToken)
    {
        var hasProcessGroupFilter = processGroupId.HasValue;
        var hasDepartmentFilter = departmentId.HasValue;

        var lines = await _transportPlanLineRepository.GetAll()
            .Where(x =>
                x.ScenarioType == ProductUnitPriceScenarioType.Plan &&
                x.OutputType == OutputType.PlanOutput &&
                x.StartMonth.Month == month &&
                x.StartMonth.Year == year &&
                (!hasProcessGroupFilter || x.ProductionProcess!.ProcessGroupId == processGroupId) &&
                (!hasDepartmentFilter || x.DepartmentId == departmentId))
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.Code)
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.ProcessGroup)
                    .ThenInclude(pg => pg!.Code)
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.ProcessGroup)
                    .ThenInclude(pg => pg!.FixedKey)
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.Equipment)
                .ThenInclude(e => e!.Code)
            .Include(x => x.TransportRoute)
                .ThenInclude(r => r!.Code)
            .Include(x => x.RouteDepartment)
                .ThenInclude(rd => rd!.Code)
            .Include(x => x.CargoType)
                .ThenInclude(c => c!.Code)
            .Include(x => x.ReceivingLocation)
            .Include(x => x.DumpingLocation)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
                    .ThenInclude(f => f.AdjustmentFactor)
                        .ThenInclude(a => a!.FixedKey)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
                    .ThenInclude(f => f.AdjustmentFactor)
                        .ThenInclude(a => a!.Code)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
                    .ThenInclude(f => f.AdjustmentFactorDescription)
                        .ThenInclude(d => d!.AdjustmentFactor)
                            .ThenInclude(a => a.FixedKey)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
                    .ThenInclude(f => f.AdjustmentFactorDescription)
                        .ThenInclude(d => d!.AdjustmentFactor)
                            .ThenInclude(a => a.Code)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.TransportUnitPrice)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.MechanizedTransportUnitPriceDetail)
            .Include(x => x.HaulDistance)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = new List<LumpSumFinalSettlementDto>();

        foreach (var line in lines)
        {
            var planItem = Application.Catalog.Pricing.TransportPlanLine.Queries
                .GetTransportPlanLineByDepartmentQueryHandler.ToItemDto(line);

            var key = new TransportLineKey(
                line.ProductionProcess?.ProcessGroupId ?? Guid.Empty,
                line.ProductionProcessId,
                line.EquipmentId,
                line.EquipmentQuality,
                line.TransportRouteId,
                line.RouteDepartmentId,
                line.HaulDistanceId,
                line.CargoTypeId,
                line.ReceivingLocationId,
                line.DumpingLocationId);
            var actualQuantity = actualByTransportLine.TryGetValue(key, out var meters) ? meters : 0;

            double ComponentTotal(TransportCostComponentDto component) => planItem.IsLowVolumeCase
                ? component.EffectiveUnitPrice
                : component.EffectiveUnitPrice * actualQuantity;

            var nameParts = new List<string?>
            {
                planItem.ProductionProcessName,
                !string.IsNullOrEmpty(planItem.EquipmentName) ? $"TB: {planItem.EquipmentName}" : null,
                !string.IsNullOrEmpty(planItem.EquipmentQuality) ? $"Loại {planItem.EquipmentQuality}" : null,
                !string.IsNullOrEmpty(planItem.HaulDistanceValue) ? $"Cung độ: {planItem.HaulDistanceValue}" : null,
                !string.IsNullOrEmpty(planItem.TransportRouteName) ? $"Tuyến: {planItem.TransportRouteName}" : null,
            }.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            var materialTotal = ComponentTotal(planItem.Material);
            var maintainTotal = ComponentTotal(planItem.Maintenance);
            var electricityTotal = ComponentTotal(planItem.Power);

            var isVtcg = line.ProductionProcess?.ProcessGroup?.Type == ProcessGroupType.VTCG
                || line.ProductionProcess?.ProcessGroup?.FixedKey?.Key == "VTCG";
            var defaultGroupCode = isVtcg ? "VTCG" : "VTL";
            var defaultGroupName = isVtcg ? "Vận tải cơ giới" : "Vận tải lò";

            var vCost = line.EquipmentId.HasValue && vehicleTransferredCosts.TryGetValue(line.EquipmentId.Value, out var cost)
                ? cost
                : new VehicleTransferredCost(0m, 0m, 0m);
            var vMaterial = (double)vCost.Material;
            var vMaintain = (double)vCost.Maintain;
            var vElectricity = (double)vCost.Electricity;

            result.Add(new LumpSumFinalSettlementDto
            {
                Id = planItem.Id,
                ProcessGroupId = key.ProcessGroupId,
                ProcessGroupCode = line.ProductionProcess?.ProcessGroup?.Code?.Value
                    ?? line.ProductionProcess?.ProcessGroup?.FixedKey?.Key
                    ?? defaultGroupCode,
                ProcessGroupName = !string.IsNullOrWhiteSpace(line.ProductionProcess?.ProcessGroup?.Name)
                    ? line.ProductionProcess.ProcessGroup.Name
                    : defaultGroupName,
                ProductCode = !string.IsNullOrEmpty(planItem.ProductionProcessCode)
                    ? planItem.ProductionProcessCode
                    : (planItem.EquipmentCode ?? planItem.TransportRouteCode ?? defaultGroupCode),
                ProductName = nameParts.Count > 0 ? string.Join(" - ", nameParts) : defaultGroupName,
                ProductionProcessCode = planItem.ProductionProcessCode,
                ProductionProcessName = planItem.ProductionProcessName,
                TransportRouteId = planItem.TransportRouteId,
                TransportRouteCode = planItem.TransportRouteCode,
                TransportRouteName = planItem.TransportRouteName,
                RouteDepartmentId = planItem.RouteDepartmentId,
                RouteDepartmentCode = planItem.RouteDepartmentCode,
                RouteDepartmentName = planItem.RouteDepartmentName,
                EquipmentId = planItem.EquipmentId,
                EquipmentCode = planItem.EquipmentCode,
                EquipmentName = planItem.EquipmentName,
                EquipmentQuality = planItem.EquipmentQuality,
                HaulDistanceId = planItem.HaulDistanceId,
                HaulDistanceValue = planItem.HaulDistanceValue,
                CargoTypeId = line.CargoTypeId,
                CargoTypeCode = line.CargoType?.Code?.Value,
                CargoTypeName = line.CargoType?.Name,
                ReceivingLocationId = line.ReceivingLocationId,
                ReceivingLocationName = line.ReceivingLocation?.Name,
                DumpingLocationId = line.DumpingLocationId,
                DumpingLocationName = line.DumpingLocation?.Name,
                VehicleTransferredMaterialAmount = vMaterial,
                VehicleTransferredMaintainAmount = vMaintain,
                VehicleTransferredElectricityAmount = vElectricity,
                VehicleTotalTransferredCost = vMaterial + vMaintain + vElectricity,
                UnitOfMeasureId = planItem.UnitOfMeasureId ?? Guid.Empty,
                UnitOfMeasureName = planItem.UnitOfMeasureName ?? string.Empty,
                PlannedQuantity = planItem.ProductionMeters,
                ActualQuantity = actualQuantity,
                Materials = new LumpSumCostDetailDto { UnitPrice = planItem.Material.EffectiveUnitPrice, TotalAmount = materialTotal },
                Maintains = new LumpSumCostDetailDto { UnitPrice = planItem.Maintenance.EffectiveUnitPrice, TotalAmount = maintainTotal },
                Electricities = new LumpSumCostDetailDto { UnitPrice = planItem.Power.EffectiveUnitPrice, TotalAmount = electricityTotal },
                TotalAmount = materialTotal + maintainTotal + electricityTotal,
            });
        }

        // Chi phí vật tư mau hỏng rẻ tiền — khoản TRỌN GÓI THEO THÁNG (tick 1 lần áp dụng đồng
        // loạt cho cả tháng, không phải theo từng dòng Tuyến/Thiết bị — xem TransportMonthSection
        // FE), nên KHÔNG cộng vào đơn giá Vật liệu của từng dòng ở trên (sẽ bị nhân sai theo số
        // dòng nếu tháng có nhiều dòng) mà tách thành 1 dòng riêng cho mỗi (Đơn vị, Nhóm công đoạn).
        var lowValueLookup = await TransportLowValuePerishableSupplyCostResolver.ResolveAsync(
            lines, _lowValuePerishableSupplyUnitPriceRepository, _mechanizedTransportOverheadUnitPriceRepository, cancellationToken);

        var lowValueGroups = lines
            .Where(x => x.PlannedTransportCost?.LowValuePerishableSupplyInclusion == LowValuePerishableSupplyInclusion.Include
                && x.ProductionProcess?.ProcessGroupId != null)
            .GroupBy(x => new { x.DepartmentId, ProcessGroupId = x.ProductionProcess!.ProcessGroupId });

        foreach (var group in lowValueGroups)
        {
            var first = group.First();
            var key = new TransportLowValuePerishableSupplyCostResolver.Key(
                group.Key.DepartmentId, group.Key.ProcessGroupId, first.StartMonth);
            var price = lowValueLookup.TryGetValue(key, out var resolvedPrice) ? resolvedPrice : 0;
            if (price <= 0)
            {
                continue;
            }

            var isVtcg = first.ProductionProcess?.ProcessGroup?.Type == ProcessGroupType.VTCG
                || first.ProductionProcess?.ProcessGroup?.FixedKey?.Key == "VTCG";
            var defaultGroupCode = isVtcg ? "VTCG" : "VTL";
            var defaultGroupName = isVtcg ? "Vận tải cơ giới" : "Vận tải lò";

            result.Add(new LumpSumFinalSettlementDto
            {
                Id = Guid.NewGuid(),
                ProcessGroupId = group.Key.ProcessGroupId,
                ProcessGroupCode = first.ProductionProcess?.ProcessGroup?.Code?.Value
                    ?? first.ProductionProcess?.ProcessGroup?.FixedKey?.Key
                    ?? defaultGroupCode,
                ProcessGroupName = !string.IsNullOrWhiteSpace(first.ProductionProcess?.ProcessGroup?.Name)
                    ? first.ProductionProcess.ProcessGroup.Name
                    : defaultGroupName,
                ProductCode = isVtcg ? "RTMH-VTCG" : "RTMH-VTL",
                ProductName = "Chi phí vật tư mau hỏng rẻ tiền",
                UnitOfMeasureName = "Đồng/tháng",
                IsLowValuePerishableSupplyRow = true,
                Materials = new LumpSumCostDetailDto { TotalAmount = price },
                TotalAmount = price,
            });
        }

        return result;
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

    private async Task<Dictionary<Guid, decimal>> BuildAnchorTransferredMaintainByReportIdAsync(
        IReadOnlyCollection<ProductionOutput> outputsWithAcceptanceReport,
        Guid? processGroupId,
        CancellationToken cancellationToken)
    {
        var reportIds = outputsWithAcceptanceReport
            .Where(po => po.AcceptanceReport != null)
            .Select(po => po.AcceptanceReport!.Id)
            .Distinct()
            .ToList();

        if (reportIds.Count == 0)
        {
            return [];
        }

        var logs = await _longTermAnchorSeedItemLogRepository.GetAllAsync(
            predicate: x => reportIds.Contains(x.AcceptanceReportId),
            include: q => q.Include(x => x.LongTermAnchorSeedItem),
            disableTracking: true);

        return logs
            .Where(x => !processGroupId.HasValue || x.LongTermAnchorSeedItem.ProcessGroupId == processGroupId.Value)
            .GroupBy(x => x.AcceptanceReportId)
            .ToDictionary(x => x.Key, x => x.Sum(log => log.AccountedValueThisPeriod));
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

    public async Task<QuarterAcceptedSavingResult> CalculateQuarterAcceptedSavingAsync(
     double revenueQuarterTotal,
     CancellationToken cancellationToken)
    {
        var savingsRateConfigs = await _savingsRateConfigRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var savingsValue = ResolveSavingsValue(revenueQuarterTotal, savingsRateConfigs);
        var acceptedSavingQuarter = revenueQuarterTotal * savingsValue;

        var revenueCostAdjustmentConfigs = await _revenueCostAdjustmentConfigRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var revenueAdjustmentRate = ResolveRevenueCostAdjustmentRate(acceptedSavingQuarter, revenueCostAdjustmentConfigs);
        var savingAddedToIncomeQuarter = acceptedSavingQuarter * revenueAdjustmentRate;

        return new QuarterAcceptedSavingResult
        {
            SavingsValue = savingsValue,
            QuyetToanSavingsLimitQuarter = acceptedSavingQuarter,
            AcceptedSavingQuarter = acceptedSavingQuarter,
            RevenueAdjustmentRate = revenueAdjustmentRate,
            SavingAddedToIncomeQuarter = savingAddedToIncomeQuarter
        };
    }

    public sealed class QuarterAcceptedSavingResult
    {
        public double SavingsValue { get; set; }
        public double QuyetToanSavingsLimitQuarter { get; set; }
        public double AcceptedSavingQuarter { get; set; }
        public double RevenueAdjustmentRate { get; set; }
        public double SavingAddedToIncomeQuarter { get; set; }
    }
}