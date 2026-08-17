using Application.Common.Caching;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportPlanLine;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PlannedTransportCostAdjustmentFactorEntity = Domain.Entities.Index.PlannedTransportCostAdjustmentFactor;
using PlannedTransportCostEntity = Domain.Entities.Pricing.PlannedTransportCost;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportPlanLine.Commands;

public record UpdateTransportPlanLineByDepartmentCommand(
    UpdateTransportPlanLineByDepartmentDto UpdateModel) : IRequest<bool>;

public class UpdateTransportPlanLineByDepartmentCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<UpdateTransportPlanLineByDepartmentCommand, bool>
{
    private const string CacheSignalKey = "TransportPlanLine";
    private readonly IWriteRepository<TransportPlanLineEntity> _transportPlanLineRepository =
        unitOfWork.GetRepository<TransportPlanLineEntity>();
    private readonly IWriteRepository<PlannedTransportCostEntity> _plannedTransportCostRepository =
        unitOfWork.GetRepository<PlannedTransportCostEntity>();
    private readonly IWriteRepository<Department> _departmentRepository =
        unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<TransportUnitPriceEntity> _transportUnitPriceRepository =
        unitOfWork.GetRepository<TransportUnitPriceEntity>();

    public async Task<bool> Handle(
        UpdateTransportPlanLineByDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var payload = await TransportPlanLineByDepartmentCommandHelper.BuildUpdatePayloadAsync(
            request.UpdateModel,
            _departmentRepository,
            _transportUnitPriceRepository);

        var existingLines = await _transportPlanLineRepository.GetAll()
            .Where(x =>
                x.ScenarioType == ProductUnitPriceScenarioType.Plan &&
                x.DepartmentId == payload.DepartmentId)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
            .ToListAsync(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var requestItems = payload.Months
                .SelectMany(m => m.Items.Select(item =>
                    (Month: m.Month, LowValuePerishableSupply: m.LowValuePerishableSupply, Item: item)))
                .ToList();
            var requestedLineIds = requestItems
                .Where(x => x.Item.TransportPlanLineId.HasValue)
                .Select(x => x.Item.TransportPlanLineId!.Value)
                .ToHashSet();

            var linesToDelete = existingLines
                .Where(x => !requestedLineIds.Contains(x.Id))
                .ToList();
            if (linesToDelete.Any())
            {
                _transportPlanLineRepository.Delete(linesToDelete);
            }

            var existingLinesById = existingLines.ToDictionary(x => x.Id, x => x);

            foreach (var (month, lowValuePerishableSupply, item) in requestItems)
            {
                var lowValuePerishableSupplyInclusion = lowValuePerishableSupply
                    ? LowValuePerishableSupplyInclusion.Include
                    : LowValuePerishableSupplyInclusion.Exclude;

                if (item.TransportPlanLineId.HasValue &&
                    existingLinesById.TryGetValue(item.TransportPlanLineId.Value, out var existingLine))
                {
                    existingLine.Update(
                        payload.DepartmentId,
                        item.ProductionProcessId,
                        item.ProductionMeters,
                        item.UnitOfMeasureId,
                        month,
                        month,
                        item.EquipmentId,
                        item.EquipmentQuality,
                        item.TransportRouteId,
                        item.RouteDepartmentId,
                        haulDistanceId: null);
                    _transportPlanLineRepository.Update(existingLine);

                    UpdateCost(existingLine.PlannedTransportCost, item, lowValuePerishableSupplyInclusion);
                    continue;
                }

                var newLine = TransportPlanLineEntity.Create(
                    payload.DepartmentId,
                    item.ProductionProcessId,
                    item.ProductionMeters,
                    item.UnitOfMeasureId,
                    ProductUnitPriceScenarioType.Plan,
                    OutputType.PlanOutput,
                    month,
                    month,
                    item.EquipmentId,
                    item.EquipmentQuality,
                    item.TransportRouteId,
                    item.RouteDepartmentId,
                    haulDistanceId: null);

                await _transportPlanLineRepository.InsertAsync(newLine, cancellationToken);

                var newCost = PlannedTransportCostEntity.Create(
                    newLine.Id, item.TransportUnitPriceId, null, lowValuePerishableSupplyInclusion);
                await _plannedTransportCostRepository.InsertAsync(newCost, cancellationToken);
                newCost.AddAdjustmentFactors(BuildAdjustmentFactors(newCost.Id, item));
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            cacheService.InvalidateGroup(CacheSignalKey);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private void UpdateCost(
        PlannedTransportCostEntity? cost,
        TransportPlanLineItemPayload item,
        LowValuePerishableSupplyInclusion lowValuePerishableSupplyInclusion)
    {
        if (cost == null)
        {
            return;
        }

        cost.Update(item.TransportUnitPriceId, null, lowValuePerishableSupplyInclusion);
        _plannedTransportCostRepository.Update(cost);

        cost.ClearAdjustmentFactors();
        cost.AddAdjustmentFactors(BuildAdjustmentFactors(cost.Id, item));
    }

    private static List<PlannedTransportCostAdjustmentFactorEntity> BuildAdjustmentFactors(
        Guid plannedTransportCostId,
        TransportPlanLineItemPayload item)
    {
        var factors = new List<PlannedTransportCostAdjustmentFactorEntity>();
        if (item.K1 != null)
        {
            factors.Add(PlannedTransportCostAdjustmentFactorEntity.Create(
                plannedTransportCostId, item.K1.AdjustmentFactorDescriptionId, item.K1.AdjustmentFactorId, item.K1.CustomValue));
        }

        if (item.K2 != null)
        {
            factors.Add(PlannedTransportCostAdjustmentFactorEntity.Create(
                plannedTransportCostId, item.K2.AdjustmentFactorDescriptionId, item.K2.AdjustmentFactorId, item.K2.CustomValue));
        }

        return factors;
    }
}
