using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Commands;

public record CreateMechanizedTransportOverheadUnitPriceCommand(CreateMechanizedTransportOverheadUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateMechanizedTransportOverheadUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateMechanizedTransportOverheadUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice>();

    public async Task<bool> Handle(CreateMechanizedTransportOverheadUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var entity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice.Create(
            request.CreateModel.ProcessGroupId,
            request.CreateModel.StartMonth,
            request.CreateModel.EndMonth,
            request.CreateModel.LowValuePerishableSupplyUnitPrice,
            request.CreateModel.ElectricityUnitPrice);

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