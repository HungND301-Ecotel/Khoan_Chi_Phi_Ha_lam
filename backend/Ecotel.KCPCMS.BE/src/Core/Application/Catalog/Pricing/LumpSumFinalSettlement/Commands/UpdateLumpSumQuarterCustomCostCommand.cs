using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Domain.Entities.Production;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Commands;

public record UpdateLumpSumQuarterCustomCostCommand(UpdateLumpSumQuarterCustomCostRequest UpdateModel) : IRequest<bool>;

public class UpdateLumpSumQuarterCustomCostCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateLumpSumQuarterCustomCostCommand, bool>
{
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();

    public async Task<bool> Handle(UpdateLumpSumQuarterCustomCostCommand request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.UpdateModel.Month, out var month) || month < 1 || month > 12)
        {
            throw new BadRequestException("Invalid quarter");
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

        var entity = await _customCostRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.Id,
            disableTracking: true)
            ?? throw new NotFoundException(MessageCommon.DataNotFound);

        entity.Update(
            month,
            year,
            processGroupId,
            request.UpdateModel.CustomName?.Trim() ?? string.Empty,
            request.UpdateModel.ActualQuantity,
            request.UpdateModel.MaterialUnitPrice,
            request.UpdateModel.MaintainUnitPrice,
            request.UpdateModel.ElectricityUnitPrice);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _customCostRepository.Update(entity);
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
