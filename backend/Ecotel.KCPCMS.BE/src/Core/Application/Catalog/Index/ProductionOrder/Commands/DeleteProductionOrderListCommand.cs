// File: Application/Catalog/Passport/Commands/DeleteProductionOrderListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Passport.Commands;

public record DeleteProductionOrderListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteProductionOrderListCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductionOrderListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionOrder>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeleteProductionOrderListCommand request, CancellationToken cancellationToken)
    {
        // 1. Loại bỏ trùng lặp
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (distinctIds.Count != request.DeleteIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.DeletedIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        var productionOrderToDelete = await _productionOrderRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: x => x.Include(x => x.Code!),
            disableTracking: true);

        if (productionOrderToDelete == null || !productionOrderToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (productionOrderToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.PassportNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _productionOrderRepository.Delete(productionOrderToDelete);
            _codeRepository.Delete(productionOrderToDelete.Select(p => p.Code!));
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