using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Commands;

public record CreateExcavatorAndBulldozerUnitPriceCommand(CreateExcavatorAndBulldozerUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateExcavatorAndBulldozerUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateExcavatorAndBulldozerUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice>();

    public async Task<bool> Handle(CreateExcavatorAndBulldozerUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var details = request.CreateModel.Details.Select(d =>
            new MechanizedTransportUnitPriceDetailInput(d.HaulDistanceId, d.FuelUnitPrice, null, d.MaintenanceUnitPrice));

        var entity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice.Create(
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
