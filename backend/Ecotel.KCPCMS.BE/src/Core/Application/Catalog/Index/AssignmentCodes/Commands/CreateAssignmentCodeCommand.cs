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

public record CreateAssignmentCodeCommand(CreateAssignmentCodeDto CreateModel) : IRequest<bool>;

public class CreateAssignmentCodeCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateAssignmentCodeCommand, bool>
{
    private readonly IWriteRepository<AssignmentCode> _assignemntcodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    private readonly IWriteRepository<AssignmentCodeMaterial> _assignmentCodeMaterialRepository = unitOfWork.GetRepository<AssignmentCodeMaterial>();
    public async Task<bool> Handle(CreateAssignmentCodeCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.AssignmentCodeAlreadyExists);
        }

        var materialIds = request.CreateModel.MaterialIds.Distinct().ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var newAssignmentCode = AssignmentCode.Create(request.CreateModel.Name, request.CreateModel.Code, request.CreateModel.UnitOfMeasureId);
            var costList = request.CreateModel.Costs
                .Select(cost => Cost.CreateAssignmentCodeCost(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: CostType.Electricity,
                    amount: cost.Amount,
                    assignmentCodeId: newAssignmentCode.Id))
                .ToList();

            if (costList.HasOverlap())
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            newAssignmentCode.AddCost(costList);
            await _assignemntcodeRepository.InsertAsync(newAssignmentCode, cancellationToken);
            await unitOfWork.SaveChangesAsync();

            if (materialIds.Any())
            {
                var materials = await _materialRepository.GetAllAsync(
                    predicate: m => materialIds.Contains(m.Id),
                    include: q => q.Include(m => m.Code),
                    disableTracking: false);

                if (materials.Count != materialIds.Count)
                {
                    throw new NotFoundException(CustomResponseMessage.MaterialNotFound);
                }

                await _assignmentCodeMaterialRepository.InsertAsync(
                    materials.Select(material => AssignmentCodeMaterial.Create(newAssignmentCode.Id, material.Id)).ToList(),
                    cancellationToken);

                foreach (var material in materials.Where(m => m.AssigmentCodeId == null))
                {
                    material.Update(
                        material.Code?.Value ?? string.Empty,
                        material.Name,
                        material.UnitOfMeasureId,
                        newAssignmentCode.Id,
                        material.MaterialType);
                }
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
