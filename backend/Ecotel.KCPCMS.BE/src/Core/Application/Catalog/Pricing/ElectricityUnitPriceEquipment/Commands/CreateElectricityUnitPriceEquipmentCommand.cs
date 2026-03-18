using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Domain.Entities.Index;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Commands;

public record CreateElectricityUnitPriceEquipmentCommand(IList<CreateElectricityUnitPriceEquipmentDto> CreateModel) : IRequest<bool>;

public class CreateElectricityUnitPriceEquipmentCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<CreateElectricityUnitPriceEquipmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _electricityUnitPriceEquipmentRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    public async Task<bool> Handle(CreateElectricityUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        if (request.CreateModel is null || !request.CreateModel.Any())
        {
            throw new BadRequestException(CustomResponseMessage.NoEquipmentCostsProvided);
        }

        var equipmentIds = request.CreateModel.Select(c => c.EquipmentId).Distinct().ToList();

        // Check for existing overlapping records
        foreach (var model in request.CreateModel)
        {
            var existed = await _electricityUnitPriceEquipmentRepository.GetFirstOrDefaultAsync(
                predicate: e =>
                    e.EquipmentId == model.EquipmentId &&
                    e.StartMonth < model.EndMonth &&
                    e.EndMonth > model.StartMonth,
                disableTracking: true);

            if (existed != null)
            {
                throw new ConflictException(CustomResponseMessage.ElectricityUnitPriceEquipmentAlreadyExists);
            }
        }

        // Validate all equipments exist
        var equipments = await _equipmentRepository.GetAllAsync(
            predicate: e => equipmentIds.Contains(e.Id),
            disableTracking: true);

        if (equipments.Count != equipmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        var equipmentDict = equipments.ToDictionary(e => e.Id, e => e);

        var resultEntities = new List<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();
        foreach (var model in request.CreateModel)
        {
            if (!equipmentDict.TryGetValue(model.EquipmentId, out var equipment))
            {
                throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
            }

            resultEntities.Add(TunnelElectricityUnitPriceEquipment.Create(
                equipmentId: equipment.Id,
                monthlyElectricityCost: model.MonthlyElectricityCost,
                averageMonthlyTunnelProduction: model.AverageMonthlyTunnelProduction,
                startMonth: model.StartMonth,
                endMonth: model.EndMonth));
        }

        await _electricityUnitPriceEquipmentRepository.InsertAsync(resultEntities, cancellationToken);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
