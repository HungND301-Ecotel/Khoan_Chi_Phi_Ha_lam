using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Production;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Commands;

public record DeleteLumpSumQuarterCustomCostCommand(Guid DeleteId) : IRequest<bool>;

public class DeleteLumpSumQuarterCustomCostCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteLumpSumQuarterCustomCostCommand, bool>
{
    private readonly IWriteRepository<LumpSumQuarterCustomCost> _customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();

    public async Task<bool> Handle(DeleteLumpSumQuarterCustomCostCommand request, CancellationToken cancellationToken)
    {
        var entity = await _customCostRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.DeleteId,
            disableTracking: true)
            ?? throw new NotFoundException(MessageCommon.DataNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _customCostRepository.Delete(entity);
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
