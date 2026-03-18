// File: Application/Catalog/ProductionProcess/Commands/DeleteProductionProcessListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProductionProcess.Commands;

public record DeleteProductionProcessListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteProductionProcessListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductionProcessListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeleteProductionProcessListCommand request, CancellationToken cancellationToken)
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

        var processesToDelete = await _productionProcessRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: t => t.Include(t => t.StoneClampRatios).Include(t => t.Code),
            disableTracking: true);

        if (processesToDelete == null || !processesToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (processesToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ProductionProcessNotFound);
        }

        var codes = processesToDelete.Select(p => p.Code);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _productionProcessRepository.Delete(processesToDelete);
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