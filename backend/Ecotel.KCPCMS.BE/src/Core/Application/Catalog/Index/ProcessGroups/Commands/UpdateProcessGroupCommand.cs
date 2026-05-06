using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;
public record UpdateProcessGroupCommand(UpdateProcessGroupDto UpdateModel) : IRequest<bool>;

public class UpdateProcessGroupCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateProcessGroupCommand, bool>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(UpdateProcessGroupCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var existProcessGroup = await _processGroupRepository.GetFirstOrDefaultAsync(
                predicate: t => t.Id == request.UpdateModel.Id,
                include: t => t.Include(t => t.Code),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            if (request.UpdateModel.FixedKeyId == null || request.UpdateModel.FixedKeyId == Guid.Empty)
            {
                throw new BadRequestException(CustomResponseMessage.ProcessGroupIdCannotBeEmpty);
            }

            var fixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
                predicate: x => x.Id == request.UpdateModel.FixedKeyId,
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            if (await _processGroupRepository.GetFirstOrDefaultAsync(
                    predicate: x => x.FixedKeyId == request.UpdateModel.FixedKeyId && x.Id != request.UpdateModel.Id,
                    disableTracking: true) != null)
            {
                throw new ConflictException(CustomResponseMessage.ProcessGroupCodeAlreadyExists);
            }

            existProcessGroup.Update(fixedKey, request.UpdateModel.Name);

            _processGroupRepository.Update(existProcessGroup);
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
