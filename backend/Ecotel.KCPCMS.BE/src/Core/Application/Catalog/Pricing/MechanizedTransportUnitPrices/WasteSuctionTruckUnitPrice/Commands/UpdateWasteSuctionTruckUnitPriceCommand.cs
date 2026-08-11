using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Commands;

public record UpdateWasteSuctionTruckUnitPriceCommand(UpdateWasteSuctionTruckUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateWasteSuctionTruckUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateWasteSuctionTruckUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();

    public async Task<bool> Handle(UpdateWasteSuctionTruckUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.Id,
            include: x => x.Include(w => w.Details),
            disableTracking: false)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var details = request.UpdateModel.Details.Select(d =>
            new MechanizedTransportUnitPriceDetailInput(d.HaulDistanceId, d.FuelUnitPrice, null, d.MaintenanceUnitPrice));

        entity.Update(
            request.UpdateModel.AssignmentCodeId,
            request.UpdateModel.EquipmentQuality,
            request.UpdateModel.ProductionProcessId,
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