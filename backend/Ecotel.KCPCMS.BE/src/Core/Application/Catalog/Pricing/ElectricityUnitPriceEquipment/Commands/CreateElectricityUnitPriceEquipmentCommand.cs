using Application.Common.Caching;
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
    IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<CreateElectricityUnitPriceEquipmentCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "ElectricityUnitPriceEquipment";
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _electricityUnitPriceEquipmentRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<AssignmentCode> _equipmentRepository = unitOfWork.GetRepository<AssignmentCode>();
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
                    e.ElectricityType == model.Type &&
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

            resultEntities.Add(model.Type == Domain.Common.Enums.ElectricityUnitPriceType.Trimming
                ? TrimmingElectricityUnitPriceEquipment.Create(
                    equipmentId: equipment.Id,
                    monthlyElectricityCost: model.MonthlyElectricityCost,
                    averageMonthlyTunnelProduction: model.AverageMonthlyTunnelProduction,
                    startMonth: model.StartMonth,
                    endMonth: model.EndMonth)
                : TunnelElectricityUnitPriceEquipment.Create(
                    equipmentId: equipment.Id,
                    monthlyElectricityCost: model.MonthlyElectricityCost,
                    averageMonthlyTunnelProduction: model.AverageMonthlyTunnelProduction,
                    startMonth: model.StartMonth,
                    endMonth: model.EndMonth,
                    electricityType: model.Type));
        }

        await _electricityUnitPriceEquipmentRepository.InsertAsync(resultEntities, cancellationToken);
        await unitOfWork.SaveChangesAsync();
        cacheService.InvalidateGroup(CacheSignalKey);
        cacheService.InvalidateGroup(ModuleCacheSignalKey);
        return true;
    }
}
