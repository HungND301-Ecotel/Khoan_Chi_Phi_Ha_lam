using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.FixedKeys.Commands;

public record DeleteFixedKeyCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteFixedKeyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteFixedKeyCommand, bool>
{
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(DeleteFixedKeyCommand request, CancellationToken cancellationToken)
    {
        var fixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: t => t.Include(t => t.ProcessGroups),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _fixedKeyRepository.Delete(fixedKey);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return true;
    }
}