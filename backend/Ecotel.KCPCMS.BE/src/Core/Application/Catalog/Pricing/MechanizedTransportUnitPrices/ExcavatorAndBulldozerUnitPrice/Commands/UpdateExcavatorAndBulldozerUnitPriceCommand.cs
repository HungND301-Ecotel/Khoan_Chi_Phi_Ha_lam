using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;
using Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Commands;

public record UpdateExcavatorAndBulldozerUnitPriceCommand(UpdateExcavatorAndBulldozerUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateExcavatorAndBulldozerUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateExcavatorAndBulldozerUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice>();

    public async Task<bool> Handle(UpdateExcavatorAndBulldozerUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.Id,
            include: x => x.Include(e => e.Details),
            disableTracking: false)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var details = request.UpdateModel.Details.Select(d =>
            new Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportUnitPriceDetailInput(d.HaulDistanceId, d.FuelUnitPrice, null, d.MaintenanceUnitPrice));

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
