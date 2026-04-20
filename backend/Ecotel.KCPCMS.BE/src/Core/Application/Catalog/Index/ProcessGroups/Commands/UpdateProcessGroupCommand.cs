using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.MasterData.FixedKeys;
using Application.Dto.Catalog.ProcessGroup;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.MasterData;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;
public record UpdateProcessGroupCommand(ProcessGroupDto UpdateModel) : IRequest<bool>;

public class UpdateProcessGroupCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdateProcessGroupCommand, bool>
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
                include: t => t.Include(t => t.Code).Include(t => t.FixedKey),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            if (!request.UpdateModel.FixedKeyId.HasValue)
            {
                throw new BadRequestException("FixedKeyId is required.");
            }

            var fixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
                predicate: x => x.Id == request.UpdateModel.FixedKeyId.Value,
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            var code = fixedKey.Code;
            Guid? fixedKeyId = fixedKey.Id;
            var processGroupType = FixedKeyCodeMapper.ToProcessGroupType(fixedKey);

            if (await codeService.IsCodeExisted(code, existProcessGroup.CodeId))
            {
                throw new ConflictException(CustomResponseMessage.ProcessGroupCodeAlreadyExists);
            }

            existProcessGroup.Update(code, request.UpdateModel.Name, fixedKeyId, processGroupType);

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
