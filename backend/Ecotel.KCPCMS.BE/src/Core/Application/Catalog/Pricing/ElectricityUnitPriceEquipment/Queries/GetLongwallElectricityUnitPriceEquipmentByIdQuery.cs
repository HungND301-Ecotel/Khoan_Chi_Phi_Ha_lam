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

public record GetLongwallElectricityUnitPriceEquipmentByIdQuery(DefaultIdType Id) : IRequest<ElectricityUnitPriceEquipmentDto>;

public class GetLongwallElectricityUnitPriceEquipmentByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLongwallElectricityUnitPriceEquipmentByIdQuery, ElectricityUnitPriceEquipmentDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();

    public async Task<ElectricityUnitPriceEquipmentDto> Handle(GetLongwallElectricityUnitPriceEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.Id && e.ElectricityType == ElectricityUnitPriceType.Longwall,
            include: e => e.Include(e => e.Equipment).ThenInclude(e => e.UnitOfMeasure)
                .Include(e => e.Equipment).ThenInclude(e => e.Costs)
                .Include(e => e.Equipment).ThenInclude(e => e.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.ElectricityUnitPriceEquipmentNotFound);

        if (entity is not LongwallElectricityUnitPriceEquipment longwallEntity)
        {
            throw new BadRequestException("The requested entity is not a Longwall Electricity Unit Price Equipment.");
        }

        return new ElectricityUnitPriceEquipmentDto
        {
            Id = longwallEntity.Id,
            EquipmentCode = longwallEntity.Equipment?.Code?.Value ?? "",
            EquipmentId = longwallEntity.EquipmentId,
            EquipmentName = longwallEntity.Equipment?.Name ?? "",
            UnitOfMeasureName = longwallEntity.Equipment?.UnitOfMeasure?.Name ?? "",
            EquipmentElectricityCost = longwallEntity.GetCurrentElectricityCost(),
            ElectricityCostPerMetres = longwallEntity.GetElectricityCostPerMetres(),
            ElectricityConsumePerMetres = longwallEntity.GetElectricityConsumePerMetres(),
            StartMonth = longwallEntity.StartMonth,
            EndMonth = longwallEntity.EndMonth,
            Type = longwallEntity.ElectricityType,
            Quantity = longwallEntity.Quantity,
            Pdm = longwallEntity.Pdm,
            Kyc = longwallEntity.Kyc,
            Kdt = longwallEntity.Kdt,
            WorkingHour = longwallEntity.WorkingHour,
            WorkingDate = longwallEntity.WorkingDate,
            LongwallAverageMonthlyTunnelProduction = longwallEntity.AverageMonthlyTunnelProduction,
            // Calculated properties
            SPdm = longwallEntity.SPdm,
            Ptt = longwallEntity.Ptt
        };
    }
}
