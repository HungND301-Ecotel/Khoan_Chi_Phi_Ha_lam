using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Product.Commands;
public record DeleteProductCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteProductCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Product> _productRepository = unitOfWork.GetRepository<Domain.Entities.Index.Product>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var existProduct = await _productRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: t => t.Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _productRepository.Delete(existProduct);
            _codeRepository.Delete(existProduct.Code);
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
