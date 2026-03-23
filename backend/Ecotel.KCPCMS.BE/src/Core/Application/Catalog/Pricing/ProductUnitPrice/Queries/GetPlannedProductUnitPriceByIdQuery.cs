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

public record GetPlannedProductUnitPriceByIdQuery(DefaultIdType Id) : IRequest<PlannedProductUnitPriceDetailDto>;

public class GetPlannedProductUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<GetPlannedProductUnitPriceByIdQuery, PlannedProductUnitPriceDetailDto>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<PlannedElectricityCostAdjustmentFactor> _plannedElectricityFactorRepository = unitOfWork.GetRepository<PlannedElectricityCostAdjustmentFactor>();

    public async Task<PlannedProductUnitPriceDetailDto> Handle(GetPlannedProductUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheSignalKey}:Planned:{request.Id}";

        var cachedResult = await cacheService.GetAsync<PlannedProductUnitPriceDetailDto>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        // STEP 1: Get base ProductUnitPrice info
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
                ProcessGroupType = p.Product.ProcessGroup.Type
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (baseData == null)
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        // STEP 2: Get all planned outputs with pre-calculated material data
        var outputs = await _outputRepository.GetAll()
            .Where(o => o.ProductUnitPriceId == request.Id && o.OutputType == OutputType.PlanOutput)
            .Select(o => new PlannedOutputRawData
            {
                Id = o.Id,
                OutputType = o.OutputType,
                StartMonth = o.StartMonth,
                EndMonth = o.EndMonth,
                ProductionMeters = o.ProductionMeters,
                PlannedMaterialCostId = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.Id : (Guid?)null,
                PlannedMaintainCostId = o.PlannedMaintainCost != null ? o.PlannedMaintainCost.Id : (Guid?)null,
                PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null,
                // Pre-calculated Planned Material Cost fields
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

        // STEP 3: Batch load cost data sequentially (DbContext is not thread-safe)
        var plannedMaintainCostIds = outputs.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).ToList();
        var plannedElectricityCostIds = outputs.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).ToList();
        var plannedMaterialUnitPriceIds = outputs.Where(o => o.PlannedMaterialUnitPriceId.HasValue).Select(o => o.PlannedMaterialUnitPriceId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialAssignments = await LoadPlannedMaterialAssignments(plannedMaterialUnitPriceIds, outputs, cancellationToken);

        // STEP 4: Calculate and build result
        var result = new PlannedProductUnitPriceDetailDto
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
            Outputs = outputs.Select(output => CalculateOutputDto(
                output,
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
                    K6AdjustmentFactorValue = f.K6AdjustmentFactorValue,
                    OtherMaterialValue = f.OtherMaterialValue,
                    EquipmentCost = f.Equipments.Sum(m =>
                    {
                        var partCost = m.PartCosts.FirstOrDefault(c => c.StartMonth <= f.MaintainStartMonth && c.EndMonth >= f.MaintainStartMonth)?.Amount ?? 0;
                        return partCost * (m.Quantity / (double)(m.ReplacementTimeStandard * m.AverageMonthlyTunnelProduction));
                    }),
                    AdjustmentFactor = f.AdjustmentValues.Any() ? f.AdjustmentValues.Aggregate(1.0, (acc, val) => acc * val) : 1.0
                }).ToList());
    }

    private async Task<Dictionary<Guid, List<ElectricityFactorData>>> LoadPlannedElectricityFactors(
        List<Guid> costIds, CancellationToken cancellationToken)
    {
        if (!costIds.Any())
        {
            return new Dictionary<Guid, List<ElectricityFactorData>>();
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

                    return new ElectricityFactorData
                    {
                        Quantity = (double)f.Quantity,
                        CostPerMetre = costPerMetre,
                        AdjustmentFactor = f.AdjustmentValues.Any() ? f.AdjustmentValues.Aggregate(1.0, (acc, val) => acc * val) : 1.0
                    };
                }).ToList());
    }

    private async Task<Dictionary<Guid, List<MaterialAssignmentData>>> LoadPlannedMaterialAssignments(
        List<Guid> materialUnitPriceIds, List<PlannedOutputRawData> outputs, CancellationToken cancellationToken)
    {
        if (!materialUnitPriceIds.Any())
        {
            return new Dictionary<Guid, List<MaterialAssignmentData>>();
        }

        var outputsByStartMonth = outputs
            .Where(o => o.PlannedMaterialUnitPriceId.HasValue)
            .GroupBy(o => o.PlannedMaterialUnitPriceId!.Value)
            .ToDictionary(g => g.Key, g => g.First().StartMonth);

        var unitPriceData = await _productUnitPriceRepository.GetAll()
            .SelectMany(p => p.PlannedMaterialCosts)
            .Where(c => materialUnitPriceIds.Contains(c.MaterialUnitPriceId))
            .Select(c => new
            {
                c.MaterialUnitPriceId,
                EffectiveMonth = c.Output.StartMonth,
                c.MaterialUnitPrice.OtherMaterialvalue,
                MaterialStartMonth = c.MaterialUnitPrice.StartMonth,
                MaterialEndMonth = c.MaterialUnitPrice.EndMonth,
                AssignmentCodesTotalPrice = c.MaterialUnitPrice.MaterialUnitPriceAssignmentCodes
                    .Sum(a => a.TotalPrice)
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return unitPriceData.GroupBy(a => a.MaterialUnitPriceId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var item = g.First();
                    double cost = 0;

                    if (item.MaterialStartMonth <= item.EffectiveMonth
                        && item.MaterialEndMonth >= item.EffectiveMonth)
                    {
                        cost = item.AssignmentCodesTotalPrice * (1 + item.OtherMaterialvalue / 100);
                    }

                    return new List<MaterialAssignmentData>
                    {
                new MaterialAssignmentData
                {
                    Quantity = 1,
                    Cost = cost
                }
                    };
                });
    }

    private static PlannedOutputDto CalculateOutputDto(
        PlannedOutputRawData output,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<ElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, List<MaterialAssignmentData>> plannedMaterialAssignments)
    {
        // Calculate Planned Material Cost
        var plannedMaterialCost = 0.0;
        if (output.PlannedMaterialUnitPriceId.HasValue &&
            plannedMaterialAssignments.TryGetValue(output.PlannedMaterialUnitPriceId.Value, out var matAssignments))
        {
            var slideCost = output.PlannedMaterialSlideCost * output.PlannedMaterialSlideQuantity;
            var materialTotal = matAssignments.Sum(m => m.Cost * m.Quantity);

            plannedMaterialCost = (slideCost + materialTotal) * output.PlannedMaterialStoneClamp;
        }

        // Calculate Planned Maintain Cost
        var plannedMaintainCost = output.PlannedMaintainCostId.HasValue &&
            plannedMaintainFactors.TryGetValue(output.PlannedMaintainCostId.Value, out var maintainFactors)
            ? maintainFactors.Sum(f => f.Quantity * f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) * f.K6AdjustmentFactorValue * f.AdjustmentFactor)
            : 0;

        // Calculate Planned Electricity Cost
        var plannedElectricityCost = output.PlannedElectricityCostId.HasValue &&
            plannedElectricityFactors.TryGetValue(output.PlannedElectricityCostId.Value, out var elecFactors)
            ? elecFactors.Sum(f => f.Quantity * f.CostPerMetre * f.AdjustmentFactor)
            : 0;

        // Calculate Total Price
        var totalPrice = output.ProductionMeters * (plannedMaterialCost + plannedMaintainCost + plannedElectricityCost);

        return new PlannedOutputDto
        {
            Id = output.Id,
            OutputType = output.OutputType,
            StartMonth = output.StartMonth,
            EndMonth = output.EndMonth,
            PlannedElectricityCostId = output.PlannedElectricityCostId,
            PlannedMaintainCostId = output.PlannedMaintainCostId,
            PlannedMaterialCostId = output.PlannedMaterialCostId,
            ProductionMeters = output.ProductionMeters,
            TotalPrice = totalPrice
        };
    }

    // Helper classes for intermediate data
    private class PlannedOutputRawData
    {
        public Guid Id { get; set; }
        public OutputType OutputType { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double ProductionMeters { get; set; }
        public Guid? PlannedMaterialCostId { get; set; }
        public Guid? PlannedMaintainCostId { get; set; }
        public Guid? PlannedElectricityCostId { get; set; }
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

    private class ElectricityFactorData
    {
        public double Quantity { get; set; }
        public double CostPerMetre { get; set; }
        public double AdjustmentFactor { get; set; }
    }
    #endregion
}