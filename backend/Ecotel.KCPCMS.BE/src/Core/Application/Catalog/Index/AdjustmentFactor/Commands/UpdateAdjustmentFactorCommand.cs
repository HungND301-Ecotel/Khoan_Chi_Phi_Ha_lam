using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record UpdateAdjustmentFactorCommand(UpdateAdjustmentFactorDto UpdateModel) : IRequest<bool>;

public class UpdateAdjustmentFactorCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAdjustmentFactorCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjusmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProcessGroup>();
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(UpdateAdjustmentFactorCommand request, CancellationToken cancellationToken)
    {
        var existAdjustmentFactor = await _adjusmentFactorRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var fixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.FixedKeyId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await _adjusmentFactorRepository.GetFirstOrDefaultAsync(
                predicate: x => x.FixedKeyId == request.UpdateModel.FixedKeyId &&
                                x.ProcessGroupId == request.UpdateModel.ProcessGroupId &&
                                x.Id != request.UpdateModel.Id,
                disableTracking: true) != null)
        {
            throw new ConflictException(CustomResponseMessage.AdjustmentFactorCodeAlreadyExists);
        }

        _ = await _processGroupRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.UpdateModel.ProcessGroupId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);

        existAdjustmentFactor.Update(fixedKey, request.UpdateModel.Name, request.UpdateModel.ProcessGroupId);

        _adjusmentFactorRepository.Update(existAdjustmentFactor);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
