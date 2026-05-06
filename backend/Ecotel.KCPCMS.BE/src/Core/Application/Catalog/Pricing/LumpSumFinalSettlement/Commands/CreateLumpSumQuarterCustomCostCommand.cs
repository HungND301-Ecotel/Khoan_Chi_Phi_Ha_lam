using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.Pricing.LumpSumFinalSettlement;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Domain.Entities.Production;
using MediatR;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Commands;

public record CreateLumpSumQuarterCustomCostCommand(CreateLumpSumQuarterCustomCostRequest CreateModel) : IRequest<bool>;

public class CreateLumpSumQuarterCustomCostCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateLumpSumQuarterCustomCostCommand, bool>
{
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();

    public async Task<bool> Handle(CreateLumpSumQuarterCustomCostCommand request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.CreateModel.Month, out var month) || month < 1 || month > 12)
        {
            throw new BadRequestException("Invalid quarter");
        }

        if (!int.TryParse(request.CreateModel.Year, out var year))
        {
            throw new BadRequestException("Invalid year");
        }

        Guid? processGroupId = null;
        if (!string.IsNullOrWhiteSpace(request.CreateModel.ProcessGroupId))
        {
            if (!Guid.TryParse(request.CreateModel.ProcessGroupId, out var parsedProcessGroupId))
            {
                throw new BadRequestException("Invalid process group id");
            }

            processGroupId = parsedProcessGroupId;
        }

        var customName = request.CreateModel.CustomName?.Trim() ?? string.Empty;
        if (LumpSumFinalSettlementSpecialQuantityKeys.IsSpecialQuantityKey(customName))
        {
            throw new BadRequestException("Custom name is reserved");
        }

        var entity = LumpSumQuarterCustomCost.Create(
            month,
            year,
            processGroupId,
            customName,
            request.CreateModel.ActualQuantity,
            request.CreateModel.MaterialUnitPrice,
            request.CreateModel.MaintainUnitPrice,
            request.CreateModel.ElectricityUnitPrice);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            await _customCostRepository.InsertAsync(entity, cancellationToken);
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
