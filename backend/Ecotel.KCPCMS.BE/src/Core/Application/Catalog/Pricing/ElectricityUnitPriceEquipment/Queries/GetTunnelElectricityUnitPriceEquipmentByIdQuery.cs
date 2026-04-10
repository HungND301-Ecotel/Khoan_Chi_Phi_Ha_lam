using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Domain.Common.Enums;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;

public record GetTunnelElectricityUnitPriceEquipmentByIdQuery(
    DefaultIdType Id,
    ElectricityUnitPriceType Type = ElectricityUnitPriceType.TunnelExcavation) : IRequest<ElectricityUnitPriceEquipmentDto>;

public class GetTunnelElectricityUnitPriceEquipmentByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTunnelElectricityUnitPriceEquipmentByIdQuery, ElectricityUnitPriceEquipmentDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();

    public async Task<ElectricityUnitPriceEquipmentDto> Handle(GetTunnelElectricityUnitPriceEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.Id && e.ElectricityType == request.Type,
            include: e => e.Include(e => e.Equipment).ThenInclude(e => e.UnitOfMeasure)
                .Include(e => e.Equipment).ThenInclude(e => e.Costs)
                .Include(e => e.Equipment).ThenInclude(e => e.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.ElectricityUnitPriceEquipmentNotFound);

        if (entity is not TunnelElectricityUnitPriceEquipment tunnelEntity)
        {
            throw new BadRequestException("The requested entity is not a Tunnel Electricity Unit Price Equipment.");
        }

        return new ElectricityUnitPriceEquipmentDto
        {
            Id = tunnelEntity.Id,
            EquipmentCode = tunnelEntity.Equipment?.Code?.Value ?? "",
            EquipmentId = tunnelEntity.EquipmentId,
            EquipmentName = tunnelEntity.Equipment?.Name ?? "",
            UnitOfMeasureName = tunnelEntity.Equipment?.UnitOfMeasure?.Name ?? "",
            EquipmentElectricityCost = tunnelEntity.GetCurrentElectricityCost(),
            ElectricityCostPerMetres = tunnelEntity.GetElectricityCostPerMetres(),
            ElectricityConsumePerMetres = tunnelEntity.GetElectricityConsumePerMetres(),
            StartMonth = tunnelEntity.StartMonth,
            EndMonth = tunnelEntity.EndMonth,
            Type = tunnelEntity.ElectricityType,
            MonthlyElectricityCost = tunnelEntity.MonthlyElectricityCost,
            AverageMonthlyTunnelProduction = tunnelEntity.AverageMonthlyTunnelProduction
        };
    }
}
