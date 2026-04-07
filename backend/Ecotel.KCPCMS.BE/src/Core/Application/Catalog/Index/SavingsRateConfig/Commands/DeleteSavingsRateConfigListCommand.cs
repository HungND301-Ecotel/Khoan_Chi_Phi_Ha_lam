using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;
using SavingsRateConfigEntity = Domain.Entities.Index.SavingsRateConfig;

namespace Application.Catalog.Index.SavingsRateConfig.Commands;

public record DeleteSavingsRateConfigListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteSavingsRateConfigListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteSavingsRateConfigListCommand, bool>
{
    private readonly IWriteRepository<SavingsRateConfigEntity> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfigEntity>();

    public async Task<bool> Handle(DeleteSavingsRateConfigListCommand request, CancellationToken cancellationToken)
    {
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (distinctIds.Count != request.DeleteIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.DeletedIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        var entitiesToDelete = await _savingsRateConfigRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (entitiesToDelete == null || !entitiesToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (entitiesToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.EntityNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _savingsRateConfigRepository.Delete(entitiesToDelete);
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
