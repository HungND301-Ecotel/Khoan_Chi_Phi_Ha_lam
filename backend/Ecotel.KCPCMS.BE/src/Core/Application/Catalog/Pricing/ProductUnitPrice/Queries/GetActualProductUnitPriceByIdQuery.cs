using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Queries;

public record GetActualProductUnitPriceByIdQuery(DefaultIdType Id) : IRequest<ActualProductUnitPriceDetailDto>;

public class GetActualProductUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<GetActualProductUnitPriceByIdQuery, ActualProductUnitPriceDetailDto>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<PlannedElectricityCostAdjustmentFactor> _plannedElectricityFactorRepository = unitOfWork.GetRepository<PlannedElectricityCostAdjustmentFactor>();

    public async Task<ActualProductUnitPriceDetailDto> Handle(GetActualProductUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheSignalKey}:Actual:{request.Id}";

        var cachedResult = await cacheService.GetAsync<ActualProductUnitPriceDetailDto>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        // STEP 1: Get base ProductUnitPrice info using optimized query
        var baseData = await _productUnitPriceRepository.GetAll()
            .Where(e => e.Id == request.Id && e.ScenarioType == ProductUnitPriceScenarioType.Plan)
            .Select(p => new
            {
                p.Id,
                p.ProductId,
                ProductName = p.Product!.Name,
                ProductCode = p.Product.Code.Value,
                p.UnitOfMeasureId,
                UnitOfMeasureName = p.UnitOfMeasure!.Name,
                ProcessGroupId = p.Product.ProcessGroupId,
                ProcessGroupCode = p.Product.ProcessGroup!.Code.Value,
                ProcessGroupName = p.Product.ProcessGroup.Name,
                ProcessGroupType = p.Product.ProcessGroup.Type,
                ProductionOutputId = p.ProductUnitPriceProductionOutputs.Select(po => po.ProductionOutputId).FirstOrDefault()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (baseData == null)
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        // STEP 2: Get all outputs for this ProductUnitPrice
        var outputs = await _outputRepository.GetAll()
            .Where(o => o.ProductUnitPriceId == request.Id)
            .Select(o => new OutputRawData
            {
                Id = o.Id,
                OutputType = o.OutputType,
                StartMonth = o.StartMonth,
                EndMonth = o.EndMonth,
                ProductionMeters = o.ProductionMeters,
                PlannedMaterialCostId = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.Id : (Guid?)null,
                PlannedMaintainCostId = o.PlannedMaintainCost != null ? o.PlannedMaintainCost.Id : (Guid?)null,
                PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null,
                PlannedMaterialStoneClamp = o.PlannedMaterialCost != null && o.PlannedMaterialCost.StoneClampRatio != null ? o.PlannedMaterialCost.StoneClampRatio.CoefficientValue : 1,
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

        var actualOutputs = outputs.Where(o => o.OutputType == OutputType.ActualOutput).ToList();
        var plannedOutputs = outputs.Where(o => o.OutputType == OutputType.PlanOutput).ToDictionary(o => (o.StartMonth, o.EndMonth));

        // STEP 3: Load planned cost data
        var plannedMaintainCostIds = plannedOutputs.Values.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).ToList();
        var plannedElectricityCostIds = plannedOutputs.Values.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).ToList();
        var plannedMaterialUnitPriceIds = plannedOutputs.Values.Where(o => o.PlannedMaterialUnitPriceId.HasValue).Select(o => o.PlannedMaterialUnitPriceId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialAssignments = await LoadPlannedMaterialAssignments(plannedMaterialUnitPriceIds, plannedOutputs.Values.ToList(), cancellationToken);

        // STEP 4: Calculate and build result
        var result = new ActualProductUnitPriceDetailDto
        {
            Id = baseData.Id,
            ProductId = baseData.ProductId,
            ProductName = baseData.ProductName ?? "",
            ProductCode = baseData.ProductCode ?? "",
            UnitOfMeasureId = baseData.UnitOfMeasureId,
            UnitOfMeasureName = baseData.UnitOfMeasureName ?? "",
            ProcessGroupId = baseData.ProcessGroupId,
            ProcessGroupCode = baseData.ProcessGroupCode ?? "",
            ProcessGroupName = baseData.ProcessGroupName ?? "",
            ProcessGroupType = baseData.ProcessGroupType,
            ProductionOutputId = baseData.ProductionOutputId,
            Outputs = actualOutputs.Select(actualOutput => CalculateOutputDto(
                actualOutput,
                plannedOutputs,
                plannedMaintainFactors,
                plannedElectricityFactors,
                plannedMaterialAssignments
            )).ToList()
        };

        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);

        return result;
    }

    #region helper

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
                f.K6AdjustmentFactorValue,
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
                    Quantity = (double)f.Quantity,
                    K6AdjustmentFactorValue = f.K6AdjustmentFactorValue,
                    OtherMaterialValue = f.OtherMaterialValue,
                    EquipmentCost = f.Equipments.Sum(m =>
                    {
                        var partCost = m.PartCosts.FirstOrDefault(c => c.StartMonth <= f.MaintainStartMonth && c.EndMonth >= f.MaintainStartMonth)?.Amount ?? 0;
                        return partCost * (m.Quantity / (double)(m.ReplacementTimeStandard * m.AverageMonthlyTunnelProduction));
                    }),
                    AdjustmentFactor = f.AdjustmentValues.Aggregate(1.0, (acc, val) => acc * val)
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
        List<Guid> materialUnitPriceIds, List<OutputRawData> plannedOutputs, CancellationToken cancellationToken)
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

    private static ActualOutputDto CalculateOutputDto(
        OutputRawData actualOutput,
        Dictionary<(DateOnly, DateOnly), OutputRawData> plannedOutputs,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<PlannedElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, List<MaterialAssignmentData>> plannedMaterialAssignments)
    {
        var adjTotalPrice = 0.0;
        if (plannedOutputs.TryGetValue((actualOutput.StartMonth, actualOutput.EndMonth), out var plannedOutput))
        {
            // Planned Material Cost
            var plannedMaterialCost = 0.0;
            if (plannedOutput.PlannedMaterialUnitPriceId.HasValue &&
                plannedMaterialAssignments.TryGetValue(plannedOutput.PlannedMaterialUnitPriceId.Value, out var pMatAssignments))
            {
                var slideCost = plannedOutput.PlannedMaterialSlideCost * plannedOutput.PlannedMaterialSlideQuantity;
                var materialTotal = pMatAssignments.Sum(m => m.Cost * m.Quantity);

                if (plannedOutput.PlannedMaterialOtherValue.HasValue)
                {
                    materialTotal += materialTotal * (plannedOutput.PlannedMaterialOtherValue.Value / 100.0);
                }

                plannedMaterialCost = (slideCost + materialTotal) * plannedOutput.PlannedMaterialStoneClamp;
            }

            // Planned Maintain Cost
            var plannedMaintainCost = plannedOutput.PlannedMaintainCostId.HasValue &&
                plannedMaintainFactors.TryGetValue(plannedOutput.PlannedMaintainCostId.Value, out var pMaintainFactors)
                ? pMaintainFactors.Sum(f => f.Quantity * f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) * f.K6AdjustmentFactorValue * f.AdjustmentFactor)
                : 0;

            // Planned Electricity Cost
            var plannedElectricityCost = plannedOutput.PlannedElectricityCostId.HasValue &&
                plannedElectricityFactors.TryGetValue(plannedOutput.PlannedElectricityCostId.Value, out var pElecFactors)
                ? pElecFactors.Sum(f => f.Quantity * f.CostPerMetre * f.AdjustmentFactor)
                : 0;

            adjTotalPrice = actualOutput.ProductionMeters * (plannedMaterialCost + plannedMaintainCost + plannedElectricityCost);
        }

        return new ActualOutputDto
        {
            Id = actualOutput.Id,
            OutputType = actualOutput.OutputType,
            StartMonth = actualOutput.StartMonth,
            EndMonth = actualOutput.EndMonth,
            ProductionMeters = actualOutput.ProductionMeters,
            TotalPrice = 0,
            AdjTotalPrice = adjTotalPrice
        };
    }

    // Helper classes for intermediate data
    private class OutputRawData
    {
        public Guid Id { get; set; }
        public OutputType OutputType { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double ProductionMeters { get; set; }
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
        public double K6AdjustmentFactorValue { get; set; }
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
    #endregion
}