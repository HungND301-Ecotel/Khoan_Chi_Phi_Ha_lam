using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;
public record UpdateProcessGroupCommand(ProcessGroupDto UpdateModel) : IRequest<bool>;

public class UpdateProcessGroupCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdateProcessGroupCommand, bool>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    public async Task<bool> Handle(UpdateProcessGroupCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var existProcessGroup = await _processGroupRepository.GetFirstOrDefaultAsync(
                predicate: t => t.Id == request.UpdateModel.Id,
                include: t => t.Include(t => t.Code),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            if (await codeService.IsCodeExisted(request.UpdateModel.Code, existProcessGroup.CodeId))
            {
                throw new ConflictException(CustomResponseMessage.ProcessGroupCodeAlreadyExists);
            }

            existProcessGroup.Update(request.UpdateModel.Code, request.UpdateModel.Name);

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
