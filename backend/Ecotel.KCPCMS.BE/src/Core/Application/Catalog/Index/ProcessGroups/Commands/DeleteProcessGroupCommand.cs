using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;
public record DeleteProcessGroupCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteProcessGroupCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteProcessGroupCommand, bool>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(DeleteProcessGroupCommand request, CancellationToken cancellationToken)
    {
        var existProcessGroup = await _processGroupRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: x => x.Include(x => x.ProductionProcesses).Include(x => x.AdjustmentFactors).Include(x => x.Products).Include(x => x.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _processGroupRepository.Delete(existProcessGroup);
            _codeRepository.Delete(existProcessGroup.Code);
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
