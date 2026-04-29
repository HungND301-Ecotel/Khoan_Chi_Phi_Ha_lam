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
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    private readonly IWriteRepository<AssignmentCodeMaterial> _assignmentCodeMaterialRepository = unitOfWork.GetRepository<AssignmentCodeMaterial>();
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
            include: t => t.Include(t => t.Code)
                .Include(t => t.AssignmentCodeMaterials),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, existAssignmentCode.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.AssignmentCodeAlreadyExists);
        }

        var materialIds = request.UpdateModel.MaterialIds.Distinct().ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            existAssignmentCode.Update(request.UpdateModel.Name, request.UpdateModel.Code, request.UpdateModel.UnitOfMeasureId);

            var selectedMaterials = materialIds.Any()
                ? await _materialRepository.GetAllAsync(
                    predicate: m => materialIds.Contains(m.Id),
                    include: m => m.Include(x => x.Code),
                    disableTracking: false)
                : new List<Domain.Entities.Index.Material>();

            if (selectedMaterials.Count != materialIds.Count)
            {
                throw new NotFoundException(CustomResponseMessage.MaterialNotFound);
            }

            var currentLinkMap = existAssignmentCode.AssignmentCodeMaterials
                .ToDictionary(x => x.MaterialId);
            var selectedMaterialIdSet = selectedMaterials.Select(m => m.Id).ToHashSet();

            var linksToDelete = existAssignmentCode.AssignmentCodeMaterials
                .Where(link => !selectedMaterialIdSet.Contains(link.MaterialId))
                .ToList();
            if (linksToDelete.Any())
            {
                _assignmentCodeMaterialRepository.Delete(linksToDelete);
            }

            var linksToAdd = selectedMaterials
                .Where(material => !currentLinkMap.ContainsKey(material.Id))
                .Select(material => AssignmentCodeMaterial.Create(existAssignmentCode, material))
                .ToList();
            if (linksToAdd.Any())
            {
                await _assignmentCodeMaterialRepository.InsertAsync(linksToAdd, cancellationToken);
            }

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
