using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.FixedKeys.Commands;

public record DeleteFixedKeyListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteFixedKeyListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteFixedKeyListCommand, bool>
{
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(DeleteFixedKeyListCommand request, CancellationToken cancellationToken)
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

        var fixedKeys = await _fixedKeyRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: x => x.Include(x => x.ProcessGroups),
            disableTracking: true);

        if (fixedKeys == null || !fixedKeys.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (fixedKeys.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _fixedKeyRepository.Delete(fixedKeys);
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