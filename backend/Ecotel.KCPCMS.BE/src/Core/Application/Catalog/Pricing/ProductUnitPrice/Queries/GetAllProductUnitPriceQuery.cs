using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ProductUnitPrice.Queries;

public record class GetAllProductUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, ProductUnitPriceScenarioType ScenarioType) : IRequest<PaginationResponse<ProductUnitPriceDto>>;

public class GetAllUnitPriceQueryHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<GetAllProductUnitPriceQuery, PaginationResponse<ProductUnitPriceDto>>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<PlannedElectricityCostAdjustmentFactor> _plannedElectricityFactorRepository = unitOfWork.GetRepository<PlannedElectricityCostAdjustmentFactor>();

    public async Task<PaginationResponse<ProductUnitPriceDto>> Handle(GetAllProductUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheSignalKey}:All:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}:{request.ScenarioType}";

        var cachedResult = await cacheService.GetAsync<PaginationResponse<ProductUnitPriceDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var searchTerm = (request.Search ?? "").Trim();

        // STEP 1: Get paginated ProductUnitPrice base data (lightweight query)
        var baseQuery = _productUnitPriceRepository.GetAll()
            .Where(m => m.ScenarioType == request.ScenarioType)
            .Where(m => m.Product != null &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                EF.Functions.Like(m.Product.Name, $"%{searchTerm}%") ||
                EF.Functions.Like(m.Product.Code.Value, $"%{searchTerm}%")));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var paginatedQuery = baseQuery
            .OrderBy(m => m.Product!.Name)
            .Select(m => new ProductUnitPriceBaseData
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductName = m.Product!.Name,
                ProductCode = m.Product.Code.Value,
                ProcessGroupId = m.Product.ProcessGroupId,
                ProcessGroupCode = m.Product.ProcessGroup!.Code.Value,
                ProcessGroupType = m.Product.ProcessGroup!.Type,
                UnitOfMeasureId = m.UnitOfMeasureId,
                UnitOfMeasureName = m.UnitOfMeasure != null ? m.UnitOfMeasure.Name : null
            });

        if (!request.IgnorePagination)
        {
            paginatedQuery = paginatedQuery
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        var baseDataList = await paginatedQuery.AsNoTracking().ToListAsync(cancellationToken);
        var productUnitPriceIds = baseDataList.Select(b => b.Id).ToList();
        var productIds = baseDataList.Select(b => b.ProductId).ToList();

        if (!productUnitPriceIds.Any())
        {
            return new PaginationResponse<ProductUnitPriceDto>(
                new List<ProductUnitPriceDto>(),
                totalCount,
                request.PageIndex,
                request.PageSize);
        }

        // STEP 2: Get all outputs for these ProductUnitPrices
        var adjustmentByProductId = baseDataList.ToDictionary(x => x.ProductId, x => x.Id);

        var planUomByProductId = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Plan && productIds.Contains(p.ProductId))
            .Select(p => new { p.ProductId, p.UnitOfMeasureId, UnitOfMeasureName = p.UnitOfMeasure != null ? p.UnitOfMeasure.Name : null })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        if (request.ScenarioType == ProductUnitPriceScenarioType.Adjustment)
        {
            for (int i = 0; i < baseDataList.Count; i++)
            {
                var item = baseDataList[i];
                if (planUomByProductId.TryGetValue(item.ProductId, out var planUom))
                {
                    item.UnitOfMeasureId = planUom.UnitOfMeasureId;
                    item.UnitOfMeasureName = planUom.UnitOfMeasureName;
                }
            }
        }

        var outputsQuery = request.ScenarioType == ProductUnitPriceScenarioType.Adjustment
            ? _outputRepository.GetAll()
                .Where(o => o.OutputType == OutputType.PlanOutput
                    && o.ProductUnitPrice != null
                    && o.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan
                    && productIds.Contains(o.ProductUnitPrice.ProductId))
            : _outputRepository.GetAll()
                .Where(o => o.OutputType == OutputType.PlanOutput && productUnitPriceIds.Contains(o.ProductUnitPriceId));

        var outputsRaw = await outputsQuery
            .Select(o => new OutputData
            {
                ProductUnitPriceId = request.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                    ? adjustmentByProductId[o.ProductUnitPrice!.ProductId]
                    : o.ProductUnitPriceId,
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

        var outputs = outputsRaw;

        var outputsByProductUnitPrice = outputs.GroupBy(o => o.ProductUnitPriceId).ToDictionary(g => g.Key, g => g.ToList());

        // STEP 3: Batch load planned cost data
        var allPlannedOutputs = outputs.Where(o => o.OutputType == OutputType.PlanOutput).ToList();
        var plannedMaintainCostIds = allPlannedOutputs.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).Distinct().ToList();
        var plannedElectricityCostIds = allPlannedOutputs.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).Distinct().ToList();
        var plannedMaterialUnitPriceIds = allPlannedOutputs.Where(o => o.PlannedMaterialUnitPriceId.HasValue).Select(o => o.PlannedMaterialUnitPriceId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialAssignments = await LoadPlannedMaterialAssignments(plannedMaterialUnitPriceIds, allPlannedOutputs, cancellationToken);

        // STEP 4: Load ProductionOutput data for ActualOutput
        var adjustmentIdsByProductId = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Adjustment && productIds.Contains(p.ProductId))
            .Select(p => new { p.ProductId, p.Id })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ProductId, x => x.Id, cancellationToken);

        var adjustmentIds = adjustmentIdsByProductId.Values.Distinct().ToList();

        var adjustmentProductionOutputsRaw = await _productUnitPriceRepository.GetAll()
            .Where(p => adjustmentIds.Contains(p.Id))
            .SelectMany(p => p.ProductUnitPriceProductionOutputs.Select(link => new ProductionOutputData
            {
                ProductUnitPriceId = p.Id,
                ProductionMeters = link.ProductionMeters,
                StartMonth = link.ProductionOutput!.StartMonth,
                EndMonth = link.ProductionOutput.EndMonth
            }))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var adjustmentProductionByAdjustmentId = adjustmentProductionOutputsRaw
            .GroupBy(po => po.ProductUnitPriceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // STEP 5: Calculate and build result
        var listData = baseDataList.Select(baseData =>
        {
            var productOutputs = outputsByProductUnitPrice.GetValueOrDefault(baseData.Id) ?? new List<OutputData>();
            var filteredOutputs = productOutputs
                .Where(o => o.StartMonth != DateOnly.MinValue && o.EndMonth != DateOnly.MinValue)
                .ToList();

            var plannedTotalCost = CalculatePlannedTotalCost(productOutputs, plannedMaintainFactors, plannedElectricityFactors, plannedMaterialAssignments);

            var adjustmentIdForBase = request.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                ? baseData.Id
                : adjustmentIdsByProductId.GetValueOrDefault(baseData.ProductId);

            var productionOutputs = adjustmentIdForBase == Guid.Empty
                ? new List<ProductionOutputData>()
                : adjustmentProductionByAdjustmentId.GetValueOrDefault(adjustmentIdForBase) ?? new List<ProductionOutputData>();

            var adjustmentTotalCost = CalculateAdjustmentTotalCost(productOutputs, productionOutputs, plannedMaintainFactors, plannedElectricityFactors, plannedMaterialAssignments);

            double totalProductionMeters;
            DateOnly? startMonth;
            DateOnly? endMonth;

            if (request.ScenarioType == ProductUnitPriceScenarioType.Plan)
            {
                totalProductionMeters = filteredOutputs.Sum(o => o.ProductionMeters);
                startMonth = filteredOutputs.Any() ? filteredOutputs.Min(o => o.StartMonth) : null;
                endMonth = filteredOutputs.Any() ? filteredOutputs.Max(o => o.EndMonth) : null;
            }
            else
            {
                totalProductionMeters = productionOutputs.Sum(po => po.ProductionMeters);
                startMonth = productionOutputs.Any() ? productionOutputs.Min(o => o.StartMonth) : null;
                endMonth = productionOutputs.Any() ? productionOutputs.Max(o => o.EndMonth) : null;
            }

            double actualTotalCost = 0;

            return new ProductUnitPriceDto
            {
                Id = baseData.Id,
                ProductId = baseData.ProductId,
                ProductName = baseData.ProductName,
                ProductCode = baseData.ProductCode,
                ProcessGroupId = baseData.ProcessGroupId,
                ProcessGroupCode = baseData.ProcessGroupCode,
                ProcessGroupType = baseData.ProcessGroupType,
                UnitOfMeasureId = baseData.UnitOfMeasureId ?? Guid.Empty,
                UnitOfMeasureName = baseData.UnitOfMeasureName,
                StartMonth = startMonth,
                EndMonth = endMonth,
                TotalProductionMeters = totalProductionMeters,
                PlannedTotalCost = plannedTotalCost,
                ActualTotalCost = actualTotalCost,
                AdjustmentTotalCost = adjustmentTotalCost
            };
        }).ToList();

        var result = new PaginationResponse<ProductUnitPriceDto>(listData, totalCount, request.PageIndex, request.PageSize);

        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);

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
        List<Guid> materialUnitPriceIds, List<OutputData> plannedOutputs, CancellationToken cancellationToken)
    {
        if (!materialUnitPriceIds.Any())
        {
            return new Dictionary<Guid, List<MaterialAssignmentData>>();
        }

        var outputsByMaterialUnitPrice = plannedOutputs
            .Where(o => o.PlannedMaterialUnitPriceId.HasValue)
            .GroupBy(o => o.PlannedMaterialUnitPriceId!.Value)
            .ToDictionary(g => g.Key, g => g.First().StartMonth);

        var unitPriceData = await _productUnitPriceRepository.GetAll()
            .SelectMany(p => p.PlannedMaterialCosts)
            .Where(c => materialUnitPriceIds.Contains(c.MaterialUnitPriceId))
            .Select(c => new
            {
                c.MaterialUnitPriceId,
                c.Output.StartMonth,
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

                    if (item.MaterialStartMonth <= item.StartMonth && item.MaterialEndMonth >= item.StartMonth)
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

    #endregion

    #region Calculate Methods

    private static double CalculatePlannedTotalCost(
        List<OutputData> outputs,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<PlannedElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, List<MaterialAssignmentData>> plannedMaterialAssignments)
    {
        var plannedOutputs = outputs.Where(o => o.OutputType == OutputType.PlanOutput).ToList();
        if (!plannedOutputs.Any())
        {
            return 0;
        }

        return plannedOutputs.Sum(output =>
        {
            var materialCost = CalculatePlannedMaterialCost(output, plannedMaterialAssignments);
            var maintainCost = CalculateMaintainCost(output.PlannedMaintainCostId, plannedMaintainFactors);
            var electricityCost = CalculatePlannedElectricityCost(output.PlannedElectricityCostId, plannedElectricityFactors);
            return output.ProductionMeters * (materialCost + maintainCost + electricityCost);
        });
    }

    private static double CalculateAdjustmentTotalCost(
        List<OutputData> outputs,
        List<ProductionOutputData> productionOutputs,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<PlannedElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, List<MaterialAssignmentData>> plannedMaterialAssignments)
    {
        var plannedOutputsDict = outputs
            .Where(o => o.OutputType == OutputType.PlanOutput)
            .ToDictionary(o => (o.StartMonth, o.EndMonth), o => o);

        if (!productionOutputs.Any() || !plannedOutputsDict.Any())
        {
            return 0;
        }

        return productionOutputs.Sum(productionOutput =>
        {
            // Find matching PlannedOutput by date range
            if (!plannedOutputsDict.TryGetValue((productionOutput.StartMonth, productionOutput.EndMonth), out var plannedOutput))
            {
                return 0;
            }

            var materialCost = CalculatePlannedMaterialCost(plannedOutput, plannedMaterialAssignments);
            var maintainCost = CalculateMaintainCost(plannedOutput.PlannedMaintainCostId, plannedMaintainFactors);
            var electricityCost = CalculatePlannedElectricityCost(plannedOutput.PlannedElectricityCostId, plannedElectricityFactors);
            return productionOutput.ProductionMeters * (materialCost + maintainCost + electricityCost);
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

        return factors.Sum(f => f.Quantity * f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) * f.K6AdjustmentFactorValue * f.AdjustmentFactor);
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

    private class ProductUnitPriceBaseData
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public Guid ProcessGroupId { get; set; }
        public string ProcessGroupCode { get; set; } = "";
        public ProcessGroupType ProcessGroupType { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string? UnitOfMeasureName { get; set; }
    }

    private class OutputData
    {
        public Guid ProductUnitPriceId { get; set; }
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

    private class PlannedElectricityFactorData
    {
        public double Quantity { get; set; }
        public double CostPerMetre { get; set; }
        public double AdjustmentFactor { get; set; }
    }

    private class ProductionOutputData
    {
        public Guid ProductUnitPriceId { get; set; }
        public double ProductionMeters { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
    }

    #endregion
}