using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Commands;

public record CreateScaniaTruckUnitPriceCommand(CreateScaniaTruckUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateScaniaTruckUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateScaniaTruckUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice>();

    public async Task<bool> Handle(CreateScaniaTruckUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var details = request.CreateModel.Details.Select(d =>
            new MechanizedTransportUnitPriceDetailInput(d.HaulDistanceId, d.FuelUnitPrice, d.PowerUnitPrice, d.MaintenanceUnitPrice));

        var entity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice.Create(
            request.CreateModel.AssignmentCodeId,
            request.CreateModel.EquipmentQuality,
            request.CreateModel.ProductionProcessId,
            request.CreateModel.CargoTypeId,
            request.CreateModel.ReceivingLocationId,
            request.CreateModel.DumpingLocationId,
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