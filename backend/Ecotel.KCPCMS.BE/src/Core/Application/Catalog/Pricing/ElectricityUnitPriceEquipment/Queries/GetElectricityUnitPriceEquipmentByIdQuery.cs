using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;

public record GetElectricityUnitPriceEquipmentByIdQuery(DefaultIdType Id) : IRequest<ElectricityUnitPriceEquipmentDto>;

public class GetElectricityUnitPriceEquipmentByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetElectricityUnitPriceEquipmentByIdQuery, ElectricityUnitPriceEquipmentDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _electricityUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();
    public async Task<ElectricityUnitPriceEquipmentDto> Handle(GetElectricityUnitPriceEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var electricityUnitPriceEquipment =
            await _electricityUnitPriceRepository.GetFirstOrDefaultAsync(
                predicate: e => e.Id == request.Id,
                include: e => e.Include(e => e.Equipment).ThenInclude(e => e.UnitOfMeasure)
                    .Include(e => e.Equipment).ThenInclude(e => e.Costs)
                    .Include(e => e.Equipment).ThenInclude(e => e.Code),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.ElectricityUnitPriceEquipmentNotFound);

        return new ElectricityUnitPriceEquipmentDto
        {
            Id = electricityUnitPriceEquipment.Id,
            EquipmentCode = electricityUnitPriceEquipment.Equipment?.Code?.Value ?? "",
            EquipmentId = electricityUnitPriceEquipment!.EquipmentId,
            EquipmentName = electricityUnitPriceEquipment.Equipment?.Name ?? "",
            UnitOfMeasureName = electricityUnitPriceEquipment.Equipment?.UnitOfMeasure?.Name ?? "",
            EquipmentElectricityCost = electricityUnitPriceEquipment.GetCurrentElectricityCost(),
            ElectricityCostPerMetres = electricityUnitPriceEquipment.GetElectricityCostPerMetres(),
            ElectricityConsumePerMetres = electricityUnitPriceEquipment.GetElectricityConsumePerMetres(),
            StartMonth = electricityUnitPriceEquipment.StartMonth,
            EndMonth = electricityUnitPriceEquipment.EndMonth,
            Type = electricityUnitPriceEquipment.ElectricityType,
            // Tunnel properties
            MonthlyElectricityCost = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.TunnelElectricityUnitPriceEquipment tunnel ? tunnel.MonthlyElectricityCost : null,
            AverageMonthlyTunnelProduction = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.TunnelElectricityUnitPriceEquipment tunnel2 ? tunnel2.AverageMonthlyTunnelProduction : null,
            // Longwall properties
            Quantity = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment longwall ? longwall.Quantity : null,
            Pdm = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment longwall2 ? longwall2.Pdm : null,
            Kyc = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment longwall3 ? longwall3.Kyc : null,
            Kdt = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment longwall4 ? longwall4.Kdt : null,
            WorkingHour = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment longwall5 ? longwall5.WorkingHour : null,
            WorkingDate = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment longwall6 ? longwall6.WorkingDate : null,
            LongwallAverageMonthlyTunnelProduction = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment longwall7 ? longwall7.AverageMonthlyTunnelProduction : null,
            // Longwall calculated properties
            SPdm = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment lwallSPdm ? lwallSPdm.SPdm : null,
            Ptt = electricityUnitPriceEquipment is Domain.Entities.Pricing.EletricityUnitPrice.LongwallElectricityUnitPriceEquipment lwallPtt ? lwallPtt.Ptt : null
        };
    }
}
