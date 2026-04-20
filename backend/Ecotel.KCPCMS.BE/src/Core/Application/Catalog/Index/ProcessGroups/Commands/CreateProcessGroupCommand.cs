using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.MasterData.FixedKeys;
using Application.Dto.Catalog.ProcessGroup;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.MasterData;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;
public record CreateProcessGroupCommand(CreateProcessGroupDto CreateModel) : IRequest<bool>;

public class CreateProcessGroupCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateProcessGroupCommand, bool>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();
    public async Task<bool> Handle(CreateProcessGroupCommand request, CancellationToken cancellationToken)
    {
        if (!request.CreateModel.FixedKeyId.HasValue)
        {
            throw new BadRequestException("FixedKeyId is required.");
        }

        var fixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.CreateModel.FixedKeyId.Value,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var code = fixedKey.Code;
        var fixedKeyId = fixedKey.Id;
        var processGroupType = FixedKeyCodeMapper.ToProcessGroupType(fixedKey);

        if (await codeService.IsCodeExisted(code))
        {
            throw new ConflictException(CustomResponseMessage.ProcessGroupCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var newProcessGroup = ProcessGroup.Create(code, request.CreateModel.Name, fixedKeyId, processGroupType);
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
