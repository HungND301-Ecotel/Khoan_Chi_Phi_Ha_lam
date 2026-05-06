using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Commands;

public record CreateEquipmentCommand(CreateEquipmentDto CreateModel) : IRequest<bool>;

public class CreateEquipmentCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService, ICostService costService) : IRequestHandler<CreateEquipmentCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<EquipmentPart> _equipmentPartRepository = unitOfWork.GetRepository<EquipmentPart>();
    public async Task<bool> Handle(CreateEquipmentCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsEquipmentCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.EquipmentCodeAlreadyExists);
        }

        var partIds = (request.CreateModel.PartIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        await EnsurePartsExist(partIds);

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

            if (partIds.Any())
            {
                var equipmentParts = partIds
                    .Select(partId => EquipmentPart.Create(newEquipment.Id, partId))
                    .ToList();
                await _equipmentPartRepository.InsertAsync(equipmentParts, cancellationToken);
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
