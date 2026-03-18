using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Domain.Entities.Index;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Commands;

public record UpdateLongwallElectricityUnitPriceEquipmentCommand(UpdateLongwallElectricityUnitPriceEquipmentDto UpdateModel) : IRequest<bool>;

public class UpdateLongwallElectricityUnitPriceEquipmentCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<UpdateLongwallElectricityUnitPriceEquipmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _electricityUnitPriceEquipmentRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();

    private const string CacheSignalKey = "ProductUnitPrice";

    public async Task<bool> Handle(UpdateLongwallElectricityUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        var existEntity = await _electricityUnitPriceEquipmentRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var equipment = await _equipmentRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.UpdateModel.EquipmentId,
            include: e => e.Include(e => e.Costs),
            disableTracking: true
            ) ?? throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);

        // Type check - only allow updating LongwallElectricityUnitPriceEquipment with this command
        if (existEntity is not LongwallElectricityUnitPriceEquipment longwallEntity)
        {
            throw new BadRequestException("This command can only update Longwall Electricity Unit Price Equipment.");
        }

        longwallEntity.Update(
            equipmentId: equipment.Id,
            startMonth: request.UpdateModel.StartMonth,
            endMonth: request.UpdateModel.EndMonth,
            quantity: request.UpdateModel.Quantity,
            pdm: request.UpdateModel.Pdm,
            kyc: request.UpdateModel.Kyc,
            kdt: request.UpdateModel.Kdt,
            workingHour: request.UpdateModel.WorkingHour,
            workingDate: request.UpdateModel.WorkingDate,
            averageMonthlyTunnelProduction: request.UpdateModel.AverageMonthlyTunnelProduction);

        _electricityUnitPriceEquipmentRepository.Update(longwallEntity);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
