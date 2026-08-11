using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Queries;

public record GetScaniaTruckUnitPriceByIdQuery(Guid Id) : IRequest<ScaniaTruckUnitPriceDto>;

public class GetScaniaTruckUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetScaniaTruckUnitPriceByIdQuery, ScaniaTruckUnitPriceDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice>();

    public async Task<ScaniaTruckUnitPriceDto> Handle(GetScaniaTruckUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.Id,
            include: x => x
                .Include(s => s.AssignmentCode)
                .Include(s => s.ProductionProcess)
                .Include(s => s.CargoType)
                .Include(s => s.ReceivingLocation)
                .Include(s => s.DumpingLocation)
                .Include(s => s.Details).ThenInclude(d => d.HaulDistance),
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new ScaniaTruckUnitPriceDto
        {
            Id = entity.Id,
            AssignmentCodeId = entity.AssignmentCodeId,
            AssignmentCodeName = entity.AssignmentCode?.Name ?? string.Empty,
            EquipmentQuality = entity.EquipmentQuality,
            ProductionProcessId = entity.ProductionProcessId,
            ProductionProcessName = entity.ProductionProcess?.Name ?? string.Empty,
            CargoTypeId = entity.CargoTypeId,
            CargoTypeName = entity.CargoType?.Name ?? string.Empty,
            ReceivingLocationId = entity.ReceivingLocationId,
            ReceivingLocationName = entity.ReceivingLocation?.Name,
            DumpingLocationId = entity.DumpingLocationId,
            DumpingLocationName = entity.DumpingLocation?.Name,
            StartMonth = entity.StartMonth,
            EndMonth = entity.EndMonth,
            Details = entity.Details.Select(d => new ScaniaTruckUnitPriceDetailDto
            {
                Id = d.Id,
                HaulDistanceId = d.HaulDistanceId,
                HaulDistanceValue = d.HaulDistance?.Value,
                FuelUnitPrice = d.FuelUnitPrice,
                PowerUnitPrice = d.PowerUnitPrice,
                MaintenanceUnitPrice = d.MaintenanceUnitPrice
            }).ToList()
        };
    }
}