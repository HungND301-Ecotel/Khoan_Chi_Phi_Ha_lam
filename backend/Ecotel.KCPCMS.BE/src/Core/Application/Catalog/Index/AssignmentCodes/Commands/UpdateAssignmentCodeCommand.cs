using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AssignmentCodes.Commands;

public record UpdateAssignmentCodeCommand(UpdateAssignmentCodeDto UpdateModel) : IRequest<bool>;

public class UpdateAssignmentCodeCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdateAssignmentCodeCommand, bool>
{
    private readonly IWriteRepository<AssignmentCode> _assignemntcodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(UpdateAssignmentCodeCommand request, CancellationToken cancellationToken)
    {
        if (request.UpdateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasure =
                await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.UpdateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasure)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }

        var existAssignmentCode = await _assignemntcodeRepository.GetFirstOrDefaultAsync(
        predicate: t => t.Id == request.UpdateModel.Id,
        include: t => t.Include(t => t.Code),
        disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, existAssignmentCode.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.AssignmentCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            existAssignmentCode.Update(request.UpdateModel.Name, request.UpdateModel.Code, request.UpdateModel.UnitOfMeasureId);

            _assignemntcodeRepository.Update(existAssignmentCode);
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