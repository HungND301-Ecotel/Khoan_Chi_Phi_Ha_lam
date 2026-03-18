using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;

public record DeleteProcessGroupListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteProcessGroupListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteProcessGroupListCommand, bool>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeleteProcessGroupListCommand request, CancellationToken cancellationToken)
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

        var processGroupsToDelete = await _processGroupRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: x => x.Include(x => x.ProductionProcesses).Include(x => x.AdjustmentFactors).Include(x => x.Products).Include(x => x.Code),
            disableTracking: true);

        if (processGroupsToDelete == null || !processGroupsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (processGroupsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
        }

        var codes = processGroupsToDelete.Select(p => p.Code);
        // 3. Xóa an toàn trong transaction
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _processGroupRepository.Delete(processGroupsToDelete);
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