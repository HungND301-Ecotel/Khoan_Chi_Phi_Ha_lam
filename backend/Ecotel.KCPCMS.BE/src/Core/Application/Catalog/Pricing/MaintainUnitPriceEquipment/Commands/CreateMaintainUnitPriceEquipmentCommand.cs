using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaintainUnitPrice;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Commands;

public record CreateMaintainUnitPriceEquipmentCommand(IList<CreateMaintainUnitPriceEquipmentDto> CreateModel) : IRequest<bool>;

public class CreateMaintainUnitPriceEquipmentCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<CreateMaintainUnitPriceEquipmentCommand, bool>
{
    private readonly IWriteRepository<MaintainUnitPrice> _maintainUnitPricRepository = unitOfWork.GetRepository<MaintainUnitPrice>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();

    public async Task<bool> Handle(CreateMaintainUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        if (request.CreateModel is null || !request.CreateModel.Any())
        {
            throw new BadRequestException(CustomResponseMessage.NoEquipmentCostsProvided);
        }

        var equipmentIds = request.CreateModel.Select(c => c.EquipmentId);
        var equipmentDetails = await _equipmentRepository.GetAllAsync(
            predicate: e => equipmentIds.Contains(e.Id),
            include: e => e.Include(e => e.EquipmentParts).ThenInclude(ep => ep.Part).Include(e => e.Costs),
            disableTracking: true);

        if (equipmentDetails.Count != equipmentIds.Count())
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        var equipmentDict = equipmentDetails.ToDictionary(e => e.Id, e => e);

        var resultEntities = new List<MaintainUnitPrice>();
        foreach (var model in request.CreateModel)
        {
            if (!equipmentDict.TryGetValue(model.EquipmentId, out var equipment))
            {
                throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
            }

            var partIds = equipment.EquipmentParts.Select(p => p.PartId).ToHashSet();
            var inputPartIds = model.Costs.Select(c => c.PartId).ToHashSet();

            if (!partIds.IsSupersetOf(inputPartIds))
            {
                throw new ConflictException(CustomResponseMessage.EquipmentPartsInvalid);
            }

            var maintainUnitPrice = MaintainUnitPrice.Create(
                equipment.Id,
                model.StartMonth,
                model.EndMonth,
                null,
                model.OtherMaterialValue,
                model.Type
            );

            foreach (var cost in model.Costs)
            {
                var part = equipment.EquipmentParts.Select(c => c.Part).FirstOrDefault(c => c.Id == cost.PartId);
                maintainUnitPrice.AddMaintainUnitPriceEquipment(Domain.Entities.Pricing.MaintainUnitPriceEquipment.Create(
                    null,
                    part.Id,
                    cost?.Quantity ?? 0,
                    cost?.AverageMonthlyTunnelProduction ?? 0,
                    cost?.ReplacementTimeStandard ?? 0
                ));
            }

            resultEntities.Add(maintainUnitPrice);
        }

        await _maintainUnitPricRepository.InsertAsync(resultEntities);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}

