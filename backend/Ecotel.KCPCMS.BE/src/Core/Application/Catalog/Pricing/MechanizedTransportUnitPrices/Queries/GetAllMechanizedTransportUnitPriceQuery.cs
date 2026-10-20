using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Domain.Common.Enums;
using Microsoft.EntityFrameworkCore;
using MediatR;
using ScaniaTruckUnitPriceEntity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice;
using WasteSuctionTruckUnitPriceEntity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice;
using ServiceAndCraneVehicleUnitPriceEntity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice;
using ExcavatorAndBulldozerUnitPriceEntity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice;
using MechanizedTransportUnitPrice = Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportUnitPrice;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.Queries;

public record GetAllMechanizedTransportUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, MechanizedTransportUnitPriceType? VehicleType) : IRequest<PaginationResponse<MechanizedTransportUnitPriceGroupDto>>;

public class GetAllMechanizedTransportUnitPriceQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllMechanizedTransportUnitPriceQuery, PaginationResponse<MechanizedTransportUnitPriceGroupDto>>
{
    private readonly IWriteRepository<MechanizedTransportUnitPrice> _repository = unitOfWork.GetRepository<MechanizedTransportUnitPrice>();

    public async Task<PaginationResponse<MechanizedTransportUnitPriceGroupDto>> Handle(GetAllMechanizedTransportUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var searchTerm = (request.Search ?? "").Trim().ToLower();
        var vehicleType = request.VehicleType;

        var baseQuery = _repository.GetAll()
            .AsNoTracking()
            .Where(x =>
                (vehicleType == null
                    || (vehicleType == MechanizedTransportUnitPriceType.ScaniaTruck && x is ScaniaTruckUnitPriceEntity)
                    || (vehicleType == MechanizedTransportUnitPriceType.WasteSuctionTruck && x is WasteSuctionTruckUnitPriceEntity)
                    || (vehicleType == MechanizedTransportUnitPriceType.ServiceAndCraneVehicle && x is ServiceAndCraneVehicleUnitPriceEntity)
                    || (vehicleType == MechanizedTransportUnitPriceType.ExcavatorAndBulldozer && x is ExcavatorAndBulldozerUnitPriceEntity))
                && (string.IsNullOrWhiteSpace(searchTerm)
                    || (x.AssignmentCode != null && x.AssignmentCode.Name.ToLower().Contains(searchTerm))));

        // Đơn vị phân trang = 1 dòng cấp 1 trên danh sách: Nhóm vật tư, tài sản (AssignmentCode) + Chất lượng
        // thiết bị. Mỗi dòng trả về đủ mọi khoảng thời gian của nó (mỗi khoảng thời gian là 1 GroupDto).
        var groupKeys = await baseQuery
            .Select(x => new
            {
                x.AssignmentCodeId,
                AssignmentCodeName = x.AssignmentCode != null ? x.AssignmentCode.Name : string.Empty,
                x.EquipmentQuality,
                x.StartMonth,
                x.EndMonth
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        // Xe có khoảng thời gian mới nhất lên trước; các chất lượng của cùng 1 xe đứng liền nhau
        var latestStartByAssignment = groupKeys
            .GroupBy(k => k.AssignmentCodeId)
            .ToDictionary(g => g.Key, g => g.Max(k => k.StartMonth));

        var orderedRowKeys = groupKeys
            .GroupBy(k => new { k.AssignmentCodeId, k.AssignmentCodeName, k.EquipmentQuality })
            .Select(g => g.Key)
            .OrderByDescending(k => latestStartByAssignment[k.AssignmentCodeId])
            .ThenBy(k => k.AssignmentCodeName)
            .ThenBy(k => k.AssignmentCodeId)
            .ThenBy(k => k.EquipmentQuality)
            .ToList();

        var totalCount = orderedRowKeys.Count;

        var pagedRowKeys = request.IgnorePagination
            ? orderedRowKeys
            : orderedRowKeys.Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();

        if (pagedRowKeys.Count == 0)
        {
            return new PaginationResponse<MechanizedTransportUnitPriceGroupDto>(new List<MechanizedTransportUnitPriceGroupDto>(), totalCount, request.PageIndex, request.PageSize);
        }

        var assignmentCodeIds = pagedRowKeys.Select(k => k.AssignmentCodeId).Distinct().ToList();

        var entities = await baseQuery
            .Where(x => assignmentCodeIds.Contains(x.AssignmentCodeId))
            .Include(x => x.ProductionProcess)
            .Include(x => x.Details).ThenInclude(d => d.HaulDistance)
            .Include(x => ((ScaniaTruckUnitPriceEntity)x).CargoType)
            .Include(x => ((ScaniaTruckUnitPriceEntity)x).ReceivingLocations).ThenInclude(r => r.TransportLocation)
            .Include(x => ((ScaniaTruckUnitPriceEntity)x).DumpingLocation)
            .ToListAsync(cancellationToken);

        var groupsByRowKey = entities
            .GroupBy(e => (e.AssignmentCodeId, e.EquipmentQuality))
            .ToDictionary(g => g.Key, g => g.ToList());

        var data = pagedRowKeys.SelectMany(rowKey =>
        {
            var rowEntities = groupsByRowKey.GetValueOrDefault((rowKey.AssignmentCodeId, rowKey.EquipmentQuality)) ?? new List<MechanizedTransportUnitPrice>();

            return rowEntities
                .GroupBy(e => (e.StartMonth, e.EndMonth))
                .OrderByDescending(g => g.Key.StartMonth)
                .Select(periodGroup => BuildGroupDto(
                    rowKey.AssignmentCodeId,
                    rowKey.AssignmentCodeName,
                    rowKey.EquipmentQuality,
                    periodGroup.Key.StartMonth,
                    periodGroup.Key.EndMonth,
                    periodGroup.ToList()));
        }).ToList();

        return new PaginationResponse<MechanizedTransportUnitPriceGroupDto>(data, totalCount, request.PageIndex, request.PageSize);
    }

    private static MechanizedTransportUnitPriceGroupDto BuildGroupDto(
        Guid assignmentCodeId,
        string assignmentCodeName,
        string equipmentQuality,
        DateOnly startMonth,
        DateOnly endMonth,
        IList<MechanizedTransportUnitPrice> groupEntities)
    {
        var sections = groupEntities
            .GroupBy(e => new
            {
                VehicleType = GetVehicleType(e),
                e.ProductionProcessId,
                // Scania-specific grouping
                CargoTypeId = e is ScaniaTruckUnitPriceEntity scania ? scania.CargoTypeId : (Guid?)null,
                ReceivingLocationKey = e is ScaniaTruckUnitPriceEntity scania2
                    ? string.Join(",", scania2.ReceivingLocations.Select(r => r.TransportLocationId).OrderBy(id => id))
                    : string.Empty,
                DumpingLocationId = e is ScaniaTruckUnitPriceEntity scania3 ? scania3.DumpingLocationId : (Guid?)null,
            })
            .Select(sectionGroup =>
            {
                var firstScania = sectionGroup.First() as ScaniaTruckUnitPriceEntity;
                var receivingNames = firstScania?.ReceivingLocations
                    .Where(r => r.TransportLocation != null)
                    .Select(r => r.TransportLocation!.Name)
                    .ToList() ?? new List<string>();

                return new MechanizedTransportUnitPriceSectionDto
                {
                    VehicleType = sectionGroup.Key.VehicleType,
                    ProductionProcessId = sectionGroup.Key.ProductionProcessId,
                    ProductionProcessName = sectionGroup.First().ProductionProcess?.Name ?? string.Empty,
                    // Scania-specific fields
                    CargoTypeId = sectionGroup.Key.CargoTypeId,
                    CargoTypeName = sectionGroup.Key.VehicleType == MechanizedTransportUnitPriceType.ScaniaTruck
                        ? (firstScania?.CargoType?.Name)
                        : null,
                    ReceivingLocationId = firstScania?.ReceivingLocations.Select(r => (Guid?)r.TransportLocationId).FirstOrDefault(),
                    ReceivingLocationName = sectionGroup.Key.VehicleType == MechanizedTransportUnitPriceType.ScaniaTruck
                        ? (receivingNames.Any() ? string.Join(", ", receivingNames) : null)
                        : null,
                    ReceivingLocationIds = firstScania?.ReceivingLocations.Select(r => r.TransportLocationId).ToList() ?? new List<Guid>(),
                    ReceivingLocationNames = receivingNames,
                    DumpingLocationId = sectionGroup.Key.DumpingLocationId,
                    DumpingLocationName = sectionGroup.Key.VehicleType == MechanizedTransportUnitPriceType.ScaniaTruck
                        ? (firstScania?.DumpingLocation?.Name)
                        : null,
                    Rows = sectionGroup
                        .SelectMany(header => header.Details.Select(detail => new MechanizedTransportUnitPriceRowDto
                        {
                            HeaderId = header.Id,
                            DetailId = detail.Id,
                            EquipmentQuality = header.EquipmentQuality,
                            HaulDistanceId = detail.HaulDistanceId,
                            HaulDistanceValue = detail.HaulDistance?.Value,
                            FuelUnitPrice = detail.FuelUnitPrice,
                            PowerUnitPrice = detail.PowerUnitPrice,
                            MaintenanceUnitPrice = detail.MaintenanceUnitPrice
                        }))
                        .OrderBy(r => r.HaulDistanceValue)
                        .ToList()
                };
            })
            .OrderBy(s => s.VehicleType)
            .ThenBy(s => s.ProductionProcessName)
            .ToList();

        return new MechanizedTransportUnitPriceGroupDto
        {
            AssignmentCodeId = assignmentCodeId,
            AssignmentCodeName = assignmentCodeName,
            EquipmentQuality = equipmentQuality,
            StartMonth = startMonth,
            EndMonth = endMonth,
            Sections = sections
        };
    }

    private static MechanizedTransportUnitPriceType GetVehicleType(MechanizedTransportUnitPrice entity) => entity switch
    {
        ScaniaTruckUnitPriceEntity => MechanizedTransportUnitPriceType.ScaniaTruck,
        WasteSuctionTruckUnitPriceEntity => MechanizedTransportUnitPriceType.WasteSuctionTruck,
        ServiceAndCraneVehicleUnitPriceEntity => MechanizedTransportUnitPriceType.ServiceAndCraneVehicle,
        _ => MechanizedTransportUnitPriceType.ExcavatorAndBulldozer
    };
}
