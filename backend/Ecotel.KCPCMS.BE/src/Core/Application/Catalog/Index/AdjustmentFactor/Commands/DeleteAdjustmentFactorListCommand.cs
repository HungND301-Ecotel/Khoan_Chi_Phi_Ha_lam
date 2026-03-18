// File: Application/Catalog/AdjustmentFactor/Commands/DeleteAdjustmentFactorListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record DeleteAdjustmentFactorListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteAdjustmentFactorListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteAdjustmentFactorListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeleteAdjustmentFactorListCommand request, CancellationToken cancellationToken)
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

        var factorsToDelete = await _adjustmentFactorRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: q => q.Include(af => af.AdjustmentFactorDescriptions).Include(af => af.Code),
            disableTracking: true);

        if (factorsToDelete == null || !factorsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (factorsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        var codes = factorsToDelete.Select(f => f.Code);
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _adjustmentFactorRepository.Delete(factorsToDelete);
            _codeRepository.Delete(codes);
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