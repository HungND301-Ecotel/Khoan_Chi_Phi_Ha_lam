using Application.Catalog.Pricing.Common;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Dashboard;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Dashboard.Queries;

public record GetCostSummaryQuery(Guid? ProcessGroupId, Guid? DepartmentId, int Year) : IRequest<CostSummaryDto>;

public class GetCostSummaryQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCostSummaryQuery, CostSummaryDto>
{
    private readonly IWriteRepository<ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<PlannedMaterialCost>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<PlannedElectricityCostAdjustmentFactor> _plannedElectricityFactorRepository = unitOfWork.GetRepository<PlannedElectricityCostAdjustmentFactor>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();

    public async Task<CostSummaryDto> Handle(GetCostSummaryQuery request, CancellationToken cancellationToken)
    {
        var year = request.Year;
        var startOfYear = new DateOnly(year, 1, 1);
        var endOfYear = new DateOnly(year, 12, 31);

        // STEP 1: Get Plan outputs for requested year and process group
        var outputs = await _outputRepository.GetAll()
            .Where(o => o.OutputType == OutputType.PlanOutput
                && o.ProductUnitPrice != null
                && o.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan
                && (!request.ProcessGroupId.HasValue || o.ProductUnitPrice.Product!.ProcessGroupId == request.ProcessGroupId)
                && (!request.DepartmentId.HasValue || o.ProductUnitPrice.DepartmentId == request.DepartmentId)
                && o.StartMonth <= endOfYear
                && o.EndMonth >= startOfYear)
            .Select(o => new OutputData
            {
                ProductUnitPriceId = o.ProductUnitPriceId,
                ProductId = o.ProductUnitPrice!.ProductId,
                DepartmentId = o.ProductUnitPrice.DepartmentId,
                Id = o.Id,
                OutputType = o.OutputType,
                StartMonth = o.StartMonth,
                EndMonth = o.EndMonth,
                ProductionMeters = o.ProductionMeters,
                ActualMaterialCostId = (Guid?)null,
                ActualMaintainCostId = (Guid?)null,
                ActualElectricityCostId = (Guid?)null,
                PlannedMaterialCostId = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.Id : (Guid?)null,
                PlannedMaintainCostId = o.PlannedMaintainCost != null ? o.PlannedMaintainCost.Id : (Guid?)null,
                PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var monthlyAllocatedOutputs = new Dictionary<int, List<OutputData>>();

        foreach (var o in outputs)
        {
            int startRange = (o.StartMonth.Year < year) ? 1 : o.StartMonth.Month;
            int endRange = (o.EndMonth.Year > year) ? 12 : o.EndMonth.Month;

            int durationInMonths = ((o.EndMonth.Year - o.StartMonth.Year) * 12) + o.EndMonth.Month - o.StartMonth.Month + 1;
            double metersPerMonth = o.ProductionMeters / durationInMonths;

            for (int m = startRange; m <= endRange; m++)
            {
                if (!monthlyAllocatedOutputs.ContainsKey(m))
                {
                    monthlyAllocatedOutputs[m] = new List<OutputData>();
                }

                var allocatedItem = new OutputData
                {
                    Id = o.Id,
                    ProductUnitPriceId = o.ProductUnitPriceId,
                    ProductId = o.ProductId,
                    DepartmentId = o.DepartmentId,
                    OutputType = o.OutputType,
                    ProductionMeters = metersPerMonth,
                    ActualMaterialCostId = o.ActualMaterialCostId,
                    ActualMaintainCostId = o.ActualMaintainCostId,
                    ActualElectricityCostId = o.ActualElectricityCostId,
                    PlannedMaterialCostId = o.PlannedMaterialCostId,
                    PlannedMaintainCostId = o.PlannedMaintainCostId,
                    PlannedElectricityCostId = o.PlannedElectricityCostId,
                    StartMonth = o.StartMonth,
                    EndMonth = o.EndMonth
                };
                monthlyAllocatedOutputs[m].Add(allocatedItem);
            }
        }

        // STEP 2: Get Adjustment production links (actual quantity source) by year and process group
        var adjustmentProductionLinks = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                && (!request.DepartmentId.HasValue || p.DepartmentId == request.DepartmentId)
                && (!request.ProcessGroupId.HasValue || p.Product!.ProcessGroupId == request.ProcessGroupId))
            .SelectMany(p => p.ProductUnitPriceProductionOutputs
                .Where(link => link.ProductionOutput != null
                    && link.ProductionOutput.StartMonth <= endOfYear
                    && link.ProductionOutput.EndMonth >= startOfYear)
                .Select(link => new AdjustmentProductionData
                {
                    ProductId = p.ProductId,
                    DepartmentId = p.DepartmentId,
                    StartMonth = link.ProductionOutput!.StartMonth,
                    EndMonth = link.ProductionOutput.EndMonth,
                    ProductionMeters = link.ProductionMeters,
                    ProcessGroupType = p.Product != null && p.Product.ProcessGroup != null
                        ? p.Product.ProcessGroup.Type
                        : ProcessGroupType.None
                }))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var monthlyAllocatedActuals = new Dictionary<int, List<AdjustmentProductionData>>();

        foreach (var item in adjustmentProductionLinks)
        {
            int startRange = (item.StartMonth.Year < year) ? 1 : item.StartMonth.Month;
            int endRange = (item.EndMonth.Year > year) ? 12 : item.EndMonth.Month;

            int durationInMonths = ((item.EndMonth.Year - item.StartMonth.Year) * 12) + item.EndMonth.Month - item.StartMonth.Month + 1;
            double metersPerMonth = item.ProductionMeters / durationInMonths;

            for (int m = startRange; m <= endRange; m++)
            {
                if (!monthlyAllocatedActuals.ContainsKey(m))
                {
                    monthlyAllocatedActuals[m] = new List<AdjustmentProductionData>();
                }

                monthlyAllocatedActuals[m].Add(new AdjustmentProductionData
                {
                    ProductId = item.ProductId,
                    DepartmentId = item.DepartmentId,
                    StartMonth = item.StartMonth,
                    EndMonth = item.EndMonth,
                    ProductionMeters = metersPerMonth,
                    ProcessGroupType = item.ProcessGroupType
                });
            }
        }

        // STEP 3: Batch load planned cost data
        var allPlannedOutputs = outputs.Where(o => o.OutputType == OutputType.PlanOutput).ToList();

        var plannedMaintainCostIds = allPlannedOutputs.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).Distinct().ToList();
        var plannedElectricityCostIds = allPlannedOutputs.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).Distinct().ToList();
        var plannedMaterialCostIds = allPlannedOutputs.Where(o => o.PlannedMaterialCostId.HasValue).Select(o => o.PlannedMaterialCostId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialCosts = await LoadPlannedMaterialCosts(plannedMaterialCostIds, cancellationToken);
        var customCostsByMonth = await LoadCustomCostsByMonth(year, request.ProcessGroupId, request.DepartmentId, cancellationToken);
        var transferredCostsByMonth = await LoadTransferredCostsByMonth(year, request.ProcessGroupId, request.DepartmentId, cancellationToken);

        // STEP 4: Calculate monthly costs
        var result = new CostSummaryDto();

        for (int month = 1; month <= 12; month++)
        {
            var monthOutputs = monthlyAllocatedOutputs.GetValueOrDefault(month) ?? new List<OutputData>();
            var plannedOutputs = monthOutputs.Where(o => o.OutputType == OutputType.PlanOutput).ToList();
            var monthActuals = monthlyAllocatedActuals.GetValueOrDefault(month) ?? new List<AdjustmentProductionData>();

            double monthlyTunnelQuantity = monthActuals
                .Where(o => o.ProcessGroupType == ProcessGroupType.DL)
                .Sum(o => o.ProductionMeters);
            double monthlyLongwallQuantity = monthActuals
                .Where(o => o.ProcessGroupType == ProcessGroupType.LC)
                .Sum(o => o.ProductionMeters);
            double monthlyOtherQuantity = monthActuals
                .Where(o => o.ProcessGroupType != ProcessGroupType.DL && o.ProcessGroupType != ProcessGroupType.LC)
                .Sum(o => o.ProductionMeters);
            double monthlyPlannedCost = CalculateMonthlyPlannedCost(
                plannedOutputs,
                plannedMaintainFactors,
                plannedElectricityFactors,
                plannedMaterialCosts);
            double monthlyAdjustmentCost = CalculateMonthlyAdjustmentCost(
                plannedOutputs,
                monthActuals,
                plannedMaintainFactors,
                plannedElectricityFactors,
                plannedMaterialCosts);
            double monthlyActualCost = customCostsByMonth.GetValueOrDefault(month, 0)
                + transferredCostsByMonth.GetValueOrDefault(month, 0);

            result.MonthlyData.Add(new MonthlyCostDto
            {
                Month = month,
                TunnelQuantity = monthlyTunnelQuantity,
                LongwallQuantity = monthlyLongwallQuantity,
                PlannedCost = monthlyPlannedCost,
                AdjustmentCost = monthlyAdjustmentCost,
                ActualCost = monthlyActualCost
            });

            result.TotalTunnelQuantity += monthlyTunnelQuantity;
            result.TotalLongwallQuantity += monthlyLongwallQuantity;
            result.TotalOtherQuantity += monthlyOtherQuantity;
            result.TotalPlannedCost += monthlyPlannedCost;
            result.TotalAdjustmentCost += monthlyAdjustmentCost;
            result.TotalActualCost += monthlyActualCost;
        }

        return result;
    }


