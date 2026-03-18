using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record UpdateAdjustmentFactorCommand(UpdateAdjustmentFactorDto UpdateModel) : IRequest<bool>;

public class UpdateAdjustmentFactorCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdateAdjustmentFactorCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjusmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProcessGroup>();
    public async Task<bool> Handle(UpdateAdjustmentFactorCommand request, CancellationToken cancellationToken)
    {
        var existAdjustmentFactor = await _adjusmentFactorRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            include: p => p.Include(p => p.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsAdjustmentFactorCodeExisted(request.UpdateModel.Code, existAdjustmentFactor.CodeId, request.UpdateModel.ProcessGroupId))
        {
            throw new ConflictException(CustomResponseMessage.AdjustmentFactorCodeAlreadyExists);
        }

        var processGroups = await _processGroupRepository.GetFirstOrDefaultAsync(predicate: p => p.Id == request.UpdateModel.ProcessGroupId) ?? throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);

        existAdjustmentFactor.Update(request.UpdateModel.Code, request.UpdateModel.Name, request.UpdateModel.ProcessGroupId);

        _adjusmentFactorRepository.Update(existAdjustmentFactor);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
