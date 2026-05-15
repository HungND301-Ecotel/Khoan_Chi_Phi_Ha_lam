using Application.Catalog.Pricing.Common;
using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ProductUnitPrice.Queries;

public record class GetAllProductUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, ProductUnitPriceScenarioType ScenarioType, Guid? DepartmentId = null) : IRequest<PaginationResponse<ProductUnitPriceDto>>;

public class GetAllUnitPriceQueryHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<GetAllProductUnitPriceQuery, PaginationResponse<ProductUnitPriceDto>>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _lowValuePerishableSupplyUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<PlannedElectricityCostAdjustmentFactor> _plannedElectricityFactorRepository = unitOfWork.GetRepository<PlannedElectricityCostAdjustmentFactor>();
    private readonly IWriteRepository<AkFactorConfig> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfig>();

    public async Task<PaginationResponse<ProductUnitPriceDto>> Handle(GetAllProductUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheSignalKey}:All:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}:{request.ScenarioType}:{request.DepartmentId}";

        var cachedResult = await cacheService.GetAsync<PaginationResponse<ProductUnitPriceDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var searchTerm = (request.Search ?? "").Trim();

        // STEP 1: Get paginated ProductUnitPrice base data (lightweight query)
        var baseQuery = _productUnitPriceRepository.GetAll()
            .Where(m => m.ScenarioType == request.ScenarioType)
            .Where(m => !request.DepartmentId.HasValue || m.DepartmentId == request.DepartmentId)
            .Where(m => m.Product != null &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                EF.Functions.Like(m.Product.Name, $"%{searchTerm}%") ||
                EF.Functions.Like(m.Product.Code.Value, $"%{searchTerm}%") ||
                (m.Department != null && (
                    EF.Functions.Like(m.Department.Name, $"%{searchTerm}%") ||
                    (m.Department.Code != null && EF.Functions.Like(m.Department.Code.Value, $"%{searchTerm}%"))
                ))));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var baseDataQuery = baseQuery
            .Select(m => new ProductUnitPriceBaseData
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductName = m.Product!.Name,
                ProductCode = m.Product.Code.Value,
                ProcessGroupId = m.Product.ProcessGroupId,
                ProcessGroupCode = m.Product.ProcessGroup!.FixedKey != null
                    ? m.Product.ProcessGroup.FixedKey.Key
                    : string.Empty,
                ProcessGroupName = m.Product.ProcessGroup.Name,
                ProcessGroupType = m.Product.ProcessGroup!.FixedKey != null
                    ? m.Product.ProcessGroup.FixedKey.Type.ToProcessGroupType()
                    : ProcessGroupType.None,
                UnitOfMeasureId = m.UnitOfMeasureId,
                UnitOfMeasureName = m.UnitOfMeasure != null ? m.UnitOfMeasure.Name : null,
                DepartmentId = m.DepartmentId,
                DepartmentCode = m.Department != null && m.Department.Code != null ? m.Department.Code.Value : null,
                DepartmentName = m.Department != null ? m.Department.Name : null
            });

        var allBaseData = await baseDataQuery.AsNoTracking().ToListAsync(cancellationToken);
        var orderedBaseData = allBaseData
            .OrderByCodeNatural(m => m.ProductCode)
            .ThenBy(m => m.ProductName);

        var baseDataList = request.IgnorePagination
            ? orderedBaseData.ToList()
            : orderedBaseData
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        var productUnitPriceIds = baseDataList.Select(b => b.Id).ToList();
        var productIds = baseDataList.Select(b => b.ProductId).Distinct().ToList();

        if (!productUnitPriceIds.Any())
        {
            return new PaginationResponse<ProductUnitPriceDto>(
                new List<ProductUnitPriceDto>(),
                totalCount,
                request.PageIndex,
                request.PageSize);
        }

        // STEP 2: Get all outputs for these ProductUnitPrices
        var outputs = new List<OutputData>();
        if (request.ScenarioType == ProductUnitPriceScenarioType.Adjustment)
        {
            var adjustmentByProductDepartment = baseDataList
                .GroupBy(x => (x.ProductId, x.DepartmentId))
                .ToDictionary(g => g.Key, g => g.First().Id);

            var planOutputsRaw = await _outputRepository.GetAll()
                .Where(o => o.OutputType == OutputType.PlanOutput
                    && o.ProductUnitPrice != null
                    && o.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan
                    && productIds.Contains(o.ProductUnitPrice.ProductId))
                .Select(o => new
                {
                    o.Id,
                    o.StartMonth,
                    o.EndMonth,
                    o.ProductionMeters,
                    o.PlanAshContent,
                    ProductId = o.ProductUnitPrice!.ProductId,
                    DepartmentId = o.ProductUnitPrice.DepartmentId,
                    ProcessGroupType = o.ProductUnitPrice.Product.ProcessGroup!.FixedKey != null
                        ? o.ProductUnitPrice.Product.ProcessGroup.FixedKey.Type.ToProcessGroupType()
                        : ProcessGroupType.None,
                    PlannedMaterialCostId = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.Id : (Guid?)null,
                    PlannedMaintainCostId = o.PlannedMaintainCost != null ? o.PlannedMaintainCost.Id : (Guid?)null,
                    PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            outputs = planOutputsRaw
                .Where(o => adjustmentByProductDepartment.ContainsKey((o.ProductId, o.DepartmentId)))
                .Select(o => new OutputData
                {
                    ProductUnitPriceId = adjustmentByProductDepartment[(o.ProductId, o.DepartmentId)],
                    Id = o.Id,
                    OutputType = OutputType.PlanOutput,
                    StartMonth = o.StartMonth,
                    EndMonth = o.EndMonth,
                    ProductionMeters = o.ProductionMeters,
                    PlanAshContent = o.PlanAshContent,
                    ProcessGroupType = o.ProcessGroupType,
                    PlannedMaterialCostId = o.PlannedMaterialCostId,
                    PlannedMaintainCostId = o.PlannedMaintainCostId,
                    PlannedElectricityCostId = o.PlannedElectricityCostId
                })
                .ToList();
        }
        else
        {
            outputs = await _outputRepository.GetAll()
                .Where(o => o.OutputType == OutputType.PlanOutput && productUnitPriceIds.Contains(o.ProductUnitPriceId))
                .Select(o => new OutputData
                {
                    ProductUnitPriceId = o.ProductUnitPriceId,
                    Id = o.Id,
                    OutputType = o.OutputType,
                    StartMonth = o.StartMonth,
                    EndMonth = o.EndMonth,
                    ProductionMeters = o.ProductionMeters,
                    PlanAshContent = o.PlanAshContent,
                    ProcessGroupType = o.ProductUnitPrice.Product.ProcessGroup!.FixedKey != null
                        ? o.ProductUnitPrice.Product.ProcessGroup.FixedKey.Type.ToProcessGroupType()
                        : ProcessGroupType.None,
                    PlannedMaterialCostId = o.PlannedMaterialCost != null ? o.PlannedMaterialCost.Id : (Guid?)null,
                    PlannedMaintainCostId = o.PlannedMaintainCost != null ? o.PlannedMaintainCost.Id : (Guid?)null,
                    PlannedElectricityCostId = o.PlannedElectricityCost != null ? o.PlannedElectricityCost.Id : (Guid?)null
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        var outputsByProductUnitPrice = outputs.GroupBy(o => o.ProductUnitPriceId).ToDictionary(g => g.Key, g => g.ToList());

        // STEP 3: Batch load planned cost data
        var allPlannedOutputs = outputs.Where(o => o.OutputType == OutputType.PlanOutput).ToList();
        var plannedMaintainCostIds = allPlannedOutputs.Where(o => o.PlannedMaintainCostId.HasValue).Select(o => o.PlannedMaintainCostId!.Value).Distinct().ToList();
        var plannedElectricityCostIds = allPlannedOutputs.Where(o => o.PlannedElectricityCostId.HasValue).Select(o => o.PlannedElectricityCostId!.Value).Distinct().ToList();
        var plannedMaterialCostIds = allPlannedOutputs.Where(o => o.PlannedMaterialCostId.HasValue).Select(o => o.PlannedMaterialCostId!.Value).Distinct().ToList();

        var plannedMaintainFactors = await LoadPlannedMaintainFactors(plannedMaintainCostIds, cancellationToken);
        var plannedElectricityFactors = await LoadPlannedElectricityFactors(plannedElectricityCostIds, cancellationToken);
        var plannedMaterialCosts = await LoadPlannedMaterialCosts(plannedMaterialCostIds, cancellationToken);
        var processGroupIds = baseDataList.Select(x => x.ProcessGroupId).Distinct().ToList();
        var akConfigs = await _akFactorConfigRepository.GetAll()
            .Where(x => processGroupIds.Contains(x.ProcessGroupId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var akConfigsByProcessGroup = akConfigs
            .GroupBy(x => x.ProcessGroupId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // STEP 4: Load ProductionOutput data for ActualOutput
        var baseDepartments = baseDataList.Select(x => x.DepartmentId).Distinct().ToList();
        var adjustmentRows = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                && productIds.Contains(p.ProductId)
                && baseDepartments.Contains(p.DepartmentId))
            .Select(p => new { p.ProductId, p.DepartmentId, p.Id })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var adjustmentIdsByProductDepartment = adjustmentRows
            .GroupBy(x => (x.ProductId, x.DepartmentId))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var adjustmentIds = adjustmentIdsByProductDepartment.Values.Distinct().ToList();

        var adjustmentProductionOutputsRaw = await _productUnitPriceRepository.GetAll()
            .Where(p => adjustmentIds.Contains(p.Id))
            .SelectMany(p => p.ProductUnitPriceProductionOutputs.Select(link => new ProductionOutputData
            {
                ProductUnitPriceId = p.Id,
                ProductionMeters = link.ProductionMeters,
                StartMonth = link.ProductionOutput!.StartMonth,
                EndMonth = link.ProductionOutput.EndMonth,
                ActualAshContent = link.ProductionOutput.ProductionOutputProcessGroups
                    .SelectMany(g => g.ProductionOutputProducts)
                    .Where(pp => pp.ProductId == p.ProductId)
                    .Select(pp => (double?)pp.ActualAshContent)
                    .FirstOrDefault() ?? 0
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

            var plannedTotalCost = CalculatePlannedTotalCost(productOutputs, plannedMaintainFactors, plannedElectricityFactors, plannedMaterialCosts);

            var adjustmentIdForBase = request.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                ? baseData.Id
                : adjustmentIdsByProductDepartment.GetValueOrDefault((baseData.ProductId, baseData.DepartmentId));

            var productionOutputs = adjustmentIdForBase == Guid.Empty
                ? new List<ProductionOutputData>()
                : adjustmentProductionByAdjustmentId.GetValueOrDefault(adjustmentIdForBase) ?? new List<ProductionOutputData>();

            var processGroupAkConfigs = akConfigsByProcessGroup.GetValueOrDefault(baseData.ProcessGroupId) ?? new List<AkFactorConfig>();
            var adjustmentTotalCost = CalculateAdjustmentTotalCost(
                productOutputs,
                productionOutputs,
                plannedMaintainFactors,
                plannedElectricityFactors,
                plannedMaterialCosts,
                processGroupAkConfigs);

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
                ProcessGroupName = baseData.ProcessGroupName,
                ProcessGroupType = baseData.ProcessGroupType,
                UnitOfMeasureId = baseData.UnitOfMeasureId ?? Guid.Empty,
                UnitOfMeasureName = baseData.UnitOfMeasureName,
                DepartmentId = baseData.DepartmentId,
                DepartmentCode = baseData.DepartmentCode,
                DepartmentName = baseData.DepartmentName,
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
                    MaterialCosts = m.Part.Costs.Select(c => new { c.StartMonth, c.EndMonth, c.Amount }).ToList()
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
                        var materialCost = m.MaterialCosts.FirstOrDefault(c => c.StartMonth <= f.MaintainStartMonth && c.EndMonth >= f.MaintainStartMonth)?.Amount ?? 0;
                        return MaintainCostCalculator.CalculateMaterialCostPerMetre(
                            materialCost,
                            m.Quantity,
                            m.ReplacementTimeStandard,
                            m.AverageMonthlyTunnelProduction,
                            f.MaintainUnitPriceType);
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
                    var costPerMetre = f.ElectricityUnitPriceEquipment.GetElectricityCostPerMetres();

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

    #endregion

    #region Calculate Methods

    private static double CalculatePlannedTotalCost(
        List<OutputData> outputs,
        Dictionary<Guid, List<MaintainFactorData>> plannedMaintainFactors,
        Dictionary<Guid, List<PlannedElectricityFactorData>> plannedElectricityFactors,
        Dictionary<Guid, double> plannedMaterialCosts)
    {
        var plannedOutputs = outputs.Where(o => o.OutputType == OutputType.PlanOutput).ToList();
        if (!plannedOutputs.Any())
        {
            return 0;
        }

        var output = plannedOutputs[0];
        var materialCost = CalculatePlannedMaterialCost(output, plannedMaterialCosts);
        var maintainCost = CalculateMaintainCost(output.PlannedMaintainCostId, plannedMaintainFactors);
        var electricityCost = CalculatePlannedElectricityCost(output.PlannedElectricityCostId, plannedElectricityFactors);

        return plannedOutputs.Sum(output =>
        {
            var materialCost = CalculatePlannedMaterialCost(output, plannedMaterialCosts);
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
        Dictionary<Guid, double> plannedMaterialCosts,
        List<AkFactorConfig> akConfigs)
    {
        var plannedOutputs = outputs
            .Where(o => o.OutputType == OutputType.PlanOutput)
            .ToList();

        if (!productionOutputs.Any() || !plannedOutputs.Any())
        {
            return 0;
        }

        return productionOutputs.Sum(productionOutput =>
        {
            // Find matching PlannedOutput by covering date range
            var plannedOutput = plannedOutputs
                .Where(o => o.StartMonth <= productionOutput.StartMonth && o.EndMonth >= productionOutput.EndMonth)
                .OrderBy(o => o.StartMonth)
                .ThenBy(o => o.EndMonth)
                .FirstOrDefault();
            if (plannedOutput == null)
            {
                return 0;
            }

            var materialCost = CalculatePlannedMaterialCost(plannedOutput, plannedMaterialCosts);
            var maintainCost = CalculateMaintainCost(plannedOutput.PlannedMaintainCostId, plannedMaintainFactors);
            var electricityCost = CalculatePlannedElectricityCost(plannedOutput.PlannedElectricityCostId, plannedElectricityFactors);
            var combinedPlannedCost = materialCost + maintainCost + electricityCost;
            var hasAkConfigs = akConfigs.Any();
            var akDiff = hasAkConfigs
                ? (decimal)(plannedOutput.PlanAshContent - productionOutput.ActualAshContent)
                : 0;
            var akRate = hasAkConfigs ? AkFactorConfig.ResolveRate(akConfigs, akDiff) : 0;
            var combinedAdjustmentAmount = hasAkConfigs
                ? (double)(akDiff * (decimal)combinedPlannedCost * akRate)
                : 0;
            return productionOutput.ProductionMeters * (combinedPlannedCost + combinedAdjustmentAmount);
        });
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

        var baseCost = factors.Sum(f =>
            f.Quantity *
            f.EquipmentCost * (1 + (f.OtherMaterialValue ?? 0) / 100.0) *
            f.K6AdjustmentFactorValue *
            f.AdjustmentFactor);
        var trimmingCoefficient = factors.FirstOrDefault()?.TrimmingCoefficient ?? 1;
        return baseCost * NormalizeTrimmingCoefficient(trimmingCoefficient);
    }

    private static double CalculatePlannedElectricityCost(
        Guid? costId,
        Dictionary<Guid, List<PlannedElectricityFactorData>> electricityFactors)
    {
        if (!costId.HasValue || !electricityFactors.TryGetValue(costId.Value, out var factors))
        {
            return 0;
        }

        var baseCost = factors.Sum(f => f.Quantity * f.CostPerMetre * f.AdjustmentFactor);
        var trimmingCoefficient = factors.FirstOrDefault()?.TrimmingCoefficient ?? 1;
        return baseCost * NormalizeTrimmingCoefficient(trimmingCoefficient);
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
        public string ProcessGroupName { get; set; } = "";
        public ProcessGroupType ProcessGroupType { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string? UnitOfMeasureName { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentName { get; set; }
    }

    private class OutputData
    {
        public Guid ProductUnitPriceId { get; set; }
        public Guid Id { get; set; }
        public OutputType OutputType { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double ProductionMeters { get; set; }
        public double PlanAshContent { get; set; }
        public ProcessGroupType ProcessGroupType { get; set; }
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

    private class ProductionOutputData
    {
        public Guid ProductUnitPriceId { get; set; }
        public double ProductionMeters { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double ActualAshContent { get; set; }
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

