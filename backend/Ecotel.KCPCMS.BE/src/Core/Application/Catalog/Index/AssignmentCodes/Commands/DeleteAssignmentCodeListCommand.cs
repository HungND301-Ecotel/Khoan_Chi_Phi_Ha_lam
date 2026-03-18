using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AssignmentCodes.Commands;
public record DeleteAssignmentCodeListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteAssignmentCodeListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteAssignmentCodeListCommand, bool>
{
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(DeleteAssignmentCodeListCommand request, CancellationToken cancellationToken)
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

        var assignmentCodesToDelete = await _assignmentCodeRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: t => t.Include(t => t.Materials).Include(t => t.Code),
            disableTracking: true);

        if (assignmentCodesToDelete == null || !assignmentCodesToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (assignmentCodesToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        var codes = assignmentCodesToDelete.Select(a => a.Code);
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _assignmentCodeRepository.Delete(assignmentCodesToDelete);
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
