using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Extensions;
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
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
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
            include: t => t.Include(t => t.Code).Include(t => t.Costs),
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
            _costRepository.Delete(existAssignmentCode.Costs.ToList());

            var costList = request.UpdateModel.Costs
                .Select(cost => Cost.CreateAssignmentCodeCost(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: CostType.Electricity,
                    amount: cost.Amount,
                    assignmentCodeId: existAssignmentCode.Id))
                .ToList();

            if (costList.HasOverlap())
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            existAssignmentCode.AddCost(costList);

            var currentLinks = await _assignmentCodeMaterialRepository.GetAllAsync(
                predicate: m => m.AssignmentCodeId == existAssignmentCode.Id,
                disableTracking: false);

            var selectedMaterials = materialIds.Any()
                ? await _materialRepository.GetAllAsync(
                    predicate: m => materialIds.Contains(m.Id),
                    include: q => q.Include(m => m.Code),
                    disableTracking: false)
                : new List<Domain.Entities.Index.Material>();

            if (selectedMaterials.Count != materialIds.Count)
            {
                throw new NotFoundException(CustomResponseMessage.MaterialNotFound);
            }

            var selectedMaterialIdSet = selectedMaterials
                .Select(m => m.Id)
                .ToHashSet();

            var currentMaterialIdSet = currentLinks.Select(link => link.MaterialId).ToHashSet();

            var linksToDelete = currentLinks
                .Where(link => !selectedMaterialIdSet.Contains(link.MaterialId))
                .ToList();
            if (linksToDelete.Any())
            {
                _assignmentCodeMaterialRepository.Delete(linksToDelete);
            }

            foreach (var material in selectedMaterials.Where(m => !currentMaterialIdSet.Contains(m.Id)))
            {
                await _assignmentCodeMaterialRepository.InsertAsync(
                    AssignmentCodeMaterial.Create(existAssignmentCode.Id, material.Id),
                    cancellationToken);
            }

            foreach (var material in selectedMaterials.Where(m => m.AssigmentCodeId == null))
            {
                material.Update(
                    material.Code?.Value ?? string.Empty,
                    material.Name,
                    material.UnitOfMeasureId,
                    existAssignmentCode.Id,
                    material.MaterialType);
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
