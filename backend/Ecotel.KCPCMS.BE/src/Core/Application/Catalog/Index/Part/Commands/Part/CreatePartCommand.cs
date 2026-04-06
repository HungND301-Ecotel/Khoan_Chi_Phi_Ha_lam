using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Commands.Part;

public record CreatePartCommand(CreatePartDto CreateModel) : IRequest<bool>;

public class CreatePartCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreatePartCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<PartProcessGroup> _partProcessGroupRepository = unitOfWork.GetRepository<PartProcessGroup>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(CreatePartCommand request, CancellationToken cancellationToken)
    {
        var equipmentIds = request.CreateModel.EquipmentIds.Distinct().ToList();
        if (!equipmentIds.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        if (await codeService.IsPartCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
        }

        var equipments = await _equipmentRepository.GetAllAsync(
            predicate: x => equipmentIds.Contains(x.Id),
            disableTracking: false);
        if (equipments.Count != equipmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        var processGroupIds = (request.CreateModel.ProcessGroupIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        await EnsureProcessGroupsExist(processGroupIds);

        if (request.CreateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.CreateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasureExisted)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var newPart = Domain.Entities.Index.Part.Create(
                request.CreateModel.Code,
                request.CreateModel.Name,
                request.CreateModel.UnitOfMeasureId,
                request.CreateModel.ReplacementTimeStandard,
                equipments.ToList());

            var costList = new List<Cost>();
            foreach (var cost in request.CreateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: cost.CostType,
                    amount: cost.Amount,
                    costTypeId: newPart.Id,
                    actualAmount: cost.ActualAmount));
            }

            if (costList.HasOverlap())
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            newPart.AddCost(costList);

            await _partRepository.InsertAsync(newPart);
            if (processGroupIds.Any())
            {
                var partProcessGroups = processGroupIds
                    .Select(processGroupId => PartProcessGroup.Create(newPart.Id, processGroupId))
                    .ToList();
                await _partProcessGroupRepository.InsertAsync(partProcessGroups, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync();
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task EnsureProcessGroupsExist(ICollection<Guid> processGroupIds)
    {
        if (!processGroupIds.Any())
        {
            return;
        }

        var existingProcessGroupIds = await _processGroupRepository.GetAllAsync(
            selector: x => x.Id,
            predicate: x => processGroupIds.Contains(x.Id),
            disableTracking: true);

        var existingIdSet = existingProcessGroupIds.ToHashSet();
        if (processGroupIds.Any(id => !existingIdSet.Contains(id)))
        {
            throw new NotFoundException("Nhóm công đoạn sản xuất không tồn tại.");
        }
    }
}