    #region Load Methods

    private async Task<Dictionary<Guid, List<MaintainFactorData>>> LoadPlannedMaintainFactors(
        List<Guid> costIds, CancellationToken cancellationToken)
    {
        if (!costIds.Any())
        {
            return new Dictionary<Guid, List<MaintainFactorData>>();
        }

        var data = await _plannedMaintainFactorRepository.GetAll()
            .Where(f => costIds.Contains(f.PlannedMaintainCostId))
            .Select(f => new
            {
                f.PlannedMaintainCostId,
                f.Quantity,
                OtherMaterialValue = f.MaintainUnitPrice.OtherMaterialValue,
                MaintainStartMonth = f.MaintainUnitPrice.StartMonth,
                Equipments = f.MaintainUnitPrice.MaintainUnitPriceEquipments.Select(m => new
                {
                    m.Quantity,
                    m.ReplacementTimeStandard,
                    m.AverageMonthlyTunnelProduction,
                    PartCosts = m.Part.Costs.Select(c => new { c.StartMonth, c.EndMonth, c.Amount }).ToList()
                }).ToList(),
                AdjustmentValues = f.PlannedMaintainCostAdjustmentFactorDescriptions
                    .Select(d => d.AdjustmentFactorDescription.MaintenanceAdjustmentValue ?? 1.0).ToList()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return data.GroupBy(f => f.PlannedMaintainCostId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => new MaintainFactorData
                {
                    Quantity = Convert.ToDouble(f.Quantity),
                    OtherMaterialValue = f.OtherMaterialValue,
                    EquipmentCost = f.Equipments.Sum(m =>
                    {
                        var partCost = m.PartCosts.FirstOrDefault(c => c.StartMonth <= f.MaintainStartMonth && c.EndMonth >= f.MaintainStartMonth)?.Amount ?? 0;
                        return partCost * (m.Quantity / (double)(m.ReplacementTimeStandard * m.AverageMonthlyTunnelProduction));
                    }),
                    AdjustmentFactor = f.AdjustmentValues.Any() ? f.AdjustmentValues.Aggregate(1.0, (acc, val) => acc * val) : 1.0
                }).ToList());
    }

    private async Task<Dictionary<Guid, List<PlannedElectricityFactorData>>> LoadPlannedElectricityFactors(
        List<Guid> costIds, CancellationToken cancellationToken)
    {
        if (!costIds.Any())
        {
            return new Dictionary<Guid, List<PlannedElectricityFactorData>>();
        }

        var data = await _plannedElectricityFactorRepository.GetAll()
            .Include(f => f.ElectricityUnitPriceEquipment)
                .ThenInclude(e => e.Equipment)
                    .ThenInclude(eq => eq.Costs)
            .Where(f => costIds.Contains(f.PlannedElectricityCostId))
            .Select(f => new
            {
                f.PlannedElectricityCostId,
                f.Quantity,
                f.ElectricityUnitPriceEquipment,
                AdjustmentValues = f.PlannedElectricityCostAdjustmentFactorDescriptions
                    .Select(d => d.AdjustmentFactorDescription.MaintenanceAdjustmentValue ?? 1.0).ToList()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return data.GroupBy(f => f.PlannedElectricityCostId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f =>
                {
                    // Use abstract methods to calculate cost per metre
                    var costPerMetre = f.ElectricityUnitPriceEquipment.GetElectricityCostPerMetres();

                    return new PlannedElectricityFactorData
                    {
                        Quantity = Convert.ToDouble(f.Quantity),
                        CostPerMetre = costPerMetre,
                        AdjustmentFactor = f.AdjustmentValues.Any() ? f.AdjustmentValues.Aggregate(1.0, (acc, val) => acc * val) : 1.0
                    };
                }).ToList());
    }

    private async Task<Dictionary<Guid, double>> LoadPlannedMaterialCosts(
        List<Guid> plannedMaterialCostIds, CancellationToken cancellationToken)
    {
        if (!plannedMaterialCostIds.Any())
        {
            return new Dictionary<Guid, double>();
        }

        var plannedMaterialCosts = await _plannedMaterialCostRepository.GetAll()
            .Where(c => plannedMaterialCostIds.Contains(c.Id))
            .Include(c => c.Output)
            .Include(c => c.SlideUnitPriceAssignmentCode)
            .Include(c => c.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes)
            .Include(c => c.MaterialUnitPrice).ThenInclude(m => m.MaterialUnitPriceAssignmentCodes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var currentTunnelMaterials = plannedMaterialCosts
            .Where(c => c.NormFactor != null
                && c.NormFactor.NormFactorAssignmentCodes.Any(nfa => nfa.TargetHardnessId.HasValue)
                && c.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice)
            .Select(c => (TunnelExcavationMaterialUnitPrice)c.MaterialUnitPrice!)
            .ToList();

        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = new List<TunnelExcavationMaterialUnitPrice>();
        if (currentTunnelMaterials.Any())
        {
            var targetHardnessIds = plannedMaterialCosts
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

        return PlannedMaterialCostCalculator.CalculateUnitPricesByCostId(plannedMaterialCosts, tunnelMaterials);
    }

    #endregion

    #region Calculate Methods

    private static double CalculateMonthlyPlannedCost(
        List<OutputData> plannedOutputs,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<PlannedElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, double> plannedMaterialCosts)
    {
        if (!plannedOutputs.Any())
        {
            return 0;
        }

        return plannedOutputs.Sum(output =>
        {
            var materialCost = CalculatePlannedMaterialCost(output, plannedMaterialCosts);
            var maintainCost = CalculateMaintainCost(output.PlannedMaintainCostId, plannedMaintainFactors);
            var electricityCost = CalculatePlannedElectricityCost(output.PlannedElectricityCostId, plannedElectricityFactors);
            return output.ProductionMeters * (materialCost + maintainCost + electricityCost);
        });
    }

    private static double CalculateMonthlyAdjustmentCost(
        List<OutputData> plannedOutputs,
        List<AdjustmentProductionData> monthActuals,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<PlannedElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, double> plannedMaterialCosts)
    {
        if (!plannedOutputs.Any() || !monthActuals.Any())
        {
            return 0;
        }

        var actualQuantityByProduct = monthActuals
            .GroupBy(x => (x.ProductId, x.DepartmentId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ProductionMeters));

        var plannedByProduct = plannedOutputs.GroupBy(x => (x.ProductId, x.DepartmentId));
        var monthlyRevenue = 0.0;

        foreach (var productGroup in plannedByProduct)
        {
            if (!actualQuantityByProduct.TryGetValue(productGroup.Key, out var actualQuantity) || actualQuantity <= 0)
            {
                continue;
            }

            var productUnitPrice = productGroup.Sum(output =>
            {
                var materialCost = CalculatePlannedMaterialCost(output, plannedMaterialCosts);
                var maintainCost = CalculateMaintainCost(output.PlannedMaintainCostId, plannedMaintainFactors);
                var electricityCost = CalculatePlannedElectricityCost(output.PlannedElectricityCostId, plannedElectricityFactors);
                return materialCost + maintainCost + electricityCost;
            });

            monthlyRevenue += productUnitPrice * actualQuantity;
        }

        return monthlyRevenue;
    }

    private static double CalculatePlannedMaterialCost(
        OutputData output,
        Dictionary<Guid, double> plannedMaterialCosts)
    {
        if (!output.PlannedMaterialCostId.HasValue)
        {
            return 0;
        }

        return plannedMaterialCosts.GetValueOrDefault(output.PlannedMaterialCostId.Value, 0);
    }

    private static double CalculateMaintainCost(
        Guid? costId,
        Dictionary<Guid, List<MaintainFactorData>> maintainFactors)
    {
        if (!costId.HasValue || !maintainFactors.TryGetValue(costId.Value, out var factors))
        {
            return 0;
        }

        return factors.Sum(f => f.Quantity * f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) * f.AdjustmentFactor);
    }

    private static double CalculatePlannedElectricityCost(
        Guid? costId,
        Dictionary<Guid, List<PlannedElectricityFactorData>> electricityFactors)
    {
        if (!costId.HasValue || !electricityFactors.TryGetValue(costId.Value, out var factors))
        {
            return 0;
        }

        return factors.Sum(f => f.Quantity * f.CostPerMetre * f.AdjustmentFactor);
    }

    private async Task<Dictionary<int, double>> LoadCustomCostsByMonth(
        int year,
        Guid? processGroupId,
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (departmentId.HasValue)
        {
            // Custom cost chưa có DepartmentId để phân bổ chính xác
            return new Dictionary<int, double>();
        }

        var customCosts = await _customCostRepository.GetAll()
            .Where(x => x.Year == year
                && (!processGroupId.HasValue || x.ProcessGroupId == processGroupId))
            .Select(x => new
            {
                x.Month,
                TotalAmount = x.ActualQuantity * (x.MaterialUnitPrice + x.MaintainUnitPrice + x.ElectricityUnitPrice)
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return customCosts
            .GroupBy(x => x.Month)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));
    }

    private async Task<Dictionary<int, double>> LoadTransferredCostsByMonth(
        int year,
        Guid? processGroupId,
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var outputsWithAcceptanceReport = await _productionOutputRepository.GetAll()
            .Where(po => po.StartMonth.Year == year
                && po.AcceptanceReport != null
                && (!departmentId.HasValue || po.DepartmentId == departmentId))
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
                            .ThenInclude(e => e!.Costs)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = new Dictionary<int, double>();

        foreach (var output in outputsWithAcceptanceReport)
        {
            if (processGroupId.HasValue
                && !output.ProductionOutputProcessGroups.Any(pg => pg.ProcessGroupId == processGroupId.Value))
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
                .Where(i => !processGroupId.HasValue || i.ProcessGroupId == processGroupId.Value)
                .ToList();

            decimal transferredMaterial = 0m;
            decimal transferredMaintain = 0m;
            decimal transferredElectricity = 0m;

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

            var month = output.StartMonth.Month;
            var totalTransferred = (double)(transferredMaterial + transferredMaintain + transferredElectricity);
            result[month] = result.GetValueOrDefault(month, 0) + totalTransferred;
        }

        return result;
    }

    private static decimal GetPlannedUnitPrice(IReadOnlyCollection<Cost> costs, DateOnly month)
    {
        var cost = costs.FirstOrDefault(c => c.StartMonth <= month && c.EndMonth >= month);
        return cost == null ? 0 : (decimal)cost.Amount;
    }

    #endregion

    #region Helper Classes
    private class OutputData
    {
        public Guid ProductUnitPriceId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid Id { get; set; }
        public OutputType OutputType { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double ProductionMeters { get; set; }
        public Guid? ActualMaterialCostId { get; set; }
        public Guid? ActualMaintainCostId { get; set; }
        public Guid? ActualElectricityCostId { get; set; }
        public Guid? PlannedMaterialCostId { get; set; }
        public Guid? PlannedMaintainCostId { get; set; }
        public Guid? PlannedElectricityCostId { get; set; }
    }

    private class MaintainFactorData
    {
        public double Quantity { get; set; }
        public double? OtherMaterialValue { get; set; }
        public double EquipmentCost { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    private class PlannedElectricityFactorData
    {
        public double Quantity { get; set; }
        public double CostPerMetre { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    private class AdjustmentProductionData
    {
        public Guid ProductId { get; set; }
        public Guid? DepartmentId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double ProductionMeters { get; set; }
        public ProcessGroupType ProcessGroupType { get; set; }
    }

    #endregion
}


