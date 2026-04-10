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

public record GetLumpSumFinalSettlementListQuery(string Month, string Year, string ProcessGroupId) : IRequest<LumpSumFinalSettlementMonthResponseDto>;

public class GetLumpSumFinalSettlementListQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLumpSumFinalSettlementListQuery, LumpSumFinalSettlementMonthResponseDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();

    public async Task<LumpSumFinalSettlementMonthResponseDto> Handle(GetLumpSumFinalSettlementListQuery request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Month, out var month) || !int.TryParse(request.Year, out var year))
        {
            throw new BadRequestException("Invalid month or year");
        }

        if (month < 1 || month > 12)
        {
            throw new BadRequestException("Month must be from 1 to 12");
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
            .GroupBy(p => new { p.ProductionOutputProcessGroup!.ProcessGroupId, p.ProductId })
            .ToDictionary(g => (g.Key.ProcessGroupId, g.Key.ProductId), g => g.Sum(x => x.ProductionMeters));

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

        var allMonthPlannedMaterialCosts = productUnitPrices
            .SelectMany(p => p.Outputs)
            .Where(o => o.OutputType == OutputType.PlanOutput && o.StartMonth.Month == month && o.StartMonth.Year == year)
            .Where(o => o.PlannedMaterialCost != null)
            .Select(o => o.PlannedMaterialCost!)
            .ToList();

        var currentTunnelMaterials = allMonthPlannedMaterialCosts
            .Where(c => c.NormFactor?.TargetHardnessId.HasValue == true && c.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice)
            .Select(c => (TunnelExcavationMaterialUnitPrice)c.MaterialUnitPrice!)
            .ToList();

        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = new List<TunnelExcavationMaterialUnitPrice>();
        if (currentTunnelMaterials.Any())
        {
            var targetHardnessIds = allMonthPlannedMaterialCosts
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
            allMonthPlannedMaterialCosts,
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

        var quarterOutputsWithAcceptanceReport = await _productionOutputRepository.GetAllAsync(
            predicate: po => po.StartMonth.Year == year
                && po.StartMonth.Month == month
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
                        .ThenInclude(i => i.AcceptanceReportItemLogs),
            disableTracking: true);

        var transferredMaterial = 0m;
        var transferredMaintain = 0m;

        foreach (var output in quarterOutputsWithAcceptanceReport)
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

        var customCosts = await _customCostRepository.GetAllAsync(
            predicate: x => x.Month == month
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

        return new LumpSumFinalSettlementMonthResponseDto
        {
            Items = result,
            Revenue = new LumpSumQuarterRevenueByMonthDto
            {
                Month = month,
                Materials = new LumpSumCostDetailDto { TotalAmount = revenueMaterialTotal },
                Maintains = new LumpSumCostDetailDto { TotalAmount = revenueMaintainTotal },
                Electricities = new LumpSumCostDetailDto { TotalAmount = revenueElectricityTotal },
                TotalAmount = revenueMaterialTotal + revenueMaintainTotal + revenueElectricityTotal
            },
            TransferredCost = new LumpSumQuarterTransferredCostDto
            {
                Month = month,
                Materials = new LumpSumCostDetailDto { TotalAmount = (double)transferredMaterial },
                Maintains = new LumpSumCostDetailDto { TotalAmount = (double)transferredMaintain },
                Electricities = new LumpSumCostDetailDto { TotalAmount = 0 },
                TotalAmount = (double)transferredMaterial + (double)transferredMaintain
            },
            CoalExcavationActualQuantity = 0,
            CoalCrosscutActualQuantity = 0,
            MeterExcavationActualQuantity = meterExcavationActualQuantity,
            MeterCrosscutActualQuantity = meterCrosscutActualQuantity,
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

