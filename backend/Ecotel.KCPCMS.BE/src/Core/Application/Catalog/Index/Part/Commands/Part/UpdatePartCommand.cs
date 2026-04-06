using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Commands.Part;

public record UpdatePartCommand(UpdatePartDto UpdateModel) : IRequest<bool>;

public class UpdatePartCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdatePartCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<PartProcessGroup> _partProcessGroupRepository = unitOfWork.GetRepository<PartProcessGroup>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    public async Task<bool> Handle(UpdatePartCommand request, CancellationToken cancellationToken)
    {
        var equipmentIds = request.UpdateModel.EquipmentIds.Distinct().ToList();
        if (!equipmentIds.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        var equipments = await _equipmentRepository.GetAllAsync(
            predicate: x => equipmentIds.Contains(x.Id),
            disableTracking: false);
        if (equipments.Count != equipmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

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
        var existedPart = await _partRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.UpdateModel.Id,
            include: m => m.Include(c => c.Costs).Include(c => c.Code).Include(c => c.EquipmentParts).Include(c => c.PartProcessGroups),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsPartCodeExisted(request.UpdateModel.Code, existedPart.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _costRepository.Delete(existedPart.Costs.ToList());

            var existedPartProcessGroups = await _partProcessGroupRepository.GetAllAsync(
                predicate: x => x.PartId == existedPart.Id,
                disableTracking: false);
            if (existedPartProcessGroups.Any())
            {
                _partProcessGroupRepository.Delete(existedPartProcessGroups);
            }

            await unitOfWork.SaveChangesAsync();

            existedPart.Update(
                request.UpdateModel.Code,
                request.UpdateModel.Name,
                request.UpdateModel.UnitOfMeasureId,
                request.UpdateModel.ReplacementTimeStandard,
                equipments.ToList());

            var costList = new List<Cost>();
            foreach (var cost in request.UpdateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: cost.CostType,
                    amount: cost.Amount,
                    costTypeId: existedPart.Id,
                    actualAmount: cost.ActualAmount));
            }

            if (costList.HasOverlap())
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            existedPart.AddCost(costList);

            if (processGroupIds.Any())
            {
                var partProcessGroups = processGroupIds
                    .Select(processGroupId => PartProcessGroup.Create(existedPart.Id, processGroupId))
                    .ToList();
                await _partProcessGroupRepository.InsertAsync(partProcessGroups, cancellationToken);
            }

            _partRepository.Update(existedPart);

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
