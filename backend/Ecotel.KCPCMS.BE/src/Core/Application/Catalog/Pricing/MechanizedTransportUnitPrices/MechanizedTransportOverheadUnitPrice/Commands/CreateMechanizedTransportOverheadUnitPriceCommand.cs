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

public record CreateMechanizedTransportOverheadUnitPriceCommand(CreateMechanizedTransportOverheadUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateMechanizedTransportOverheadUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateMechanizedTransportOverheadUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice>();

    public async Task<bool> Handle(CreateMechanizedTransportOverheadUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var normalizedStartMonth = new DateOnly(request.CreateModel.StartMonth.Year, request.CreateModel.StartMonth.Month, 1);
        var normalizedEndMonth = new DateOnly(request.CreateModel.EndMonth.Year, request.CreateModel.EndMonth.Month, 1);

        if (normalizedStartMonth > normalizedEndMonth)
        {
            throw new BadRequestException("Thời gian bắt đầu không được lớn hơn thời gian kết thúc.");
        }

        bool overlapExists = await _repository.ExistsAsync(e =>
            e.DepartmentId == request.CreateModel.DepartmentId &&
            e.ProcessGroupId == request.CreateModel.ProcessGroupId &&
            e.StartMonth <= normalizedEndMonth &&
            e.EndMonth >= normalizedStartMonth);

        if (overlapExists)
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        var entity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice.Create(
            request.CreateModel.ProcessGroupId,
            request.CreateModel.DepartmentId,
            normalizedStartMonth,
            normalizedEndMonth,
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