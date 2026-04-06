using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Commands;

public record CreateEquipmentCommand(CreateEquipmentDto CreateModel) : IRequest<bool>;

public class CreateEquipmentCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService, ICostService costService) : IRequestHandler<CreateEquipmentCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<EquipmentProcessGroup> _equipmentProcessGroupRepository = unitOfWork.GetRepository<EquipmentProcessGroup>();
    public async Task<bool> Handle(CreateEquipmentCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.EquipmentCodeAlreadyExists);
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

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var newEquipment = Equipment.Create(request.CreateModel.Code, request.CreateModel.Name, request.CreateModel.UnitOfMeasureId);

            var costList = new List<Cost>();
            foreach (var cost in request.CreateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: cost.CostType,
                    amount: cost.Amount,
                    costTypeId: newEquipment.Id));
            }

            if (await costService.IsOverlap(costList) && costList.Any())
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            newEquipment.AddCost(costList);

            await _equipmentRepository.InsertAsync(newEquipment, cancellationToken);

            if (processGroupIds.Any())
            {
                var equipmentProcessGroups = processGroupIds
                    .Select(processGroupId => EquipmentProcessGroup.Create(newEquipment.Id, processGroupId))
                    .ToList();
                await _equipmentProcessGroupRepository.InsertAsync(equipmentProcessGroups, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
        return true;
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
