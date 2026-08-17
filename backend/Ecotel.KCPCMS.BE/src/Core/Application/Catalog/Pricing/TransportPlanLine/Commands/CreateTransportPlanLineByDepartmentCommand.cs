using Application.Common.Caching;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportPlanLine;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using PlannedTransportCostAdjustmentFactorEntity = Domain.Entities.Index.PlannedTransportCostAdjustmentFactor;
using PlannedTransportCostEntity = Domain.Entities.Pricing.PlannedTransportCost;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportPlanLine.Commands;

public record CreateTransportPlanLineByDepartmentCommand(
    CreateTransportPlanLineByDepartmentDto CreateModel) : IRequest<bool>;

public class CreateTransportPlanLineByDepartmentCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<CreateTransportPlanLineByDepartmentCommand, bool>
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
        CreateTransportPlanLineByDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var payload = await TransportPlanLineByDepartmentCommandHelper.BuildCreatePayloadAsync(
            request.CreateModel,
            _departmentRepository,
            _transportUnitPriceRepository);

        foreach (var month in payload.Months)
        {
            foreach (var item in month.Items)
            {
                var line = TransportPlanLineEntity.Create(
                    payload.DepartmentId,
                    item.ProductionProcessId,
                    item.ProductionMeters,
                    item.UnitOfMeasureId,
                    ProductUnitPriceScenarioType.Plan,
                    OutputType.PlanOutput,
                    month.Month,
                    month.Month,
                    item.EquipmentId,
                    item.EquipmentQuality,
                    item.TransportRouteId,
                    item.RouteDepartmentId,
                    haulDistanceId: null);

                await _transportPlanLineRepository.InsertAsync(line, cancellationToken);

                var cost = PlannedTransportCostEntity.Create(
                    line.Id,
                    item.TransportUnitPriceId,
                    null,
                    month.LowValuePerishableSupply
                        ? LowValuePerishableSupplyInclusion.Include
                        : LowValuePerishableSupplyInclusion.Exclude);
                await _plannedTransportCostRepository.InsertAsync(cost, cancellationToken);

                var adjustmentFactors = new List<PlannedTransportCostAdjustmentFactorEntity>();
                if (item.K1 != null)
                {
                    adjustmentFactors.Add(PlannedTransportCostAdjustmentFactorEntity.Create(
                        cost.Id, item.K1.AdjustmentFactorDescriptionId, item.K1.AdjustmentFactorId, item.K1.CustomValue));
                }

                if (item.K2 != null)
                {
                    adjustmentFactors.Add(PlannedTransportCostAdjustmentFactorEntity.Create(
                        cost.Id, item.K2.AdjustmentFactorDescriptionId, item.K2.AdjustmentFactorId, item.K2.CustomValue));
                }

                cost.AddAdjustmentFactors(adjustmentFactors);
            }
        }

        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
