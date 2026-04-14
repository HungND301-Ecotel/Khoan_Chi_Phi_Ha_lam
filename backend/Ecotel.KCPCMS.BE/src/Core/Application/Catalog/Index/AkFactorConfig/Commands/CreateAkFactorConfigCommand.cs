using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AkFactorConfig;
using MediatR;
using Shared.Constants;
using AkFactorConfigEntity = Domain.Entities.Index.AkFactorConfig;
using ProcessGroup = Domain.Entities.Index.ProcessGroup;

namespace Application.Catalog.Index.AkFactorConfig.Commands;

public record CreateAkFactorConfigCommand(CreateAkFactorConfigDto CreateModel) : IRequest<bool>;

public class CreateAkFactorConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateAkFactorConfigCommand, bool>
{
    private readonly IWriteRepository<AkFactorConfigEntity> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfigEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(CreateAkFactorConfigCommand request, CancellationToken cancellationToken)
    {
        var processGroupExists = await _processGroupRepository.ExistsAsync(x => x.Id == request.CreateModel.ProcessGroupId);
        if (!processGroupExists)
        {
            throw new Application.Common.Exceptions.NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
        }

        var newAkFactorConfig = AkFactorConfigEntity.Create(
            request.CreateModel.ProcessGroupId,
            request.CreateModel.AkDiffDisplay,
            request.CreateModel.AdjustmentRateDisplay,
            request.CreateModel.Description);

        await _akFactorConfigRepository.InsertAsync(newAkFactorConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
