using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;
using Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Queries;

public record GetExcavatorAndBulldozerUnitPriceByIdQuery(Guid Id) : IRequest<ExcavatorAndBulldozerUnitPriceDto>;

public class GetExcavatorAndBulldozerUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetExcavatorAndBulldozerUnitPriceByIdQuery, ExcavatorAndBulldozerUnitPriceDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice>();

    public async Task<ExcavatorAndBulldozerUnitPriceDto> Handle(GetExcavatorAndBulldozerUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.Id,
            include: x => x
                .Include(e => e.AssignmentCode)
                .Include(e => e.ProductionProcess)
                .Include(e => e.Details).ThenInclude(d => d.HaulDistance),
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new ExcavatorAndBulldozerUnitPriceDto
        {
            Id = entity.Id,
            AssignmentCodeId = entity.AssignmentCodeId,
            AssignmentCodeName = entity.AssignmentCode?.Name ?? string.Empty,
            EquipmentQuality = entity.EquipmentQuality,
            ProductionProcessId = entity.ProductionProcessId,
            ProductionProcessName = entity.ProductionProcess?.Name ?? string.Empty,
            StartMonth = entity.StartMonth,
            EndMonth = entity.EndMonth,
            Details = entity.Details.Select(d => new ExcavatorAndBulldozerUnitPriceDetailDto
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
