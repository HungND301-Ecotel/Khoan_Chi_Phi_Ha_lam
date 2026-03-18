using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Dto.Catalog.PlannedMaintainCost;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.PlannedMaintainCost.Queries;

public record GetPlannedMaintainCostByIdQuery(DefaultIdType Id) : IRequest<PlannedMaintainCostDetailDto>;

public class GetPlannedMaintainCostByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPlannedMaintainCostByIdQuery, PlannedMaintainCostDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaintainCost> _plannedMaintainCostRespository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaintainCost>();
    public async Task<PlannedMaintainCostDetailDto> Handle(GetPlannedMaintainCostByIdQuery request, CancellationToken cancellationToken)
    {
        var plannedMaintainCost = await _plannedMaintainCostRespository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
            .Include(m => m.Output)
            .Include(m => m.PlannedMaintainCostAdjustmentFactors).ThenInclude(p => p.MaintainUnitPrice).ThenInclude(m => m.MaintainUnitPriceEquipments).ThenInclude(m => m.Part).ThenInclude(p => p.Costs)
            .Include(m => m.PlannedMaintainCostAdjustmentFactors).ThenInclude(p => p.MaintainUnitPrice).ThenInclude(m => m.MaintainUnitPriceEquipments).ThenInclude(m => m.Part).ThenInclude(p => p.Code)
            .Include(m => m.PlannedMaintainCostAdjustmentFactors).ThenInclude(p => p.MaintainUnitPrice).ThenInclude(m => m.Equipment).ThenInclude(e => e.Code!)
            .Include(m => m.PlannedMaintainCostAdjustmentFactors).ThenInclude(p => p.PlannedMaintainCostAdjustmentFactorDescriptions).ThenInclude(p => p.AdjustmentFactorDescription).ThenInclude(p => p.AdjustmentFactor).ThenInclude(p => p.Code!),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var mCost = new PlannedMaintainCostDetailDto
        {
            Id = plannedMaintainCost.Id,
            ProductUnitPriceId = plannedMaintainCost.ProductUnitPriceId,
            OutputId = plannedMaintainCost.OutputId,
            Costs = plannedMaintainCost.PlannedMaintainCostAdjustmentFactors.Select(p =>
            {
                return new PlannedMaintainCostAdjDto
                {
                    EquipmentId = p.MaintainUnitPrice!.EquipmentId,
                    EquipmentCode = p.MaintainUnitPrice.Equipment?.Code?.Value ?? string.Empty,
                    EquipmentName = p.MaintainUnitPrice.Equipment?.Name ?? string.Empty,
                    Quantity = p.Quantity,
                    MaintainUnitPriceId = p.MaintainUnitPriceId,
                    MaintainUnitPrice = p.MaintainUnitPrice.GetMaintainTotalPrice(),
                    TotalPrice = p.GetCurrentMaintainCost(),
                    K6AdjustmentFactorValue = p.K6AdjustmentFactorValue,
                    AdjustmentFactorDescriptions = p.PlannedMaintainCostAdjustmentFactorDescriptions.Select(a => new MaintainAjustmentFactorDescriptionDto
                    {
                        Id = a.AdjustmentFactorDescription?.Id ?? Guid.Empty,
                        AdjustmentFactorId = a.AdjustmentFactorDescription?.AdjustmentFactorId ?? Guid.Empty,
                        AdjustmentFactorCode = a.AdjustmentFactorDescription?.AdjustmentFactor?.Code?.Value ?? string.Empty,
                        AdjustmentFactorName = a.AdjustmentFactorDescription?.AdjustmentFactor?.Name ?? string.Empty,
                        Description = a.AdjustmentFactorDescription?.Description ?? "",
                        MaintenanceAdjustmentValue = a.AdjustmentFactorDescription?.MaintenanceAdjustmentValue ?? 0
                    }).ToList()
                };
            }).ToList()
        };
        return mCost;
    }
}