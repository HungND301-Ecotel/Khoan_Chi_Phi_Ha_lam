using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Queries;

public record GetTransportUnitPriceByIdQuery(DefaultIdType Id) : IRequest<TransportUnitPriceDto>;

public class GetTransportUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetTransportUnitPriceByIdQuery, TransportUnitPriceDto>
{
    private readonly IWriteRepository<TransportUnitPriceEntity> _transportUnitPriceRepository = unitOfWork.GetRepository<TransportUnitPriceEntity>();

    public async Task<TransportUnitPriceDto> Handle(GetTransportUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _transportUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: query => query
                .Include(t => t.ProductionProcess)
                .Include(t => t.TransportRoute)
                .Include(t => t.Department)
                .Include(t => t.Equipment),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.TransportUnitPriceNotFound);

        return new TransportUnitPriceDto
        {
            Id = entity.Id,
            ProductionProcessId = entity.ProductionProcessId,
            ProductionProcessName = entity.ProductionProcess?.Name ?? string.Empty,
            TransportRouteId = entity.TransportRouteId,
            TransportRouteName = entity.TransportRoute?.Name,
            DepartmentId = entity.DepartmentId,
            DepartmentName = entity.Department?.Name,
            EquipmentId = entity.EquipmentId,
            EquipmentName = entity.Equipment?.Name,
            EquipmentQuality = entity.EquipmentQuality,
            PowerUnitPrice = entity.PowerUnitPrice,
            MaintenanceUnitPrice = entity.MaintenanceUnitPrice,
            Quantity = entity.Quantity,
            IsLowVolumeCase = entity.IsLowVolumeCase,
            StartMonth = entity.StartMonth,
            EndMonth = entity.EndMonth
        };
    }
}