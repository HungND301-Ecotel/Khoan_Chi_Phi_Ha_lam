using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Commands;

public record CreateWasteSuctionTruckUnitPriceCommand(CreateWasteSuctionTruckUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateWasteSuctionTruckUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateWasteSuctionTruckUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();

    public async Task<bool> Handle(CreateWasteSuctionTruckUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var details = request.CreateModel.Details.Select(d =>
            new MechanizedTransportUnitPriceDetailInput(d.HaulDistanceId, d.FuelUnitPrice, null, d.MaintenanceUnitPrice));

        var entity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice.Create(
            request.CreateModel.AssignmentCodeId,
            request.CreateModel.EquipmentQuality,
            request.CreateModel.ProductionProcessId,
            request.CreateModel.StartMonth,
            request.CreateModel.EndMonth,
            details);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            await _repository.InsertAsync(entity, cancellationToken);
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