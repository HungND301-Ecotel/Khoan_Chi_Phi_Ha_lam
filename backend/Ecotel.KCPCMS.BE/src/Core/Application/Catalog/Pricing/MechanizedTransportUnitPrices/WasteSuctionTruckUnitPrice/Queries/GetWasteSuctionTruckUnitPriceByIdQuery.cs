using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Queries;

public record GetWasteSuctionTruckUnitPriceByIdQuery(Guid Id) : IRequest<WasteSuctionTruckUnitPriceDto>;

public class GetWasteSuctionTruckUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetWasteSuctionTruckUnitPriceByIdQuery, WasteSuctionTruckUnitPriceDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();

    public async Task<WasteSuctionTruckUnitPriceDto> Handle(GetWasteSuctionTruckUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.Id,
            include: x => x
                .Include(w => w.AssignmentCode)
                .Include(w => w.ProductionProcess)
                .Include(w => w.Details).ThenInclude(d => d.HaulDistance),
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new WasteSuctionTruckUnitPriceDto
        {
            Id = entity.Id,
            AssignmentCodeId = entity.AssignmentCodeId,
            AssignmentCodeName = entity.AssignmentCode?.Name ?? string.Empty,
            EquipmentQuality = entity.EquipmentQuality,
            ProductionProcessId = entity.ProductionProcessId,
            ProductionProcessName = entity.ProductionProcess?.Name ?? string.Empty,
            StartMonth = entity.StartMonth,
            EndMonth = entity.EndMonth,
            Details = entity.Details.Select(d => new WasteSuctionTruckUnitPriceDetailDto
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