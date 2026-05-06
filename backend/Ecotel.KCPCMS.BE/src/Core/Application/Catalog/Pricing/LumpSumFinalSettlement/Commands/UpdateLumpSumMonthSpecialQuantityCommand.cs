using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.Pricing.LumpSumFinalSettlement;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Commands;

public record UpdateLumpSumMonthSpecialQuantityCommand(UpdateLumpSumMonthSpecialQuantityRequest UpdateModel) : IRequest<bool>;

public class UpdateLumpSumMonthSpecialQuantityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLumpSumMonthSpecialQuantityCommand, bool>
{
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository =
        unitOfWork.GetRepository<LumpSumQuarterCustomCost>();

    public async Task<bool> Handle(UpdateLumpSumMonthSpecialQuantityCommand request, CancellationToken cancellationToken)
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

        var specialCosts = await _customCostRepository.GetAll()
            .Where(x => x.Month == month
                && x.Year == year
                && x.ProcessGroupId == processGroupId
                && (x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.CoalExcavation
                    || x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.CoalCrosscut))
            .ToListAsync(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var excavation = specialCosts.FirstOrDefault(x =>
                x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.CoalExcavation);
            var crosscut = specialCosts.FirstOrDefault(x =>
                x.CustomName == LumpSumFinalSettlementSpecialQuantityKeys.CoalCrosscut);

            if (excavation == null)
            {
                excavation = LumpSumQuarterCustomCost.Create(
                    month,
                    year,
                    processGroupId,
                    LumpSumFinalSettlementSpecialQuantityKeys.CoalExcavation,
                    request.UpdateModel.CoalExcavationActualQuantity,
                    0,
                    0,
                    0);
                await _customCostRepository.InsertAsync(excavation, cancellationToken);
            }
            else
            {
                excavation.Update(
                    month,
                    year,
                    processGroupId,
                    LumpSumFinalSettlementSpecialQuantityKeys.CoalExcavation,
                    request.UpdateModel.CoalExcavationActualQuantity,
                    0,
                    0,
                    0);
                _customCostRepository.Update(excavation);
            }

            if (crosscut == null)
            {
                crosscut = LumpSumQuarterCustomCost.Create(
                    month,
                    year,
                    processGroupId,
                    LumpSumFinalSettlementSpecialQuantityKeys.CoalCrosscut,
                    request.UpdateModel.CoalCrosscutActualQuantity,
                    0,
                    0,
                    0);
                await _customCostRepository.InsertAsync(crosscut, cancellationToken);
            }
            else
            {
                crosscut.Update(
                    month,
                    year,
                    processGroupId,
                    LumpSumFinalSettlementSpecialQuantityKeys.CoalCrosscut,
                    request.UpdateModel.CoalCrosscutActualQuantity,
                    0,
                    0,
                    0);
                _customCostRepository.Update(crosscut);
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
