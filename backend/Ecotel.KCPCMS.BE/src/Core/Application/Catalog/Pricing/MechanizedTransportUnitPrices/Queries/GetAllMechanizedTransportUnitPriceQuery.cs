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

        // Gộp theo Nhóm vật tư, tài sản (AssignmentCode) + khoảng thời gian: đây mới là đơn vị phân trang (1 "card" hiển thị),
        // không phân trang theo từng dòng loại xe/công đoạn/chất lượng bên trong.
        var groupKeys = await baseQuery
            .Select(x => new
            {
                x.AssignmentCodeId,
                AssignmentCodeName = x.AssignmentCode != null ? x.AssignmentCode.Name : string.Empty,
                x.StartMonth,
                x.EndMonth
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var orderedKeys = groupKeys
            .OrderByDescending(k => k.StartMonth)
            .ThenBy(k => k.AssignmentCodeName)
            .ToList();

        var totalCount = orderedKeys.Count;

        var pagedKeys = request.IgnorePagination
            ? orderedKeys
            : orderedKeys.Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();

        if (pagedKeys.Count == 0)
        {
            return new PaginationResponse<MechanizedTransportUnitPriceGroupDto>(new List<MechanizedTransportUnitPriceGroupDto>(), totalCount, request.PageIndex, request.PageSize);
        }

        var assignmentCodeIds = pagedKeys.Select(k => k.AssignmentCodeId).Distinct().ToList();

        var entities = await baseQuery
            .Where(x => assignmentCodeIds.Contains(x.AssignmentCodeId))
            .Include(x => x.ProductionProcess)
            .Include(x => x.Details).ThenInclude(d => d.HaulDistance)
            .Include(x => ((ScaniaTruckUnitPriceEntity)x).CargoType)
            .Include(x => ((ScaniaTruckUnitPriceEntity)x).ReceivingLocation)
            .Include(x => ((ScaniaTruckUnitPriceEntity)x).DumpingLocation)
            .ToListAsync(cancellationToken);

        var pagedKeySet = pagedKeys.Select(k => (k.AssignmentCodeId, k.StartMonth, k.EndMonth)).ToHashSet();

        var groupsByKey = entities
            .Where(e => pagedKeySet.Contains((e.AssignmentCodeId, e.StartMonth, e.EndMonth)))
            .GroupBy(e => (e.AssignmentCodeId, e.StartMonth, e.EndMonth))
            .ToDictionary(g => g.Key, g => g.ToList());

        var data = pagedKeys.Select(key =>
        {
            var groupEntities = groupsByKey.GetValueOrDefault((key.AssignmentCodeId, key.StartMonth, key.EndMonth)) ?? new List<MechanizedTransportUnitPrice>();

            var sections = groupEntities
                .GroupBy(e => new
                {
                    VehicleType = GetVehicleType(e),
                    e.ProductionProcessId,
                    // Scania-specific grouping
                    CargoTypeId = e is ScaniaTruckUnitPriceEntity scania ? scania.CargoTypeId : (Guid?)null,
                    ReceivingLocationId = e is ScaniaTruckUnitPriceEntity scania2 ? scania2.ReceivingLocationId : (Guid?)null,
                    DumpingLocationId = e is ScaniaTruckUnitPriceEntity scania3 ? scania3.DumpingLocationId : (Guid?)null,
                })
                .Select(sectionGroup => new MechanizedTransportUnitPriceSectionDto
                {
                    VehicleType = sectionGroup.Key.VehicleType,
                    ProductionProcessId = sectionGroup.Key.ProductionProcessId,
                    ProductionProcessName = sectionGroup.First().ProductionProcess?.Name ?? string.Empty,
                    // Scania-specific fields
                    CargoTypeId = sectionGroup.Key.CargoTypeId,
                    CargoTypeName = sectionGroup.Key.VehicleType == MechanizedTransportUnitPriceType.ScaniaTruck
                        ? (sectionGroup.First() is ScaniaTruckUnitPriceEntity s && s.CargoType != null ? s.CargoType.Name : null)
                        : null,
                    ReceivingLocationId = sectionGroup.Key.ReceivingLocationId,
                    ReceivingLocationName = sectionGroup.Key.VehicleType == MechanizedTransportUnitPriceType.ScaniaTruck
                        ? (sectionGroup.First() is ScaniaTruckUnitPriceEntity s2 && s2.ReceivingLocation != null ? s2.ReceivingLocation.Name : null)
                        : null,
                    DumpingLocationId = sectionGroup.Key.DumpingLocationId,
                    DumpingLocationName = sectionGroup.Key.VehicleType == MechanizedTransportUnitPriceType.ScaniaTruck
                        ? (sectionGroup.First() is ScaniaTruckUnitPriceEntity s3 && s3.DumpingLocation != null ? s3.DumpingLocation.Name : null)
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
                        .OrderBy(r => r.EquipmentQuality)
                        .ThenBy(r => r.HaulDistanceValue)
                        .ToList()
                })
                .OrderBy(s => s.VehicleType)
                .ThenBy(s => s.ProductionProcessName)
                .ToList();

            return new MechanizedTransportUnitPriceGroupDto
            {
                AssignmentCodeId = key.AssignmentCodeId,
                AssignmentCodeName = key.AssignmentCodeName,
                StartMonth = key.StartMonth,
                EndMonth = key.EndMonth,
                Sections = sections
            };
        }).ToList();

        return new PaginationResponse<MechanizedTransportUnitPriceGroupDto>(data, totalCount, request.PageIndex, request.PageSize);
    }

    private static MechanizedTransportUnitPriceType GetVehicleType(MechanizedTransportUnitPrice entity) => entity switch
    {
        ScaniaTruckUnitPriceEntity => MechanizedTransportUnitPriceType.ScaniaTruck,
        WasteSuctionTruckUnitPriceEntity => MechanizedTransportUnitPriceType.WasteSuctionTruck,
        ServiceAndCraneVehicleUnitPriceEntity => MechanizedTransportUnitPriceType.ServiceAndCraneVehicle,
        _ => MechanizedTransportUnitPriceType.ExcavatorAndBulldozer
    };
}
