using Application.Catalog.Pricing.Common;
using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;
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
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
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
                p.DepartmentId,
                DepartmentCode = p.Department != null && p.Department.Code != null ? p.Department.Code.Value : null,
                DepartmentName = p.Department != null ? p.Department.Name : null,
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
                PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // STEP 3: Batch load cost data sequentially (DbContext is not thread-safe)
        var plannedMaintainCostIds = outputs.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).ToList();
        var plannedElectricityCostIds = outputs.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).ToList();
        var plannedMaterialCostIds = outputs.Where(o => o.PlannedMaterialCostId.HasValue).Select(o => o.PlannedMaterialCostId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialCosts = await LoadPlannedMaterialCosts(plannedMaterialCostIds, cancellationToken);

        // STEP 4: Calculate and build result
        var result = new PlannedProductUnitPriceDetailDto
        {
            Id = baseData.Id,
            ProductId = baseData.ProductId,
            ProductName = baseData.ProductName ?? "",
            ProductCode = baseData.ProductCode ?? "",
            UnitOfMeasureId = baseData.UnitOfMeasureId,
            UnitOfMeasureName = baseData.UnitOfMeasureName ?? "",
            DepartmentId = baseData.DepartmentId,
            DepartmentCode = baseData.DepartmentCode ?? "",
            DepartmentName = baseData.DepartmentName ?? "",
            ProcessGroupId = baseData.ProcessGroupId,
            ProcessGroupCode = baseData.ProcessGroupCode ?? "",
            ProcessGroupName = baseData.ProcessGroupName ?? "",
            ProcessGroupType = baseData.ProcessGroupType,
            Outputs = outputs.Select(output => CalculateOutputDto(
                output,
                plannedMaintainFactors,
                plannedElectricityFactors,
                plannedMaterialCosts
            )).OrderByDescending(o => o.StartMonth).ToList()
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
                TrimmingCoefficient = f.PlannedMaintainCost.TrimmingCoefficient,
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
                    TrimmingCoefficient = f.TrimmingCoefficient,
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
                TrimmingCoefficient = f.PlannedElectricityCost.TrimmingCoefficient,
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
                        TrimmingCoefficient = f.TrimmingCoefficient,
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

    private static PlannedOutputDto CalculateOutputDto(
        PlannedOutputRawData output,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<ElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, double> plannedMaterialCosts)
    {
        var plannedMaterialCost = output.PlannedMaterialCostId.HasValue
            ? plannedMaterialCosts.GetValueOrDefault(output.PlannedMaterialCostId.Value, 0)
            : 0;

        // Calculate Planned Maintain Cost
        var plannedMaintainCost = output.PlannedMaintainCostId.HasValue &&
            plannedMaintainFactors.TryGetValue(output.PlannedMaintainCostId.Value, out var maintainFactors)
            ? maintainFactors.Sum(f => f.Quantity * f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) * f.K6AdjustmentFactorValue * f.AdjustmentFactor)
                * NormalizeTrimmingCoefficient(maintainFactors.FirstOrDefault()?.TrimmingCoefficient ?? 1)
            : 0;

        // Calculate Planned Electricity Cost
        var plannedElectricityCost = output.PlannedElectricityCostId.HasValue &&
            plannedElectricityFactors.TryGetValue(output.PlannedElectricityCostId.Value, out var elecFactors)
            ? elecFactors.Sum(f => f.Quantity * f.CostPerMetre * f.AdjustmentFactor)
                * NormalizeTrimmingCoefficient(elecFactors.FirstOrDefault()?.TrimmingCoefficient ?? 1)
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
    }

    private class MaintainFactorData
    {
        public double Quantity { get; set; }
        public double TrimmingCoefficient { get; set; }
        public double K6AdjustmentFactorValue { get; set; }
        public double? OtherMaterialValue { get; set; }
        public double EquipmentCost { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    private class ElectricityFactorData
    {
        public double Quantity { get; set; }
        public double TrimmingCoefficient { get; set; }
        public double CostPerMetre { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    private static double NormalizeTrimmingCoefficient(double trimmingCoefficient)
    {
        if (trimmingCoefficient <= 0)
        {
            return 1;
        }

        return trimmingCoefficient > 1 ? trimmingCoefficient / 100 : trimmingCoefficient;
    }
    #endregion
}

