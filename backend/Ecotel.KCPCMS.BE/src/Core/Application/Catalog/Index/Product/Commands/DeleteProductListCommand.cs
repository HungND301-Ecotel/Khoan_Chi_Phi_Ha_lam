// File: Application/Catalog/Product/Commands/DeleteProductListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Product.Commands;

public record DeleteProductListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteProductListCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Product> _productRepository = unitOfWork.GetRepository<Domain.Entities.Index.Product>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(DeleteProductListCommand request, CancellationToken cancellationToken)
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

        var productsToDelete = await _productRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: x => x.Include(x => x.Code),
            disableTracking: true);

        if (productsToDelete == null || !productsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (productsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var codes = productsToDelete.Select(p => p.Code);
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _productRepository.Delete(productsToDelete);
            _codeRepository.Delete(codes);
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