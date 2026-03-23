using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Dashboard;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Dashboard.Queries;

public record GetCostSummaryQuery(Guid? ProcessGroupId, int Year) : IRequest<CostSummaryDto>;

public class GetCostSummaryQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCostSummaryQuery, CostSummaryDto>
{
    private readonly IWriteRepository<ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<PlannedElectricityCostAdjustmentFactor> _plannedElectricityFactorRepository = unitOfWork.GetRepository<PlannedElectricityCostAdjustmentFactor>();

    public async Task<CostSummaryDto> Handle(GetCostSummaryQuery request, CancellationToken cancellationToken)
    {
        var year = request.Year;

        // STEP 1: Get Plan outputs for requested year and process group
        var outputs = await _outputRepository.GetAll()
            .Where(o => o.OutputType == OutputType.PlanOutput
                && o.ProductUnitPrice != null
                && o.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan
                && (!request.ProcessGroupId.HasValue || o.ProductUnitPrice.Product!.ProcessGroupId == request.ProcessGroupId)
                && o.StartMonth.Year <= year
                && o.EndMonth.Year >= year)
            .Select(o => new OutputData
            {
                ProductUnitPriceId = o.ProductUnitPriceId,
                ProductId = o.ProductUnitPrice!.ProductId,
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
                PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null,
                PlannedMaterialStoneClamp = o.PlannedMaterialCost != null && o.PlannedMaterialCost.StoneClampRatio != null
                    ? o.PlannedMaterialCost.StoneClampRatio.CoefficientValue
                    : 1,
                PlannedMaterialSlideQuantity = o.PlannedMaterialCost != null && o.PlannedMaterialCost.SlideUnitPriceAssignmentCode != null
                    ? 1 : 0,
                PlannedMaterialSlideCost = o.PlannedMaterialCost != null && o.PlannedMaterialCost.SlideUnitPriceAssignmentCode != null
                    ? o.PlannedMaterialCost.SlideUnitPriceAssignmentCode.Amount
                    : 0,
                PlannedMaterialUnitPriceId = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.MaterialUnitPriceId : (Guid?)null,
                PlannedMaterialUnitPriceStartMonth = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.MaterialUnitPrice.StartMonth : (DateOnly?)null
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
                    OutputType = o.OutputType,
                    ProductionMeters = metersPerMonth,
                    ActualMaterialCostId = o.ActualMaterialCostId,
                    ActualMaintainCostId = o.ActualMaintainCostId,
                    ActualElectricityCostId = o.ActualElectricityCostId,
                    PlannedMaterialCostId = o.PlannedMaterialCostId,
                    PlannedMaintainCostId = o.PlannedMaintainCostId,
                    PlannedElectricityCostId = o.PlannedElectricityCostId,
                    PlannedMaterialUnitPriceId = o.PlannedMaterialUnitPriceId,
                    PlannedMaterialStoneClamp = o.PlannedMaterialStoneClamp,
                    PlannedMaterialSlideQuantity = o.PlannedMaterialSlideQuantity,
                    PlannedMaterialSlideCost = o.PlannedMaterialSlideCost,
                    PlannedMaterialOtherValue = o.PlannedMaterialOtherValue,
                    StartMonth = o.StartMonth,
                    EndMonth = o.EndMonth
                };
                monthlyAllocatedOutputs[m].Add(allocatedItem);
            }
        }

