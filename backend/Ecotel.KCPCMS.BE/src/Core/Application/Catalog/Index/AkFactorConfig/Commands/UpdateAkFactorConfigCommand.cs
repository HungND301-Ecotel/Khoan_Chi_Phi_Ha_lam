using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AkFactorConfig;
using MediatR;
using Shared.Constants;
using AkFactorConfigEntity = Domain.Entities.Index.AkFactorConfig;
using ProcessGroup = Domain.Entities.Index.ProcessGroup;

namespace Application.Catalog.Index.AkFactorConfig.Commands;

public record UpdateAkFactorConfigCommand(AkFactorConfigDto UpdateModel) : IRequest<bool>;

public class UpdateAkFactorConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAkFactorConfigCommand, bool>
{
    private readonly IWriteRepository<AkFactorConfigEntity> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfigEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(UpdateAkFactorConfigCommand request, CancellationToken cancellationToken)
    {
        var existAkFactorConfig = await _akFactorConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var processGroupExists = await _processGroupRepository.ExistsAsync(x => x.Id == request.UpdateModel.ProcessGroupId);
        if (!processGroupExists)
        {
            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
        }

        existAkFactorConfig.Update(
            request.UpdateModel.ProcessGroupId,
            request.UpdateModel.AkDiffDisplay,
            request.UpdateModel.AdjustmentRateDisplay,
            request.UpdateModel.Description);

        _akFactorConfigRepository.Update(existAkFactorConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
