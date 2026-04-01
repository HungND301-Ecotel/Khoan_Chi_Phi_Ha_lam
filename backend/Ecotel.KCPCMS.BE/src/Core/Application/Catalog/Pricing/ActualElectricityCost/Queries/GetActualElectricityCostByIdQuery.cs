using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ActualElectricityCost;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ActualElectricityCost.Queries;

public record GetActualElectricityCostByIdQuery(DefaultIdType Id) : IRequest<ActualElectricityCostDetailDto>;

public class GetActualElectricityCostByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetActualElectricityCostByIdQuery, ActualElectricityCostDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ActualElectricityCost> _actualElectricityCostRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.ActualElectricityCost>();

    public async Task<ActualElectricityCostDetailDto> Handle(GetActualElectricityCostByIdQuery request, CancellationToken cancellationToken)
    {
        var model = await _actualElectricityCostRepository.GetFirstOrDefaultAsync(
            predicate: x => x.AcceptanceReportId == request.Id,
            include: x => x
                .Include(c => c.AcceptanceReport).ThenInclude(a => a!.ProductionOutput)
                .Include(c => c.ActualEletricityEquipment).ThenInclude(e => e.Equipment).ThenInclude(e => e!.Code)
                .Include(c => c.ActualEletricityEquipment).ThenInclude(e => e.Equipment).ThenInclude(e => e!.Costs),
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.ActualElectricityCostNotFound);

        var effectiveDate = model.AcceptanceReport?.ProductionOutput?.StartMonth ?? DateOnly.FromDateTime(DateTime.UtcNow);

        return new ActualElectricityCostDetailDto
        {
            Id = model.Id,
            AcceptanceReportId = model.AcceptanceReportId,
            Equipments = model.ActualEletricityEquipment.Select(item =>
            {
                var unitPrice = item.Equipment?.GetEffectiveDateCost(effectiveDate) ?? 0;
                return new ActualElectricityEquipmentDetailDto
                {
                    EquipmentId = item.EquipmentId,
                    EquipmentCode = item.Equipment?.Code?.Value ?? string.Empty,
                    EquipmentName = item.Equipment?.Name ?? string.Empty,
                    ElectricityUnitPrice = unitPrice,
                    ActualElectricityConsumption = item.ActualElectricityConsumption,
                    TotalPrice = unitPrice * item.ActualElectricityConsumption
                };
            }).ToList()
        };
    }
}
