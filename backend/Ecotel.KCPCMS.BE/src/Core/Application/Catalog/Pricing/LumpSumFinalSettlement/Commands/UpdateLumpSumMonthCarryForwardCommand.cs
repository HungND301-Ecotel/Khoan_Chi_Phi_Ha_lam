using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Commands;

public record UpdateLumpSumMonthCarryForwardCommand(UpdateLumpSumMonthCarryForwardRequest UpdateModel) : IRequest<bool>;

public class UpdateLumpSumMonthCarryForwardCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLumpSumMonthCarryForwardCommand, bool>
{
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository =
        unitOfWork.GetRepository<LumpSumQuarterCustomCost>();

    public async Task<bool> Handle(UpdateLumpSumMonthCarryForwardCommand request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.UpdateModel.Month, out var month) || month < 1 || month > 12)
        {
            throw new BadRequestException("Invalid month");
        }

        if (!int.TryParse(request.UpdateModel.Year, out var year))
        {
            throw new BadRequestException("Invalid year");
        }

        Guid? processGroupId = null;
        if (!string.IsNullOrWhiteSpace(request.UpdateModel.ProcessGroupId))
        {
            if (!Guid.TryParse(request.UpdateModel.ProcessGroupId, out var parsedProcessGroupId))
            {
                throw new BadRequestException("Invalid process group id");
            }

            processGroupId = parsedProcessGroupId;
        }

        var carryForward = await _customCostRepository.GetAll()
            .Where(x => x.Month == month
                && x.Year == year
                && x.ProcessGroupId == processGroupId
                && x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.SavingCarryForward)
            .FirstOrDefaultAsync(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (carryForward == null)
            {
                carryForward = LumpSumQuarterCustomCost.Create(
                    month,
                    year,
                    processGroupId,
                    LumpSumFinalSettlementSpecialQuantityKeys.SavingCarryForward,
                    request.UpdateModel.SavingCarryForwardToNextMonths,
                    0,
                    0,
                    0);
                await _customCostRepository.InsertAsync(carryForward, cancellationToken);
            }
            else
            {
                carryForward.Update(
                    month,
                    year,
                    processGroupId,
                    LumpSumFinalSettlementSpecialQuantityKeys.SavingCarryForward,
                    request.UpdateModel.SavingCarryForwardToNextMonths,
                    0,
                    0,
                    0);
                _customCostRepository.Update(carryForward);
            }

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
