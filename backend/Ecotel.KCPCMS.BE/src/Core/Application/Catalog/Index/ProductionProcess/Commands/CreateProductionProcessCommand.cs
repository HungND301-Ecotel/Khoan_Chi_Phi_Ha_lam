using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionProcess;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.ProductionProcess.Commands;
public record CreateProductionProcessCommand(CreateProductionProcessDto CreateModel) : IRequest<bool>;

public class CreateProductionProcessCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService)
    : IRequestHandler<CreateProductionProcessCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();

    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(CreateProductionProcessCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.ProductionProcessCodeAlreadyExists);
        }
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var checkProcessGroupExisted =
                await _processGroupRepository
                    .ExistsAsync(x => x.Id == request.CreateModel.ProcessGroupId);

            if (!checkProcessGroupExisted)
            {
                throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
            }

            var newProcessGroup = Domain.Entities.Index.ProductionProcess.Create(request.CreateModel.Code,
                request.CreateModel.Name, request.CreateModel.ProcessGroupId);
            await _productionProcessRepository.InsertAsync(newProcessGroup);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        return true;
    }
}
