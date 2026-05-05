using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;
public record CreateProcessGroupCommand(CreateProcessGroupDto CreateModel) : IRequest<bool>;

public class CreateProcessGroupCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateProcessGroupCommand, bool>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(CreateProcessGroupCommand request, CancellationToken cancellationToken)
    {
        var fixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.CreateModel.FixedKeyId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await _processGroupRepository.GetFirstOrDefaultAsync(
                predicate: x => x.FixedKeyId == request.CreateModel.FixedKeyId,
                disableTracking: true) != null)
        {
            throw new ConflictException(CustomResponseMessage.ProcessGroupCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var newProcessGroup = ProcessGroup.Create(fixedKey, request.CreateModel.Name);
            await _processGroupRepository.InsertAsync(newProcessGroup, cancellationToken);
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
