using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;
public record CreateProcessGroupCommand(CreateProcessGroupDto CreateModel) : IRequest<bool>;

public class CreateProcessGroupCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateProcessGroupCommand, bool>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    public async Task<bool> Handle(CreateProcessGroupCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.ProcessGroupCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var newProcessGroup = ProcessGroup.Create(request.CreateModel.Code, request.CreateModel.Name);
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
