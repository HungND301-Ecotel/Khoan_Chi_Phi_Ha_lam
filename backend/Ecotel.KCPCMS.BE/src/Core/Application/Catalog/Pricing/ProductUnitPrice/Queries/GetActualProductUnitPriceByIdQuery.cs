using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.Pricing.Common;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;
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
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _lowValuePerishableSupplyUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
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
                p.DepartmentId,
                DepartmentCode = p.Department != null && p.Department.Code != null ? p.Department.Code.Value : null,
                DepartmentName = p.Department != null ? p.Department.Name : null,
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
                PlanAshContent = o.PlanAshContent,
                PlannedMaterialCostId = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.Id : (Guid?)null,
                PlannedMaintainCostId = o.PlannedMaintainCost != null ? o.PlannedMaintainCost.Id : (Guid?)null,
                PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var actualOutputs = outputs.Where(o => o.OutputType == OutputType.ActualOutput).ToList();
        var plannedOutputs = outputs.Where(o => o.OutputType == OutputType.PlanOutput).ToDictionary(o => (o.StartMonth, o.EndMonth));

        // STEP 3: Load planned cost data
        var plannedMaintainCostIds = plannedOutputs.Values.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).ToList();
        var plannedElectricityCostIds = plannedOutputs.Values.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).ToList();
        var plannedMaterialCostIds = plannedOutputs.Values.Where(o => o.PlannedMaterialCostId.HasValue).Select(o => o.PlannedMaterialCostId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialCosts = await LoadPlannedMaterialCosts(plannedMaterialCostIds, cancellationToken);

        // STEP 4: Calculate and build result
        var result = new ActualProductUnitPriceDetailDto
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
            ProductionOutputId = baseData.ProductionOutputId,
            Outputs = actualOutputs.Select(actualOutput => CalculateOutputDto(
                actualOutput,
                plannedOutputs,
                plannedMaintainFactors,
                plannedElectricityFactors,
                plannedMaterialCosts
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
                TrimmingCoefficient = f.PlannedMaintainCost.TrimmingCoefficient,
                f.Quantity,
                f.K6AdjustmentFactorValue,
                MaintainUnitPriceType = f.MaintainUnitPrice.Type,
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
                    .Select(d => d.CustomValue
                        ?? (d.AdjustmentFactorDescription != null
                            ? d.AdjustmentFactorDescription.MaintenanceAdjustmentValue
                            : null)
                        ?? 1.0).ToList()
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
                        return MaintainCostCalculator.CalculateMaterialCostPerMetre(
                            partCost,
                            m.Quantity,
                            m.ReplacementTimeStandard,
                            m.AverageMonthlyTunnelProduction,
                            f.MaintainUnitPriceType);
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
                TrimmingCoefficient = f.PlannedElectricityCost.TrimmingCoefficient,
                f.Quantity,
                f.ElectricityUnitPriceEquipment,
                AdjustmentValues = f.PlannedElectricityCostAdjustmentFactorDescriptions
                    .Select(d => d.CustomValue
                        ?? (d.AdjustmentFactorDescription != null
                            ? d.AdjustmentFactorDescription.ElectricityAdjustmentValue
                            : null)
                        ?? 1.0).ToList()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return data.GroupBy(f => f.PlannedElectricityCostId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f =>
                {
                    var costPerMetre = f.ElectricityUnitPriceEquipment.GetRoundedElectricityCostPerMetres();

                    return new PlannedElectricityFactorData
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
            .Include(c => c.ProductUnitPrice).ThenInclude(p => p.Product)
            .Include(c => c.SlideUnitPriceAssignmentCode)
            .Include(c => c.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes)
            .Include(c => c.MaterialUnitPrice).ThenInclude(m => m.MaterialUnitPriceAssignmentCodes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dependencies = await PlannedMaterialCostCalculationDependencyLoader.LoadAsync(
            plannedMaterialCosts,
            _tunnelMaterialUnitPriceRepository,
            _lowValuePerishableSupplyUnitPriceRepository,
            cancellationToken);

        return PlannedMaterialCostCalculator.CalculateUnitPricesByCostId(
            plannedMaterialCosts,
            dependencies.TunnelMaterialUnitPrices,
            dependencies.LowValuePerishableSupplyUnitPrices);
    }

    private static ActualOutputDto CalculateOutputDto(
        OutputRawData actualOutput,
        Dictionary<(DateOnly, DateOnly), OutputRawData> plannedOutputs,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<PlannedElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, double> plannedMaterialCosts)
    {
        var adjTotalPrice = 0.0;
        var plannedOutput = plannedOutputs.Values
            .Where(o => o.StartMonth <= actualOutput.StartMonth && o.EndMonth >= actualOutput.EndMonth)
            .OrderBy(o => o.StartMonth)
            .ThenBy(o => o.EndMonth)
            .FirstOrDefault();
        if (plannedOutput != null)
        {
            // Planned Material Cost
            var plannedMaterialCost = plannedOutput.PlannedMaterialCostId.HasValue
                ? plannedMaterialCosts.GetValueOrDefault(plannedOutput.PlannedMaterialCostId.Value, 0)
                : 0;

            // Planned Maintain Cost
            var plannedMaintainCost = plannedOutput.PlannedMaintainCostId.HasValue &&
                plannedMaintainFactors.TryGetValue(plannedOutput.PlannedMaintainCostId.Value, out var pMaintainFactors)
                ? pMaintainFactors.Sum(f => f.Quantity * f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) * f.K6AdjustmentFactorValue * f.AdjustmentFactor)
                    * NormalizeTrimmingCoefficient(pMaintainFactors.FirstOrDefault()?.TrimmingCoefficient ?? 1)
                : 0;

            // Planned Electricity Cost
            var plannedElectricityCost = plannedOutput.PlannedElectricityCostId.HasValue &&
                plannedElectricityFactors.TryGetValue(plannedOutput.PlannedElectricityCostId.Value, out var pElecFactors)
                ? pElecFactors.Sum(f => f.Quantity * f.CostPerMetre * f.AdjustmentFactor)
                    * NormalizeTrimmingCoefficient(pElecFactors.FirstOrDefault()?.TrimmingCoefficient ?? 1)
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
            PlanAshContent = plannedOutput?.PlanAshContent ?? 0,
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
        public double PlanAshContent { get; set; }
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

    private class PlannedElectricityFactorData
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

