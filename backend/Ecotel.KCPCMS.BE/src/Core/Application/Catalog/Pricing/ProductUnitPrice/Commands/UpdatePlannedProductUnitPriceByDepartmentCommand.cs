using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Commands;

public record UpdatePlannedProductUnitPriceByDepartmentCommand(
    UpdatePlannedProductUnitPriceByDepartmentDto UpdateModel) : IRequest<bool>;

public class UpdatePlannedProductUnitPriceByDepartmentCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<UpdatePlannedProductUnitPriceByDepartmentCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository =
        unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<Department> _departmentRepository =
        unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<Product> _productRepository =
        unitOfWork.GetRepository<Product>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository =
        unitOfWork.GetRepository<UnitOfMeasure>();

    public async Task<bool> Handle(
        UpdatePlannedProductUnitPriceByDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var payload = await PlannedProductUnitPriceByDepartmentCommandHelper.BuildUpdatePayloadAsync(
            request.UpdateModel,
            _departmentRepository,
            _productRepository,
            _unitOfMeasureRepository,
            _productUnitPriceRepository,
            cancellationToken);

        var existingEntities = await _productUnitPriceRepository.GetAll()
            .Where(x =>
                x.ScenarioType == ProductUnitPriceScenarioType.Plan &&
                x.DepartmentId == payload.DepartmentId)
            .Include(x => x.Outputs)
            .Include(x => x.PlannedMaterialCosts)
            .Include(x => x.PlannedMaintainCosts)
            .Include(x => x.PlannedElectricityCosts)
            .ToListAsync(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var requestProductsById = payload.Products
                .Where(x => x.ProductUnitPriceId.HasValue)
                .ToDictionary(x => x.ProductUnitPriceId!.Value, x => x);
            var requestProductsByProductId = payload.Products.ToDictionary(x => x.ProductId, x => x);

            var entitiesToDelete = existingEntities
                .Where(x => !requestProductsById.ContainsKey(x.Id) && !requestProductsByProductId.ContainsKey(x.ProductId))
                .ToList();

            if (entitiesToDelete.Any())
            {
                _productUnitPriceRepository.Delete(entitiesToDelete);
            }

            var entitiesToUpdate = new List<Domain.Entities.Pricing.ProductUnitPrice>();
            foreach (var existingEntity in existingEntities.Except(entitiesToDelete))
            {
                var requestProduct =
                    requestProductsById.GetValueOrDefault(existingEntity.Id) ??
                    requestProductsByProductId.GetValueOrDefault(existingEntity.ProductId);

                if (requestProduct == null)
                {
                    continue;
                }

                existingEntity.Update(
                    requestProduct.ProductId,
                    requestProduct.UnitOfMeasureId,
                    payload.DepartmentId);

                SyncOutputs(existingEntity, requestProduct.Outputs);
                entitiesToUpdate.Add(existingEntity);
            }

            if (entitiesToUpdate.Any())
            {
                _productUnitPriceRepository.Update(entitiesToUpdate);
            }

            var existingEntityIds = existingEntities.Select(x => x.Id).ToHashSet();
            var newEntities = payload.Products
                .Where(x => !x.ProductUnitPriceId.HasValue || !existingEntityIds.Contains(x.ProductUnitPriceId.Value))
                .Select(product =>
                {
                    var productUnitPrice = Domain.Entities.Pricing.ProductUnitPrice.Create(
                        product.ProductId,
                        product.UnitOfMeasureId,
                        payload.DepartmentId,
                        ProductUnitPriceScenarioType.Plan);

                    var outputs = product.Outputs.SelectMany(output => new[]
                    {
                        Output.Create(0, output.Month, output.Month, OutputType.ActualOutput),
                        Output.Create(
                            output.ProductionMeters,
                            output.Month,
                            output.Month,
                            OutputType.PlanOutput,
                            output.PlanAshContent),
                    });
                    productUnitPrice.AddOutputs(outputs);
                    return productUnitPrice;
                })
                .ToList();

            if (newEntities.Any())
            {
                await _productUnitPriceRepository.InsertAsync(newEntities, cancellationToken);
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

    private void SyncOutputs(
        Domain.Entities.Pricing.ProductUnitPrice existingEntity,
        IList<PlannedProductUnitPriceByDepartmentOutputPayload> requestedOutputs)
    {
        var currentPlanOutputs = existingEntity.Outputs
            .Where(x => x.OutputType == OutputType.PlanOutput)
            .ToList();
        var currentActualOutputs = existingEntity.Outputs
            .Where(x => x.OutputType == OutputType.ActualOutput)
            .ToList();

        var matchedPlanOutputIds = new HashSet<Guid>();
        var matchedMonths = new HashSet<DateOnly>();

        foreach (var requestedOutput in requestedOutputs)
        {
            var matchedPlanOutput = currentPlanOutputs.FirstOrDefault(x =>
                    requestedOutput.OutputId.HasValue && x.Id == requestedOutput.OutputId.Value) ??
                currentPlanOutputs.FirstOrDefault(x =>
                    !matchedMonths.Contains(requestedOutput.Month) &&
                    x.StartMonth.Year == requestedOutput.Month.Year &&
                    x.StartMonth.Month == requestedOutput.Month.Month);

            if (matchedPlanOutput == null)
            {
                existingEntity.AddOutput(Output.Create(
                    0,
                    requestedOutput.Month,
                    requestedOutput.Month,
                    OutputType.ActualOutput));
                existingEntity.AddOutput(Output.Create(
                    requestedOutput.ProductionMeters,
                    requestedOutput.Month,
                    requestedOutput.Month,
                    OutputType.PlanOutput,
                    requestedOutput.PlanAshContent));
                matchedMonths.Add(requestedOutput.Month);
                continue;
            }

            var originalStartMonth = matchedPlanOutput.StartMonth;
            var originalEndMonth = matchedPlanOutput.EndMonth;
            var pairedActualOutput = currentActualOutputs.FirstOrDefault(x =>
                x.StartMonth.Year == originalStartMonth.Year &&
                x.StartMonth.Month == originalStartMonth.Month &&
                x.EndMonth.Year == originalEndMonth.Year &&
                x.EndMonth.Month == originalEndMonth.Month);

            matchedPlanOutput.Update(
                requestedOutput.ProductionMeters,
                requestedOutput.Month,
                requestedOutput.Month,
                requestedOutput.PlanAshContent);

            pairedActualOutput?.Update(
                pairedActualOutput.ProductionMeters,
                requestedOutput.Month,
                requestedOutput.Month,
                pairedActualOutput.PlanAshContent);

            matchedPlanOutputIds.Add(matchedPlanOutput.Id);
            matchedMonths.Add(requestedOutput.Month);
        }

        var planOutputsToDelete = currentPlanOutputs
            .Where(x => !matchedPlanOutputIds.Contains(x.Id) &&
                        !requestedOutputs.Any(y =>
                            y.Month.Year == x.StartMonth.Year &&
                            y.Month.Month == x.StartMonth.Month))
            .ToList();

        if (planOutputsToDelete.Any())
        {
            var actualOutputsToDelete = currentActualOutputs
                .Where(actual =>
                    planOutputsToDelete.Any(plan =>
                        plan.StartMonth.Year == actual.StartMonth.Year &&
                        plan.StartMonth.Month == actual.StartMonth.Month &&
                        plan.EndMonth.Year == actual.EndMonth.Year &&
                        plan.EndMonth.Month == actual.EndMonth.Month))
                .ToList();

            _outputRepository.Delete(planOutputsToDelete);
            if (actualOutputsToDelete.Any())
            {
                _outputRepository.Delete(actualOutputsToDelete);
            }
        }
    }
}
