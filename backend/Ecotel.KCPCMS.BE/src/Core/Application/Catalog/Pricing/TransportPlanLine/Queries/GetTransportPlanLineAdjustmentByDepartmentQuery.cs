using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportPlanLine;
using Domain.Common.Enums;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;

namespace Application.Catalog.Pricing.TransportPlanLine.Queries;

public record GetTransportPlanLineAdjustmentByDepartmentQuery(Guid DepartmentId)
    : IRequest<TransportPlanLineAdjustmentByDepartmentDetailDto>;

public class GetTransportPlanLineAdjustmentByDepartmentQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTransportPlanLineAdjustmentByDepartmentQuery, TransportPlanLineAdjustmentByDepartmentDetailDto>
{
    private readonly IWriteRepository<TransportPlanLineEntity> _transportPlanLineRepository =
        unitOfWork.GetRepository<TransportPlanLineEntity>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository =
        unitOfWork.GetRepository<ProductionOutput>();

    private readonly record struct ActualLineKey(
        DateOnly StartMonth,
        Guid ProductionProcessId,
        Guid? EquipmentId,
        string? EquipmentQuality,
        Guid? TransportRouteId,
        Guid? RouteDepartmentId);

    public async Task<TransportPlanLineAdjustmentByDepartmentDetailDto> Handle(
        GetTransportPlanLineAdjustmentByDepartmentQuery request,
        CancellationToken cancellationToken)
    {
        var lines = await _transportPlanLineRepository.GetAll()
            .Where(x =>
                x.ScenarioType == ProductUnitPriceScenarioType.Plan &&
                x.OutputType == OutputType.PlanOutput &&
                x.DepartmentId == request.DepartmentId)
            .Include(x => x.Department)
                .ThenInclude(d => d!.Code)
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.Code)
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.Equipment)
                .ThenInclude(e => e!.Code)
            .Include(x => x.TransportRoute)
                .ThenInclude(r => r!.Code)
            .Include(x => x.RouteDepartment)
                .ThenInclude(rd => rd!.Code)
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
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!lines.Any())
        {
            return new TransportPlanLineAdjustmentByDepartmentDetailDto
            {
                DepartmentId = request.DepartmentId,
                DepartmentCode = string.Empty,
                DepartmentName = string.Empty,
                Months = [],
            };
        }

        var months = lines.Select(x => x.StartMonth).Distinct().ToList();

        var actualLines = await _productionOutputRepository.GetAll()
            .Where(po => po.DepartmentId == request.DepartmentId && months.Contains(po.StartMonth))
            .SelectMany(po => po.ProductionOutputProcessGroups
                .SelectMany(pg => pg.ProductionOutputTransportLines
                    .Select(tl => new
                    {
                        po.StartMonth,
                        tl.ProductionProcessId,
                        tl.EquipmentId,
                        tl.EquipmentQuality,
                        tl.TransportRouteId,
                        tl.RouteDepartmentId,
                        tl.ProductionMeters,
                    })))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var actualByKey = actualLines
            .GroupBy(x => new ActualLineKey(
                x.StartMonth, x.ProductionProcessId, x.EquipmentId, x.EquipmentQuality, x.TransportRouteId, x.RouteDepartmentId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ProductionMeters));

        var department = lines.First();
        return new TransportPlanLineAdjustmentByDepartmentDetailDto
        {
            DepartmentId = department.DepartmentId,
            DepartmentCode = department.Department?.Code?.Value ?? string.Empty,
            DepartmentName = department.Department?.Name ?? string.Empty,
            Months = lines
                .GroupBy(x => x.StartMonth)
                .OrderBy(x => x.Key)
                .Select(monthGroup => new TransportPlanLineAdjustmentMonthDto
                {
                    Month = monthGroup.Key,
                    Items = monthGroup
                        .OrderBy(x => x.ProductionProcess?.Code?.Value ?? string.Empty)
                        .Select(line => ToAdjustmentItemDto(line, actualByKey))
                        .ToList(),
                })
                .ToList(),
        };
    }

    private static TransportPlanLineAdjustmentItemDto ToAdjustmentItemDto(
        TransportPlanLineEntity line,
        Dictionary<ActualLineKey, double> actualByKey)
    {
        var planItem = GetTransportPlanLineByDepartmentQueryHandler.ToItemDto(line);

        var key = new ActualLineKey(
            line.StartMonth, line.ProductionProcessId, line.EquipmentId, line.EquipmentQuality, line.TransportRouteId, line.RouteDepartmentId);
        var actualProductionMeters = actualByKey.TryGetValue(key, out var meters) ? meters : 0;

        var unitTotal = planItem.Material.EffectiveUnitPrice
            + planItem.Maintenance.EffectiveUnitPrice
            + planItem.Power.EffectiveUnitPrice;
        var adjustmentTotalCost = planItem.IsLowVolumeCase
            ? unitTotal
            : actualProductionMeters * unitTotal;

        return new TransportPlanLineAdjustmentItemDto
        {
            Id = planItem.Id,
            ProductionProcessId = planItem.ProductionProcessId,
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
            ActualProductionMeters = actualProductionMeters,
            UnitOfMeasureId = planItem.UnitOfMeasureId,
            UnitOfMeasureName = planItem.UnitOfMeasureName,
            K1 = planItem.K1,
            K2 = planItem.K2,
            IsLowVolumeCase = planItem.IsLowVolumeCase,
            Material = planItem.Material,
            Maintenance = planItem.Maintenance,
            Power = planItem.Power,
            AdjustmentTotalCost = adjustmentTotalCost,
        };
    }
}
