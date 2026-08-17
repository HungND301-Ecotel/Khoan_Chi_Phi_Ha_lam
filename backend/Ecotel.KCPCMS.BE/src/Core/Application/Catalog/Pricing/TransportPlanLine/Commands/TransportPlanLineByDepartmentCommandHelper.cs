using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Dto.Catalog.TransportPlanLine;
using Domain.Entities.Index;
using Shared.Constants;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportPlanLine.Commands;

internal sealed class TransportPlanLineByDepartmentPayload
{
    public Guid DepartmentId { get; init; }
    public List<TransportPlanLineMonthPayload> Months { get; init; } = new();
}

internal sealed class TransportPlanLineMonthPayload
{
    public DateOnly Month { get; init; }
    public bool LowValuePerishableSupply { get; init; }
    public List<TransportPlanLineItemPayload> Items { get; init; } = new();
}

internal sealed class TransportPlanLineItemPayload
{
    public Guid? TransportPlanLineId { get; init; }
    public Guid ProductionProcessId { get; init; }
    public Guid? TransportRouteId { get; init; }
    public Guid? RouteDepartmentId { get; init; }
    public Guid? EquipmentId { get; init; }
    public string? EquipmentQuality { get; init; }
    public double ProductionMeters { get; init; }
    public Guid? UnitOfMeasureId { get; init; }
    public AdjustmentFactorSelectionDto? K1 { get; init; }
    public AdjustmentFactorSelectionDto? K2 { get; init; }
    public Guid TransportUnitPriceId { get; init; }
}

internal static class TransportPlanLineByDepartmentCommandHelper
{
    // Sản lượng dưới ngưỡng này áp dụng bộ đơn giá đặc biệt (TransportUnitPrice.IsLowVolumeCase = true)
    // cho các tuyến đánh dấu IsSpecialLowVolume
    private const double LowVolumeThresholdTons = 10000d;

    public static async Task<TransportPlanLineByDepartmentPayload> BuildCreatePayloadAsync(
        CreateTransportPlanLineByDepartmentDto request,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<TransportUnitPriceEntity> transportUnitPriceRepository)
    {
        return await BuildPayloadAsync(
            request.DepartmentId,
            request.Months.Select(month => new MonthInput(
                month.Month,
                month.LowValuePerishableSupply,
                month.Items.Select(item => new ItemInput(
                    null,
                    item.ProductionProcessId,
                    item.TransportRouteId,
                    item.RouteDepartmentId,
                    item.EquipmentId,
                    item.EquipmentQuality,
                    item.ProductionMeters,
                    item.UnitOfMeasureId,
                    item.K1,
                    item.K2)).ToList())).ToList(),
            departmentRepository,
            transportUnitPriceRepository);
    }

    public static async Task<TransportPlanLineByDepartmentPayload> BuildUpdatePayloadAsync(
        UpdateTransportPlanLineByDepartmentDto request,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<TransportUnitPriceEntity> transportUnitPriceRepository)
    {
        return await BuildPayloadAsync(
            request.DepartmentId,
            request.Months.Select(month => new MonthInput(
                month.Month,
                month.LowValuePerishableSupply,
                month.Items.Select(item => new ItemInput(
                    item.TransportPlanLineId,
                    item.ProductionProcessId,
                    item.TransportRouteId,
                    item.RouteDepartmentId,
                    item.EquipmentId,
                    item.EquipmentQuality,
                    item.ProductionMeters,
                    item.UnitOfMeasureId,
                    item.K1,
                    item.K2)).ToList())).ToList(),
            departmentRepository,
            transportUnitPriceRepository);
    }

    private static async Task<TransportPlanLineByDepartmentPayload> BuildPayloadAsync(
        Guid departmentId,
        IList<MonthInput> months,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<TransportUnitPriceEntity> transportUnitPriceRepository)
    {
        if (departmentId == Guid.Empty)
        {
            throw new BadRequestException(CustomResponseMessage.DepartmentIdCannotBeEmpty);
        }

        if (!months.Any())
        {
            throw new BadRequestException(CustomResponseMessage.OutputEmpty);
        }

        var duplicatedMonths = months
            .GroupBy(x => x.Month)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicatedMonths.Any())
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        var departmentExists = await departmentRepository.ExistsAsync(x => x.Id == departmentId);
        if (!departmentExists)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var allItems = months.SelectMany(m => m.Items.Select(item => (Month: m.Month, Item: item))).ToList();
        if (!allItems.Any())
        {
            throw new BadRequestException(CustomResponseMessage.OutputEmpty);
        }

