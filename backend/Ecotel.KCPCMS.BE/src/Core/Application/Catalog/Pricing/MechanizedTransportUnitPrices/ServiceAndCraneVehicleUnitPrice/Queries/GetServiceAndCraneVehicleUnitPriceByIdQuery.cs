using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ServiceAndCraneVehicleUnitPrice.Queries;

public record GetServiceAndCraneVehicleUnitPriceByIdQuery(Guid Id) : IRequest<ServiceAndCraneVehicleUnitPriceDto>;

public class GetServiceAndCraneVehicleUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetServiceAndCraneVehicleUnitPriceByIdQuery, ServiceAndCraneVehicleUnitPriceDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice>();

    public async Task<ServiceAndCraneVehicleUnitPriceDto> Handle(GetServiceAndCraneVehicleUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.Id,
            include: x => x
                .Include(s => s.AssignmentCode)
                .Include(s => s.ProductionProcess)
                .Include(s => s.Details).ThenInclude(d => d.HaulDistance),
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new ServiceAndCraneVehicleUnitPriceDto
        {
            Id = entity.Id,
            AssignmentCodeId = entity.AssignmentCodeId,
            AssignmentCodeName = entity.AssignmentCode?.Name ?? string.Empty,
            EquipmentQuality = entity.EquipmentQuality,
            ProductionProcessId = entity.ProductionProcessId,
            ProductionProcessName = entity.ProductionProcess?.Name ?? string.Empty,
            StartMonth = entity.StartMonth,
            EndMonth = entity.EndMonth,
            Details = entity.Details.Select(d => new ServiceAndCraneVehicleUnitPriceDetailDto
            {
                Id = d.Id,
                HaulDistanceId = d.HaulDistanceId,
                HaulDistanceValue = d.HaulDistance?.Value,
                FuelUnitPrice = d.FuelUnitPrice,
                MaintenanceUnitPrice = d.MaintenanceUnitPrice
            }).ToList()
        };
    }
}