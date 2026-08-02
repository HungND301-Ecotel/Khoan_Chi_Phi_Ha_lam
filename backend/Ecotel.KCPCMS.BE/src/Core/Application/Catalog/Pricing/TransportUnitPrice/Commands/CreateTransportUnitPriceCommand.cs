using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportUnitPrice;
using MediatR;
using Shared.Constants;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Commands;

public record CreateTransportUnitPriceCommand(CreateTransportUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateTransportUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateTransportUnitPriceCommand, bool>
{
    private readonly IWriteRepository<TransportUnitPriceEntity> _transportUnitPriceRepository = unitOfWork.GetRepository<TransportUnitPriceEntity>();

    public async Task<bool> Handle(CreateTransportUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var model = request.CreateModel;

        if (model.TransportRouteId.HasValue && model.EquipmentId.HasValue)
        {
            throw new BadRequestException(CustomResponseMessage.TransportUnitPriceRouteEquipmentConflict);
        }

        var newTransportUnitPrice = TransportUnitPriceEntity.Create(
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

        await _transportUnitPriceRepository.InsertAsync(newTransportUnitPrice, cancellationToken);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}