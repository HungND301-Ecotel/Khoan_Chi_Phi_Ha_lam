using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Dto.Catalog.TransportPlanLine;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using Microsoft.EntityFrameworkCore;
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
    public Guid? HaulDistanceId { get; init; }
    public Guid? CargoTypeId { get; init; }
    public Guid? ReceivingLocationId { get; init; }
    public Guid? DumpingLocationId { get; init; }
    public double ProductionMeters { get; init; }
    public Guid? UnitOfMeasureId { get; init; }
    public AdjustmentFactorSelectionDto? K1 { get; init; }
    public AdjustmentFactorSelectionDto? K2 { get; init; }
    public Guid? TransportUnitPriceId { get; init; }
    public Guid? MechanizedTransportUnitPriceDetailId { get; init; }
}

internal static class TransportPlanLineByDepartmentCommandHelper
{
    // Sản lượng dưới ngưỡng này áp dụng bộ đơn giá đặc biệt (TransportUnitPrice.IsLowVolumeCase = true)
    // cho các tuyến đánh dấu IsSpecialLowVolume
    private const double LowVolumeThresholdTons = 10000d;

    public static async Task<TransportPlanLineByDepartmentPayload> BuildCreatePayloadAsync(
        CreateTransportPlanLineByDepartmentDto request,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<TransportUnitPriceEntity> transportUnitPriceRepository,
        IWriteRepository<MechanizedTransportUnitPrice> mechanizedTransportUnitPriceRepository,
        IWriteRepository<ProductionProcess> productionProcessRepository)
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
                    item.HaulDistanceId,
                    item.CargoTypeId,
                    item.ReceivingLocationId,
                    item.DumpingLocationId,
                    item.ProductionMeters,
                    item.UnitOfMeasureId,
                    item.K1,
                    item.K2)).ToList())).ToList(),
            departmentRepository,
            transportUnitPriceRepository,
            mechanizedTransportUnitPriceRepository,
            productionProcessRepository);
    }

    public static async Task<TransportPlanLineByDepartmentPayload> BuildUpdatePayloadAsync(
        UpdateTransportPlanLineByDepartmentDto request,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<TransportUnitPriceEntity> transportUnitPriceRepository,
        IWriteRepository<MechanizedTransportUnitPrice> mechanizedTransportUnitPriceRepository,
        IWriteRepository<ProductionProcess> productionProcessRepository)
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
                    item.HaulDistanceId,
                    item.CargoTypeId,
                    item.ReceivingLocationId,
                    item.DumpingLocationId,
                    item.ProductionMeters,
                    item.UnitOfMeasureId,
                    item.K1,
                    item.K2)).ToList())).ToList(),
            departmentRepository,
            transportUnitPriceRepository,
            mechanizedTransportUnitPriceRepository,
            productionProcessRepository);
    }

    private static async Task<TransportPlanLineByDepartmentPayload> BuildPayloadAsync(
        Guid departmentId,
        IList<MonthInput> months,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<TransportUnitPriceEntity> transportUnitPriceRepository,
        IWriteRepository<MechanizedTransportUnitPrice> mechanizedTransportUnitPriceRepository,
        IWriteRepository<ProductionProcess> productionProcessRepository)
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
        var processes = await productionProcessRepository.GetAll()
            .Where(x => processIds.Contains(x.Id))
            .Include(x => x.ProcessGroup)
                .ThenInclude(g => g!.FixedKey)
            .AsNoTracking()
            .ToListAsync();

        var processMap = processes.ToDictionary(x => x.Id, x => x);

        var vtlProcessIds = processMap.Values
            .Where(p => p.ProcessGroup?.Type == ProcessGroupType.VTL || p.ProcessGroup?.FixedKey?.Key == "VTL")
            .Select(p => p.Id)
            .ToHashSet();

        var vtcgProcessIds = processMap.Values
            .Where(p => p.ProcessGroup?.Type == ProcessGroupType.VTCG || p.ProcessGroup?.FixedKey?.Key == "VTCG"
                     || (p.ProcessGroup?.Name != null && p.ProcessGroup.Name.Contains("cơ giới", StringComparison.OrdinalIgnoreCase)))
            .Select(p => p.Id)
            .ToHashSet();

        var candidateVtlPrices = await transportUnitPriceRepository.GetAllAsync(
            predicate: x => processIds.Contains(x.ProductionProcessId),
            disableTracking: true);

        var candidateVtcgPrices = await mechanizedTransportUnitPriceRepository.GetAll()
            .Where(x => processIds.Contains(x.ProductionProcessId))
            .Include(x => x.Details)
            .AsNoTracking()
            .ToListAsync();

        var months_ = months.Select(month => new TransportPlanLineMonthPayload
        {
            Month = month.Month,
            LowValuePerishableSupply = month.LowValuePerishableSupply,
            Items = month.Items.Select(item =>
            {
                var isVtcg = item.HaulDistanceId.HasValue
                    || candidateVtcgPrices.Any(x => x.ProductionProcessId == item.ProductionProcessId)
                    || vtcgProcessIds.Contains(item.ProductionProcessId)
                    || (item.TransportRouteId == null && item.RouteDepartmentId == null && !candidateVtlPrices.Any(x => x.ProductionProcessId == item.ProductionProcessId));

                Guid? transportUnitPriceId = null;
                Guid? mechanizedTransportUnitPriceDetailId = null;

                if (isVtcg)
                {
                    mechanizedTransportUnitPriceDetailId = ResolveMechanizedTransportUnitPriceDetailId(candidateVtcgPrices, item, month.Month);
                }
                else
                {
                    transportUnitPriceId = ResolveTransportUnitPriceId(candidateVtlPrices, item, month.Month);
                }

                return new TransportPlanLineItemPayload
                {
                    TransportPlanLineId = item.TransportPlanLineId,
                    ProductionProcessId = item.ProductionProcessId,
                    TransportRouteId = item.TransportRouteId,
                    RouteDepartmentId = item.RouteDepartmentId,
                    EquipmentId = item.EquipmentId,
                    EquipmentQuality = item.EquipmentQuality,
                    HaulDistanceId = item.HaulDistanceId,
                    CargoTypeId = item.CargoTypeId,
                    ReceivingLocationId = item.ReceivingLocationId,
                    DumpingLocationId = item.DumpingLocationId,
                    ProductionMeters = item.ProductionMeters,
                    UnitOfMeasureId = item.UnitOfMeasureId,
                    K1 = item.K1,
                    K2 = item.K2,
                    TransportUnitPriceId = transportUnitPriceId,
                    MechanizedTransportUnitPriceDetailId = mechanizedTransportUnitPriceDetailId,
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
    /// Tự động dò dòng TransportUnitPrice khớp với tổ hợp field của dòng kế hoạch VTL — không cần người
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
                (x.DepartmentId == null || x.DepartmentId == item.RouteDepartmentId) &&
                x.EquipmentId == item.EquipmentId &&
                x.EquipmentQuality == item.EquipmentQuality &&
                x.StartMonth <= month &&
                x.EndMonth >= month)
            .ToList();

        if (matches.Count == 0)
        {
            matches = candidates
                .Where(x =>
                    x.ProductionProcessId == item.ProductionProcessId &&
                    x.TransportRouteId == item.TransportRouteId &&
                    (x.DepartmentId == null || x.DepartmentId == item.RouteDepartmentId) &&
                    x.EquipmentId == item.EquipmentId &&
                    x.EquipmentQuality == item.EquipmentQuality)
                .ToList();
        }

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

    /// <summary>
    /// Tự động dò dòng MechanizedTransportUnitPriceDetail khớp với tổ hợp field của dòng kế hoạch VTCG
    /// (Đơn giá vận tải cơ giới không theo đơn vị phòng ban mà theo Công đoạn, Nhóm xe, Chất lượng, Cung độ)
    /// </summary>
    private static Guid ResolveMechanizedTransportUnitPriceDetailId(
        IList<MechanizedTransportUnitPrice> candidates,
        ItemInput item,
        DateOnly month)
    {
        // 1. Khớp chính xác: Công đoạn + Nhóm xe + Chất lượng + Thời gian hiệu lực
        var matchedHeaders = candidates
            .Where(x =>
                x.ProductionProcessId == item.ProductionProcessId &&
                (item.EquipmentId == null || x.AssignmentCodeId == item.EquipmentId) &&
                (string.IsNullOrEmpty(item.EquipmentQuality) || string.Equals(x.EquipmentQuality, item.EquipmentQuality, StringComparison.OrdinalIgnoreCase)) &&
                x.StartMonth <= month &&
                x.EndMonth >= month)
            .ToList();

        // 2. Nếu không khớp thời gian, lấy theo Công đoạn + Nhóm xe + Chất lượng
        if (matchedHeaders.Count == 0)
        {
            matchedHeaders = candidates
                .Where(x =>
                    x.ProductionProcessId == item.ProductionProcessId &&
                    (item.EquipmentId == null || x.AssignmentCodeId == item.EquipmentId) &&
                    (string.IsNullOrEmpty(item.EquipmentQuality) || string.Equals(x.EquipmentQuality, item.EquipmentQuality, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // 3. Nếu vẫn chưa có, lấy theo Công đoạn + Nhóm xe
        if (matchedHeaders.Count == 0 && item.EquipmentId.HasValue)
        {
            matchedHeaders = candidates
                .Where(x =>
                    x.ProductionProcessId == item.ProductionProcessId &&
                    x.AssignmentCodeId == item.EquipmentId)
                .ToList();
        }

        // 4. Nếu vẫn chưa có, lấy theo Công đoạn
        if (matchedHeaders.Count == 0)
        {
            matchedHeaders = candidates
                .Where(x => x.ProductionProcessId == item.ProductionProcessId)
                .ToList();
        }

        if (matchedHeaders.Count == 0)
        {
            throw new NotFoundException(CustomResponseMessage.TransportUnitPriceNotFound);
        }

        var allDetails = matchedHeaders.SelectMany(h => h.Details).ToList();

        MechanizedTransportUnitPriceDetail? matchedDetail = null;
        if (item.HaulDistanceId.HasValue)
        {
            matchedDetail = allDetails.FirstOrDefault(d => d.HaulDistanceId == item.HaulDistanceId.Value);
        }

        matchedDetail ??= allDetails.FirstOrDefault(d => d.HaulDistanceId == null) ?? allDetails.FirstOrDefault();

        if (matchedDetail == null)
        {
            throw new NotFoundException(CustomResponseMessage.TransportUnitPriceNotFound);
        }

        return matchedDetail.Id;
    }

    private sealed record MonthInput(DateOnly Month, bool LowValuePerishableSupply, IList<ItemInput> Items);

    private sealed record ItemInput(
        Guid? TransportPlanLineId,
        Guid ProductionProcessId,
        Guid? TransportRouteId,
        Guid? RouteDepartmentId,
        Guid? EquipmentId,
        string? EquipmentQuality,
        Guid? HaulDistanceId,
        Guid? CargoTypeId,
        Guid? ReceivingLocationId,
        Guid? DumpingLocationId,
        double ProductionMeters,
        Guid? UnitOfMeasureId,
        AdjustmentFactorSelectionDto? K1,
        AdjustmentFactorSelectionDto? K2);
}
