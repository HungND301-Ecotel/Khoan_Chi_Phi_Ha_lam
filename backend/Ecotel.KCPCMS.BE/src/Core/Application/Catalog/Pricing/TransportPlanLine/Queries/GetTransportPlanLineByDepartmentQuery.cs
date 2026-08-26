using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportPlanLine;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;

namespace Application.Catalog.Pricing.TransportPlanLine.Queries;

public record GetTransportPlanLineByDepartmentQuery(Guid DepartmentId)
    : IRequest<TransportPlanLineByDepartmentDetailDto>;

public class GetTransportPlanLineByDepartmentQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTransportPlanLineByDepartmentQuery, TransportPlanLineByDepartmentDetailDto>
{
    private readonly IWriteRepository<TransportPlanLineEntity> _transportPlanLineRepository =
        unitOfWork.GetRepository<TransportPlanLineEntity>();

    public async Task<TransportPlanLineByDepartmentDetailDto> Handle(
        GetTransportPlanLineByDepartmentQuery request,
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
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.ProcessGroup)
                    .ThenInclude(pg => pg!.Code)
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.ProcessGroup)
                    .ThenInclude(pg => pg!.FixedKey)
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
            // Chế độ "chọn từ danh mục" (AdjustmentFactorDescriptionId) không set AdjustmentFactorId
            // trên factor — phải đi qua AdjustmentFactorDescription.AdjustmentFactor mới ra đúng K.
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
            .Include(x => x.HaulDistance)
            .Include(x => x.CargoType)
                .ThenInclude(c => c!.Code)
            .Include(x => x.ReceivingLocation)
                .ThenInclude(l => l!.Code)
            .Include(x => x.DumpingLocation)
                .ThenInclude(l => l!.Code)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!lines.Any())
        {
            return new TransportPlanLineByDepartmentDetailDto
            {
                DepartmentId = request.DepartmentId,
                DepartmentCode = string.Empty,
                DepartmentName = string.Empty,
                Months = [],
            };
        }

        var department = lines.First();
        return new TransportPlanLineByDepartmentDetailDto
        {
            DepartmentId = department.DepartmentId,
            DepartmentCode = department.Department?.Code?.Value ?? string.Empty,
            DepartmentName = department.Department?.Name ?? string.Empty,
            Months = lines
                .GroupBy(x => x.StartMonth)
                .OrderBy(x => x.Key)
                .Select(monthGroup => new TransportPlanLineMonthDto
                {
                    Month = monthGroup.Key,
                    // Cùng giá trị trên mọi dòng trong tháng (set đồng loạt lúc Create/Update) —
                    // lấy đại diện từ dòng đầu tiên.
                    LowValuePerishableSupply = monthGroup.Any(x =>
                        x.PlannedTransportCost?.LowValuePerishableSupplyInclusion
                            == LowValuePerishableSupplyInclusion.Include),
                    Items = monthGroup
                        .OrderBy(x => x.ProductionProcess?.Code?.Value ?? string.Empty)
                        .ThenBy(x => x.Equipment?.Code?.Value ?? string.Empty)
                        .ThenBy(x => x.EquipmentQuality ?? string.Empty)
                        .ThenBy(x => x.TransportRoute?.Code?.Value ?? string.Empty)
                        .Select(ToItemDto)
                        .ToList(),
                })
                .ToList(),
        };
    }

    // internal: dùng chung cho GetTransportPlanLineByIdQueryHandler 
    internal static TransportPlanLineItemDto ToItemDto(TransportPlanLineEntity line)
    {
        var cost = line.PlannedTransportCost;

        var materialEffectiveUnitPrice = (double)(cost?.MaterialFuelUnitPrice ?? 0);
        var maintenanceEffectiveUnitPrice = (double)(cost?.MaintenanceUnitPrice ?? 0) * (cost?.MaintenanceCoefficient ?? 1);
        var powerEffectiveUnitPrice = (double)(cost?.PowerUnitPrice ?? 0) * (cost?.PowerCoefficient ?? 1);
        var isLowVolumeCase = cost?.IsLowVolumeCase ?? false;
        var unitTotal = materialEffectiveUnitPrice + maintenanceEffectiveUnitPrice + powerEffectiveUnitPrice;

        return new TransportPlanLineItemDto
        {
            Id = line.Id,
            PlannedTransportCostId = cost?.Id,
            ProcessGroupId = line.ProductionProcess?.ProcessGroupId,
            ProcessGroupCode = line.ProductionProcess?.ProcessGroup?.Code?.Value ?? line.ProductionProcess?.ProcessGroup?.FixedKey?.Key,
            ProcessGroupName = line.ProductionProcess?.ProcessGroup?.Name,
            ProductionProcessId = line.ProductionProcessId,
            ProductionProcessCode = line.ProductionProcess?.Code?.Value ?? string.Empty,
            ProductionProcessName = line.ProductionProcess?.Name ?? string.Empty,
            TransportRouteId = line.TransportRouteId,
            TransportRouteCode = line.TransportRoute?.Code?.Value,
            TransportRouteName = line.TransportRoute?.Name,
            RouteDepartmentId = line.RouteDepartmentId,
            RouteDepartmentCode = line.RouteDepartment?.Code?.Value,
            RouteDepartmentName = line.RouteDepartment?.Name,
            EquipmentId = line.EquipmentId,
            EquipmentCode = line.Equipment?.Code?.Value,
            EquipmentName = line.Equipment?.Name,
            EquipmentQuality = line.EquipmentQuality,
            HaulDistanceId = line.HaulDistanceId,
            HaulDistanceValue = line.HaulDistance?.Value,
            CargoTypeId = line.CargoTypeId,
            CargoTypeCode = line.CargoType?.Code?.Value,
            CargoTypeName = line.CargoType?.Name,
            ReceivingLocationId = line.ReceivingLocationId,
            ReceivingLocationCode = line.ReceivingLocation?.Code?.Value,
            ReceivingLocationName = line.ReceivingLocation?.Name,
            DumpingLocationId = line.DumpingLocationId,
            DumpingLocationCode = line.DumpingLocation?.Code?.Value,
            DumpingLocationName = line.DumpingLocation?.Name,
            ProductionMeters = line.ProductionMeters,
            UnitOfMeasureId = line.UnitOfMeasureId,
            UnitOfMeasureName = line.UnitOfMeasure?.Name,
            K1 = ToSelectionDto(cost, FixedKeyType.K1, "K1"),
            K2 = ToSelectionDto(cost, FixedKeyType.K2, "K2"),
            IsLowVolumeCase = isLowVolumeCase,
            Material = new TransportCostComponentDto
            {
                BaseUnitPrice = cost?.MaterialFuelUnitPrice,
                K1Coefficient = null,
                K2Coefficient = null,
                EffectiveUnitPrice = materialEffectiveUnitPrice,
            },
            Maintenance = new TransportCostComponentDto
            {
                BaseUnitPrice = cost?.MaintenanceUnitPrice,
                K1Coefficient = GetFactorEffectiveValue(cost, FixedKeyType.K1, "K1", forMaintenance: true),
                K2Coefficient = GetFactorEffectiveValue(cost, FixedKeyType.K2, "K2", forMaintenance: true),
                EffectiveUnitPrice = maintenanceEffectiveUnitPrice,
            },
            Power = new TransportCostComponentDto
            {
                BaseUnitPrice = cost?.PowerUnitPrice,
                K1Coefficient = GetFactorEffectiveValue(cost, FixedKeyType.K1, "K1", forMaintenance: false),
                K2Coefficient = GetFactorEffectiveValue(cost, FixedKeyType.K2, "K2", forMaintenance: false),
                EffectiveUnitPrice = powerEffectiveUnitPrice,
            },
            PlannedTotalCost = isLowVolumeCase ? unitTotal : line.ProductionMeters * unitTotal,
        };
    }

    private static AdjustmentFactorSelectionDto? ToSelectionDto(
        Domain.Entities.Pricing.PlannedTransportCost? cost,
        FixedKeyType fixedKeyType,
        string fixedKeyKey)
    {
        var factor = FindFactor(cost, fixedKeyType, fixedKeyKey);
        if (factor == null)
        {
            return null;
        }

        return new AdjustmentFactorSelectionDto
        {
            AdjustmentFactorDescriptionId = factor.AdjustmentFactorDescriptionId,
            AdjustmentFactorId = factor.AdjustmentFactorId,
            CustomValue = factor.CustomValue,
        };
    }

    // Giá trị hiệu lực riêng của từng K
    private static double? GetFactorEffectiveValue(
        Domain.Entities.Pricing.PlannedTransportCost? cost,
        FixedKeyType fixedKeyType,
        string fixedKeyKey,
        bool forMaintenance)
    {
        var factor = FindFactor(cost, fixedKeyType, fixedKeyKey);
        if (factor == null)
        {
            return null;
        }

        return forMaintenance ? factor.MaintenanceEffectiveValue : factor.PowerEffectiveValue;
    }

    private static Domain.Entities.Index.PlannedTransportCostAdjustmentFactor? FindFactor(
        Domain.Entities.Pricing.PlannedTransportCost? cost,
        FixedKeyType fixedKeyType,
        string fixedKeyKey)
    {
        return cost?.AdjustmentFactors.FirstOrDefault(f =>
        {
            var adjustmentFactor = f.AdjustmentFactorDescription?.AdjustmentFactor ?? f.AdjustmentFactor;
            var fixedKey = adjustmentFactor?.FixedKey;
            if (fixedKey != null)
            {
                return fixedKey.Type == fixedKeyType;
            }

            return string.Equals(adjustmentFactor?.Code?.Value, fixedKeyKey, StringComparison.OrdinalIgnoreCase);
        });
    }
}
