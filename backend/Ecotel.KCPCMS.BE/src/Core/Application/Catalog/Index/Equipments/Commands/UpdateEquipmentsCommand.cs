using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Commands;
public record UpdateEquipmentsCommand(UpdateEquipmentDto UpdateModel) : IRequest<bool>;

public class UpdateEquipmentsCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService, ICostService costService) : IRequestHandler<UpdateEquipmentsCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<EquipmentProcessGroup> _equipmentProcessGroupRepository = unitOfWork.GetRepository<EquipmentProcessGroup>();
    public async Task<bool> Handle(UpdateEquipmentsCommand request, CancellationToken cancellationToken)
    {
        var processGroupIds = (request.UpdateModel.ProcessGroupIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        await EnsureProcessGroupsExist(processGroupIds);

        if (request.UpdateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.UpdateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasureExisted)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }

        var existedEquipment = await _equipmentRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.UpdateModel.Id,
            include: m => m.Include(c => c.Costs).Include(c => c.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, existedEquipment.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.EquipmentCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            _costRepository.Delete(existedEquipment.Costs.ToList());

            var existedEquipmentProcessGroups = await _equipmentProcessGroupRepository.GetAllAsync(
                predicate: x => x.EquipmentId == existedEquipment.Id,
                disableTracking: false);
            if (existedEquipmentProcessGroups.Any())
            {
                _equipmentProcessGroupRepository.Delete(existedEquipmentProcessGroups);
            }

            await unitOfWork.SaveChangesAsync();

            existedEquipment.Update(request.UpdateModel.Code, request.UpdateModel.Name, request.UpdateModel.UnitOfMeasureId);

            var costList = new List<Cost>();
            foreach (var cost in request.UpdateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: cost.CostType,
                    amount: cost.Amount,
                    costTypeId: existedEquipment.Id));
            }

            if (await costService.IsOverlap(costList))
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            existedEquipment.AddCost(costList);

            _equipmentRepository.Update(existedEquipment);

            if (processGroupIds.Any())
            {
                var equipmentProcessGroups = processGroupIds
                    .Select(processGroupId => EquipmentProcessGroup.Create(existedEquipment.Id, processGroupId))
                    .ToList();
                await _equipmentProcessGroupRepository.InsertAsync(equipmentProcessGroups, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync();
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
