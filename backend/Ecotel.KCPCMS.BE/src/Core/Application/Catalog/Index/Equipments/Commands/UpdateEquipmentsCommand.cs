using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Common.Enums;
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
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<EquipmentPart> _equipmentPartRepository = unitOfWork.GetRepository<EquipmentPart>();
    private readonly IWriteRepository<EquipmentProcessGroup> _equipmentProcessGroupRepository = unitOfWork.GetRepository<EquipmentProcessGroup>();
    public async Task<bool> Handle(UpdateEquipmentsCommand request, CancellationToken cancellationToken)
    {
        var processGroupId = request.UpdateModel.ProcessGroupId;
        if (!processGroupId.HasValue || processGroupId.Value == Guid.Empty)
        {
            throw new BadRequestException("Nhóm công đoạn sản xuất không được để trống.");
        }

        var partIds = (request.UpdateModel.PartIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        await EnsureProcessGroupExist(processGroupId.Value);
        await EnsurePartsExist(partIds);

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

        if (await codeService.IsEquipmentCodeExisted(
                request.UpdateModel.Code,
                processGroupId.Value,
                existedEquipment.Id))
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

            var existedEquipmentParts = await _equipmentPartRepository.GetAllAsync(
                predicate: x => x.EquipmentId == existedEquipment.Id,
                disableTracking: false);
            if (existedEquipmentParts.Any())
            {
                _equipmentPartRepository.Delete(existedEquipmentParts);
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

            var equipmentProcessGroup = EquipmentProcessGroup.Create(existedEquipment.Id, processGroupId.Value);
            await _equipmentProcessGroupRepository.InsertAsync(equipmentProcessGroup, cancellationToken);

            if (partIds.Any())
            {
                var equipmentParts = partIds
                    .Select(partId => EquipmentPart.Create(existedEquipment.Id, partId))
                    .ToList();
                await _equipmentPartRepository.InsertAsync(equipmentParts, cancellationToken);
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

    private async Task EnsureProcessGroupExist(Guid processGroupId)
    {
        var exists = await _processGroupRepository.ExistsAsync(x => x.Id == processGroupId);
        if (!exists)
        {
            throw new NotFoundException("Nhóm công đoạn sản xuất không tồn tại.");
        }
    }

    private async Task EnsurePartsExist(ICollection<Guid> partIds)
    {
        if (!partIds.Any())
        {
            return;
        }

        var existingPartIds = await _partRepository.GetAllAsync(
            selector: x => x.Id,
            predicate: x => partIds.Contains(x.Id) &&
                            (x.Type == PartType.Part || x.Type == PartType.OtherPart),
            disableTracking: true);

        var existingIdSet = existingPartIds.ToHashSet();
        if (partIds.Any(id => !existingIdSet.Contains(id)))
        {
            throw new NotFoundException(CustomResponseMessage.PartNotFound);
        }
    }
}
