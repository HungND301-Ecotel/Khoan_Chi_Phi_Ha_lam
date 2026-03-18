using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AssignmentCodes.Commands
{
    public record DeleteAssignmentCodeCommand(DefaultIdType DeleteId) : IRequest<bool>;

    public class DeleteAssignmentCodeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteAssignmentCodeCommand, bool>
    {
        private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
        private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
        public async Task<bool> Handle(DeleteAssignmentCodeCommand request, CancellationToken cancellationToken)
        {
            var existAssignmentCode = await _assignmentCodeRepository.GetFirstOrDefaultAsync(
                predicate: t => t.Id == request.DeleteId,
                include: t => t.Include(t => t.Materials).Include(t => t.Code),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);


            await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
            try
            {
                _assignmentCodeRepository.Delete(existAssignmentCode);
                _codeRepository.Delete(existAssignmentCode.Code);
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
}
