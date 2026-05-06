using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record CreateAdjustmentFactorCommand(CreateAdjustmentFactorDto CreateModel) : IRequest<bool>;

public class CreateAdjustmentFactorCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateAdjustmentFactorCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProcessGroup>();
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(CreateAdjustmentFactorCommand request, CancellationToken cancellationToken)
    {
        var fixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.CreateModel.FixedKeyId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await _adjustmentFactorRepository.GetFirstOrDefaultAsync(
                predicate: x => x.FixedKeyId == request.CreateModel.FixedKeyId && x.ProcessGroupId == request.CreateModel.ProcessGroupId,
                disableTracking: true) != null)
        {
            throw new ConflictException(CustomResponseMessage.AdjustmentFactorCodeAlreadyExists);
        }

        _ = await _processGroupRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.CreateModel.ProcessGroupId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var adjustmentFactor = Domain.Entities.Index.AdjustmentFactor.Create(fixedKey, request.CreateModel.Name, request.CreateModel.ProcessGroupId);
        await _adjustmentFactorRepository.InsertAsync(adjustmentFactor);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}