        if (allItems.Any(x => x.Item.ProductionProcessId == Guid.Empty))
        {
            throw new NotFoundException(CustomResponseMessage.ProductionProcessNotFound);
        }

        if (allItems.Any(x => x.Item.ProductionMeters <= 0))
        {
            throw new BadRequestException(CustomResponseMessage.ProductionMetersMustBeGreaterThanZero);
        }

        var processIds = allItems.Select(x => x.Item.ProductionProcessId).Distinct().ToList();
        var candidatePrices = await transportUnitPriceRepository.GetAllAsync(
            predicate: x => processIds.Contains(x.ProductionProcessId),
            disableTracking: true);

        var months_ = months.Select(month => new TransportPlanLineMonthPayload
        {
            Month = month.Month,
            LowValuePerishableSupply = month.LowValuePerishableSupply,
            Items = month.Items.Select(item =>
            {
                var transportUnitPriceId = ResolveTransportUnitPriceId(candidatePrices, item, month.Month);
                return new TransportPlanLineItemPayload
                {
                    TransportPlanLineId = item.TransportPlanLineId,
                    ProductionProcessId = item.ProductionProcessId,
                    TransportRouteId = item.TransportRouteId,
                    RouteDepartmentId = item.RouteDepartmentId,
                    EquipmentId = item.EquipmentId,
                    EquipmentQuality = item.EquipmentQuality,
                    ProductionMeters = item.ProductionMeters,
                    UnitOfMeasureId = item.UnitOfMeasureId,
                    K1 = item.K1,
                    K2 = item.K2,
                    TransportUnitPriceId = transportUnitPriceId,
                };
            }).ToList(),
        }).ToList();

        return new TransportPlanLineByDepartmentPayload
        {
            DepartmentId = departmentId,
            Months = months_,
        };
    }

    /// <summary>
    /// Tự động dò dòng TransportUnitPrice khớp với tổ hợp field của dòng kế hoạch — không cần người
    /// dùng tự chọn đơn giá
    /// </summary>
    private static Guid ResolveTransportUnitPriceId(
        IList<TransportUnitPriceEntity> candidates,
        ItemInput item,
        DateOnly month)
    {
        var matches = candidates
            .Where(x =>
                x.ProductionProcessId == item.ProductionProcessId &&
                x.TransportRouteId == item.TransportRouteId &&
                x.DepartmentId == item.RouteDepartmentId &&
                x.EquipmentId == item.EquipmentId &&
                x.EquipmentQuality == item.EquipmentQuality &&
                x.StartMonth <= month &&
                x.EndMonth >= month)
            .ToList();

        if (matches.Count == 0)
        {
            throw new NotFoundException(CustomResponseMessage.TransportUnitPriceNotFound);
        }

        if (matches.Count == 1)
        {
            return matches[0].Id;
        }

        // Tuyến đặc biệt: 2 dòng song song (IsLowVolumeCase true/false cho cùng tổ hợp field)
        var preferLowVolume = item.ProductionMeters < LowVolumeThresholdTons;
        var matched = matches.FirstOrDefault(x => x.IsLowVolumeCase == preferLowVolume) ?? matches[0];
        return matched.Id;
    }

    private sealed record MonthInput(DateOnly Month, bool LowValuePerishableSupply, IList<ItemInput> Items);

    private sealed record ItemInput(
        Guid? TransportPlanLineId,
        Guid ProductionProcessId,
        Guid? TransportRouteId,
        Guid? RouteDepartmentId,
        Guid? EquipmentId,
        string? EquipmentQuality,
        double ProductionMeters,
        Guid? UnitOfMeasureId,
        AdjustmentFactorSelectionDto? K1,
        AdjustmentFactorSelectionDto? K2);
}
