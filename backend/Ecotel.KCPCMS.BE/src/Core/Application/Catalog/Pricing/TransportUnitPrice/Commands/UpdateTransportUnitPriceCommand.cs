using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportUnitPrice;
using MediatR;
using Shared.Constants;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Commands;

public record UpdateTransportUnitPriceCommand(TransportUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateTransportUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateTransportUnitPriceCommand, bool>
{
    private readonly IWriteRepository<TransportUnitPriceEntity> _transportUnitPriceRepository = unitOfWork.GetRepository<TransportUnitPriceEntity>();

    public async Task<bool> Handle(UpdateTransportUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var model = request.UpdateModel;

        if (model.TransportRouteId.HasValue && model.EquipmentId.HasValue)
        {
            throw new BadRequestException(CustomResponseMessage.TransportUnitPriceRouteEquipmentConflict);
        }

        var existTransportUnitPrice = await _transportUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == model.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.TransportUnitPriceNotFound);

        existTransportUnitPrice.Update(
            model.ProductionProcessId,
            model.TransportRouteId,
            model.DepartmentId,
            model.EquipmentId,
            model.EquipmentQuality,
            model.MaterialFuelUnitPrice,
            model.PowerUnitPrice,
            model.MaintenanceUnitPrice,
            model.Quantity,
            model.IsLowVolumeCase,
            model.StartMonth,
            model.EndMonth);

        _transportUnitPriceRepository.Update(existTransportUnitPrice);
        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitAsync();
        return true;
    }
}