// File: Application/Catalog/Part/Commands/DeletePartListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Commands.Part;

public record DeletePartListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeletePartListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeletePartListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeletePartListCommand request, CancellationToken cancellationToken)
    {
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        var partsToDelete = await _partRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: t => t.Include(t => t.Code),
            disableTracking: false);

        if (partsToDelete == null || !partsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (partsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.PartNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _partRepository.Delete(partsToDelete);
            _codeRepository.Delete(partsToDelete.Select(p => p.Code).ToList());
            await unitOfWork.SaveChangesAsync();

            await unitOfWork.CommitAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
