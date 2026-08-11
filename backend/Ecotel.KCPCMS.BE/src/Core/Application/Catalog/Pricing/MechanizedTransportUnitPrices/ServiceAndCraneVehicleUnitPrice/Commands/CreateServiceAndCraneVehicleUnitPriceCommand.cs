using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ServiceAndCraneVehicleUnitPrice.Commands;

public record CreateServiceAndCraneVehicleUnitPriceCommand(CreateServiceAndCraneVehicleUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateServiceAndCraneVehicleUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateServiceAndCraneVehicleUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice>();

    public async Task<bool> Handle(CreateServiceAndCraneVehicleUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var details = request.CreateModel.Details.Select(d =>
            new MechanizedTransportUnitPriceDetailInput(d.HaulDistanceId, d.FuelUnitPrice, null, d.MaintenanceUnitPrice));

        var entity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice.Create(
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