        // STEP 2: Get Adjustment production links (actual quantity source) by year and process group
        var adjustmentProductionLinks = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                && (!request.ProcessGroupId.HasValue || p.Product!.ProcessGroupId == request.ProcessGroupId))
            .SelectMany(p => p.ProductUnitPriceProductionOutputs
                .Where(link => link.ProductionOutput != null
                    && link.ProductionOutput.StartMonth.Year <= year
                    && link.ProductionOutput.EndMonth.Year >= year)
                .Select(link => new AdjustmentProductionData
                {
                    ProductId = p.ProductId,
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
        var plannedMaterialUnitPriceIds = allPlannedOutputs.Where(o => o.PlannedMaterialUnitPriceId.HasValue).Select(o => o.PlannedMaterialUnitPriceId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialAssignments = await LoadPlannedMaterialAssignments(plannedMaterialUnitPriceIds, allPlannedOutputs, cancellationToken);

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
            double monthlyPlannedCost = CalculatePlannedTotalCost(plannedOutputs, plannedMaintainFactors, plannedElectricityFactors, plannedMaterialAssignments);

            var monthDate = new DateOnly(year, month, 1);
            double monthlyActualCost = monthActuals.Sum(actual =>
            {
                var matchedPlanOutput = allPlannedOutputs.FirstOrDefault(plan =>
                    plan.ProductId == actual.ProductId
                    && plan.StartMonth <= monthDate
                    && plan.EndMonth >= monthDate);

                if (matchedPlanOutput == null)
                {
                    return 0;
                }

                var materialCost = CalculatePlannedMaterialCost(matchedPlanOutput, plannedMaterialAssignments);
                var maintainCost = CalculateMaintainCost(matchedPlanOutput.PlannedMaintainCostId, plannedMaintainFactors);
                var electricityCost = CalculatePlannedElectricityCost(matchedPlanOutput.PlannedElectricityCostId, plannedElectricityFactors);

                return actual.ProductionMeters * (materialCost + maintainCost + electricityCost);
            });

            result.MonthlyData.Add(new MonthlyCostDto
            {
                Month = month,
                TunnelQuantity = monthlyTunnelQuantity,
                LongwallQuantity = monthlyLongwallQuantity,
                PlannedCost = monthlyPlannedCost,
                ActualCost = monthlyActualCost
            });

            result.TotalTunnelQuantity += monthlyTunnelQuantity;
            result.TotalLongwallQuantity += monthlyLongwallQuantity;
            result.TotalOtherQuantity += monthlyOtherQuantity;
            result.TotalPlannedCost += monthlyPlannedCost;
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
                    //m.ReplacementTimeStandard,
                    m.Part.ReplacementTimeStandard,
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
                    Quantity = (double)f.Quantity,
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
                        Quantity = (double)f.Quantity,
                        CostPerMetre = costPerMetre,
                        AdjustmentFactor = f.AdjustmentValues.Any() ? f.AdjustmentValues.Aggregate(1.0, (acc, val) => acc * val) : 1.0
                    };
                }).ToList());
    }

    private async Task<Dictionary<Guid, List<MaterialAssignmentData>>> LoadPlannedMaterialAssignments(
        List<Guid> materialUnitPriceIds, List<OutputData> plannedOutputs, CancellationToken cancellationToken)
    {
        if (!materialUnitPriceIds.Any())
        {
            return new Dictionary<Guid, List<MaterialAssignmentData>>();
        }

        var unitPriceData = await _productUnitPriceRepository.GetAll()
            .SelectMany(p => p.PlannedMaterialCosts)
            .Where(c => materialUnitPriceIds.Contains(c.MaterialUnitPriceId))
            .Select(c => new
            {
                c.MaterialUnitPriceId,
                c.MaterialUnitPrice.TotalPrice
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return unitPriceData.GroupBy(a => a.MaterialUnitPriceId)
            .ToDictionary(
                g => g.Key,
                g => new List<MaterialAssignmentData>
                {
                    new MaterialAssignmentData { Quantity = 1, Cost = g.FirstOrDefault()?.TotalPrice ?? 0 }
                });
    }

    #endregion

    #region Calculate Methods

    private static double CalculatePlannedTotalCost(
        List<OutputData> outputs,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<PlannedElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, List<MaterialAssignmentData>> plannedMaterialAssignments)
    {
        if (!outputs.Any())
        {
            return 0;
        }

        return outputs.Sum(output =>
        {
            var materialCost = CalculatePlannedMaterialCost(output, plannedMaterialAssignments);
            var maintainCost = CalculateMaintainCost(output.PlannedMaintainCostId, plannedMaintainFactors);
            var electricityCost = CalculatePlannedElectricityCost(output.PlannedElectricityCostId, plannedElectricityFactors);
            return output.ProductionMeters * (materialCost + maintainCost + electricityCost);
        });
    }

    private static double CalculatePlannedMaterialCost(
        OutputData output,
        Dictionary<Guid, List<MaterialAssignmentData>> plannedMaterialAssignments)
    {
        if (!output.PlannedMaterialUnitPriceId.HasValue ||
            !plannedMaterialAssignments.TryGetValue(output.PlannedMaterialUnitPriceId.Value, out var matAssignments))
        {
            return 0;
        }

        var slideCost = output.PlannedMaterialSlideCost * output.PlannedMaterialSlideQuantity;
        var materialTotal = matAssignments.Sum(m => m.Cost * m.Quantity);

        if (output.PlannedMaterialOtherValue.HasValue)
        {
            materialTotal += materialTotal * (output.PlannedMaterialOtherValue.Value / 100.0);
        }

        return (slideCost + materialTotal) * output.PlannedMaterialStoneClamp;
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

    #endregion

    #region Helper Classes
    private class OutputData
    {
        public Guid ProductUnitPriceId { get; set; }
        public Guid ProductId { get; set; }
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
        public double? PlannedMaterialOtherValue { get; set; }
        public double PlannedMaterialStoneClamp { get; set; }
        public double PlannedMaterialSlideQuantity { get; set; }
        public double PlannedMaterialSlideCost { get; set; }
        public Guid? PlannedMaterialUnitPriceId { get; set; }
        public DateOnly? PlannedMaterialUnitPriceStartMonth { get; set; }
    }

    private class MaterialAssignmentData
    {
        public double Quantity { get; set; }
        public double Cost { get; set; }
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
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double ProductionMeters { get; set; }
        public ProcessGroupType ProcessGroupType { get; set; }
    }

    #endregion
}
