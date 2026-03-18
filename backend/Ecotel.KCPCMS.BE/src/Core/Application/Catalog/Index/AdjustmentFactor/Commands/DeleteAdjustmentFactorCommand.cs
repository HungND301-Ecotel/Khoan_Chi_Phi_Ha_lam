using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;
public record DeleteAdjustmentFactorCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteAdjustmentFactorCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteAdjustmentFactorCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactoRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(DeleteAdjustmentFactorCommand request, CancellationToken cancellationToken)
    {
        var existAdjustmentFactor = await _adjustmentFactoRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: aj => aj.Include(aj => aj.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _adjustmentFactoRepository.Delete(existAdjustmentFactor);
            _codeRepository.Delete(existAdjustmentFactor.Code);
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
