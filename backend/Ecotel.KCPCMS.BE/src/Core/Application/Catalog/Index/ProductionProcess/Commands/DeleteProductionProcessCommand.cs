using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProductionProcess.Commands;
public record DeleteProductionProcessCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteProductionProcessCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductionProcessCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(DeleteProductionProcessCommand request, CancellationToken cancellationToken)
    {
        var existProductionProcess = await _productionProcessRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: t => t.Include(t => t.StoneClampRatios).Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _productionProcessRepository.Delete(existProductionProcess);
            _codeRepository.Delete(existProductionProcess.Code);
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

