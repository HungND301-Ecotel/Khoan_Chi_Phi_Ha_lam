using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record CreateAdjustmentFactorCommand(CreateAdjustmentFactorDto CreateModel) : IRequest<bool>;

public class CreateAdjustmentFactorCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateAdjustmentFactorCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProcessGroup>();
    public async Task<bool> Handle(CreateAdjustmentFactorCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsAdjustmentFactorCodeExisted(request.CreateModel.Code, request.CreateModel.ProcessGroupId))
        {
            throw new ConflictException(CustomResponseMessage.AdjustmentFactorCodeAlreadyExists);
        }

        var processGroups = await _processGroupRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.CreateModel.ProcessGroupId,
            include: p => p.Include(p => p.Code)) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var adjustmentFactor = Domain.Entities.Index.AdjustmentFactor.Create(request.CreateModel.Code, request.CreateModel.Name, request.CreateModel.ProcessGroupId);
        await _adjustmentFactorRepository.InsertAsync(adjustmentFactor);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}

