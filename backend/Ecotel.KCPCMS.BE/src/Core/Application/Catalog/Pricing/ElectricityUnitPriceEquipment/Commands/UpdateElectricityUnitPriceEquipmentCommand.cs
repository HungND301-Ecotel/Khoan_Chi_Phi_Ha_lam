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

public record UpdateElectricityUnitPriceEquipmentCommand(UpdateElectricityUnitPriceEquipmentDto UpdateModel) : IRequest<bool>;

public class UpdateElectricityUnitPriceEquipmentCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<UpdateElectricityUnitPriceEquipmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _electricityUnitPriceEquipmentRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();

    private const string CacheSignalKey = "ProductUnitPrice";

    public async Task<bool> Handle(UpdateElectricityUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        var existEntity = await _electricityUnitPriceEquipmentRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var equipment = await _equipmentRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.UpdateModel.EquipmentId,
            include: e => e.Include(e => e.Costs),
            disableTracking: true
            ) ?? throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);

        // Type check - only allow updating TunnelElectricityUnitPriceEquipment with this command
        if (existEntity is not TunnelElectricityUnitPriceEquipment tunnelEntity)
        {
            throw new BadRequestException("This command can only update Tunnel Electricity Unit Price Equipment.");
        }

        tunnelEntity.Update(
            equipmentId: equipment.Id,
            monthlyElectricityCost: request.UpdateModel.MonthlyElectricityCost,
            averageMonthlyTunnelProduction: request.UpdateModel.AverageMonthlyTunnelProduction,
            startMonth: request.UpdateModel.StartMonth,
            endMonth: request.UpdateModel.EndMonth);

        _electricityUnitPriceEquipmentRepository.Update(tunnelEntity);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
