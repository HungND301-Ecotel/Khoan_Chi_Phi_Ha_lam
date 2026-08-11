using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Commands;

public record UpdateScaniaTruckUnitPriceCommand(UpdateScaniaTruckUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateScaniaTruckUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateScaniaTruckUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice>();

    public async Task<bool> Handle(UpdateScaniaTruckUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.Id,
            include: x => x.Include(s => s.Details),
            disableTracking: false)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var details = request.UpdateModel.Details.Select(d =>
            new MechanizedTransportUnitPriceDetailInput(d.HaulDistanceId, d.FuelUnitPrice, d.PowerUnitPrice, d.MaintenanceUnitPrice));

        entity.Update(
            request.UpdateModel.AssignmentCodeId,
            request.UpdateModel.EquipmentQuality,
            request.UpdateModel.ProductionProcessId,
            request.UpdateModel.CargoTypeId,
            request.UpdateModel.ReceivingLocationId,
            request.UpdateModel.DumpingLocationId,
            request.UpdateModel.StartMonth,
            request.UpdateModel.EndMonth,
            details);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _repository.Update(entity);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}