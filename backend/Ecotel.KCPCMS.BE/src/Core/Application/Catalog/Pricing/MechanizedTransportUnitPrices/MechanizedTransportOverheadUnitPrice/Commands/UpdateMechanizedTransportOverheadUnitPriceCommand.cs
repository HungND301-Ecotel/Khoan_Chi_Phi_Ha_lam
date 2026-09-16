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

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Commands;

public record UpdateMechanizedTransportOverheadUnitPriceCommand(UpdateMechanizedTransportOverheadUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateMechanizedTransportOverheadUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateMechanizedTransportOverheadUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice>();

    public async Task<bool> Handle(UpdateMechanizedTransportOverheadUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.Id,
            disableTracking: false)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var normalizedStartMonth = new DateOnly(request.UpdateModel.StartMonth.Year, request.UpdateModel.StartMonth.Month, 1);
        var normalizedEndMonth = new DateOnly(request.UpdateModel.EndMonth.Year, request.UpdateModel.EndMonth.Month, 1);

        if (normalizedStartMonth > normalizedEndMonth)
        {
            throw new BadRequestException("Thời gian bắt đầu không được lớn hơn thời gian kết thúc.");
        }

        bool overlapExists = await _repository.ExistsAsync(e =>
            e.Id != request.UpdateModel.Id &&
            e.DepartmentId == request.UpdateModel.DepartmentId &&
            e.ProcessGroupId == request.UpdateModel.ProcessGroupId &&
            e.StartMonth <= normalizedEndMonth &&
            e.EndMonth >= normalizedStartMonth);

        if (overlapExists)
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        entity.Update(
            request.UpdateModel.ProcessGroupId,
            request.UpdateModel.DepartmentId,
            normalizedStartMonth,
            normalizedEndMonth,
            request.UpdateModel.LowValuePerishableSupplyUnitPrice,
            request.UpdateModel.ElectricityUnitPrice);

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
