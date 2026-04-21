using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Dto.Catalog.PlannedElectricityCost;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.PlannedElectricityCost.Queries;

public record GetPlannedElectricityCostByIdQuery(DefaultIdType Id) : IRequest<PlannedElectricityCostDetailDto>;

public class GetPlannedElectricityCostByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPlannedElectricityCostByIdQuery, PlannedElectricityCostDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedElectricityCost> _plannedElectricityCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedElectricityCost>();
    public async Task<PlannedElectricityCostDetailDto> Handle(GetPlannedElectricityCostByIdQuery request, CancellationToken cancellationToken)
    {
        var plannedElectricityCost = await _plannedElectricityCostRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
            .Include(m => m.Output)
            .Include(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.ElectricityUnitPriceEquipment).ThenInclude(e => e.Equipment).ThenInclude(e => e.Costs)
            .Include(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.ElectricityUnitPriceEquipment).ThenInclude(e => e.Equipment).ThenInclude(e => e.Code)
            .Include(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.PlannedElectricityCostAdjustmentFactorDescriptions).ThenInclude(p => p.AdjustmentFactorDescription).ThenInclude(p => p.AdjustmentFactor).ThenInclude(p => p.Code!)
            .Include(m => m.PlannedElectricityCostAdjustmentFactors).ThenInclude(p => p.PlannedElectricityCostAdjustmentFactorDescriptions).ThenInclude(p => p.AdjustmentFactor).ThenInclude(p => p.Code!),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var mCost = new PlannedElectricityCostDetailDto
        {
            Id = plannedElectricityCost.Id,
            ProductUnitPriceId = plannedElectricityCost.ProductUnitPriceId,
            OutputId = plannedElectricityCost.OutputId,
            TrimmingCoefficient = plannedElectricityCost.TrimmingCoefficient,
            Costs = plannedElectricityCost.PlannedElectricityCostAdjustmentFactors.Select(p =>
            {
                return new PlannedElectricityCostAdjDto
                {
                    EquipmentId = p.ElectricityUnitPriceEquipment!.EquipmentId,
                    EquipmentCode = p.ElectricityUnitPriceEquipment.Equipment?.Code?.Value ?? string.Empty,
                    EquipmentName = p.ElectricityUnitPriceEquipment.Equipment?.Name ?? string.Empty,
                    Quantity = p.Quantity,
                    ElectricityUnitPriceEquipmentId = p.ElectricityUnitPriceId,
                    ElectricityUnitPrice = p.ElectricityUnitPriceEquipment.GetElectricityCostPerMetres(),
                    TotalPrice = p.GetCurrentElectricityCost(),
                    AdjustmentFactorDescriptions = p.PlannedElectricityCostAdjustmentFactorDescriptions.Select(a => new ElectricityAjustmentFactorDescriptionDto
                    {
                        Id = a.Id,
                        AdjustmentFactorDescriptionId = a.AdjustmentFactorDescriptionId,
                        AdjustmentFactorId = a.AdjustmentFactorDescription?.AdjustmentFactorId
                            ?? a.AdjustmentFactorId
                            ?? Guid.Empty,
                        AdjustmentFactorCode = a.AdjustmentFactorDescription?.AdjustmentFactor?.Code?.Value
                            ?? a.AdjustmentFactor?.Code?.Value
                            ?? string.Empty,
                        AdjustmentFactorName = a.AdjustmentFactorDescription?.AdjustmentFactor?.Name
                            ?? a.AdjustmentFactor?.Name
                            ?? string.Empty,
                        Description = a.AdjustmentFactorDescription?.Description ?? string.Empty,
                        ElectricityAdjustmentValue = a.AdjustmentFactorDescription?.ElectricityAdjustmentValue,
                        CustomValue = a.CustomValue,
                        EffectiveValue = a.EffectiveValue
                    }).ToList()
                };
            }).ToList()
        };
        return mCost;
    }
}
