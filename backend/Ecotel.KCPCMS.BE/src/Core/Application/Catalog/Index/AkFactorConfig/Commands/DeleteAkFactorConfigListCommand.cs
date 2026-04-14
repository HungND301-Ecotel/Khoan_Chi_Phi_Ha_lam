using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;
using AkFactorConfigEntity = Domain.Entities.Index.AkFactorConfig;

namespace Application.Catalog.Index.AkFactorConfig.Commands;

public record DeleteAkFactorConfigListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteAkFactorConfigListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteAkFactorConfigListCommand, bool>
{
    private readonly IWriteRepository<AkFactorConfigEntity> _AkFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfigEntity>();

    public async Task<bool> Handle(DeleteAkFactorConfigListCommand request, CancellationToken cancellationToken)
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

        var entitiesToDelete = await _AkFactorConfigRepository.GetAllAsync(
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
            _AkFactorConfigRepository.Delete(entitiesToDelete);
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
