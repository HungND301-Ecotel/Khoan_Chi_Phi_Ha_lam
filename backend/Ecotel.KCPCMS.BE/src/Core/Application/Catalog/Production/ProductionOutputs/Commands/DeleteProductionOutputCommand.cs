using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Production;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Production.ProductionOutputs.Commands;

public record DeleteProductionOutputCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteProductionOutputCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductionOutputCommand, bool>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();

    public async Task<bool> Handle(DeleteProductionOutputCommand request, CancellationToken cancellationToken)
    {
        var existProductionOutput = await _productionOutputRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _productionOutputRepository.Delete(existProductionOutput);
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
