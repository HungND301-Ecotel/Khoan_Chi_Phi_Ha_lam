using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record DeleteNormFactorListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteNormFactorListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteNormFactorListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.NormFactor> _normFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.NormFactor>();

    public async Task<bool> Handle(DeleteNormFactorListCommand request, CancellationToken cancellationToken)
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

        var factorsToDelete = await _normFactorRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: q => q.Include(nf => nf.NormFactorAssignmentCodes),
            disableTracking: true);

        if (factorsToDelete == null || !factorsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (factorsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _normFactorRepository.Delete(factorsToDelete);
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