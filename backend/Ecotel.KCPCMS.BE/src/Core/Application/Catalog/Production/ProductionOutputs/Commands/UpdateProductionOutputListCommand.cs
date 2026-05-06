using Application.Catalog.Production.ProductionOutputs.Utilities;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOutput;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Production.ProductionOutputs.Commands;

public record UpdateProductionOutputListCommand(IList<ProductionOutputDto> UpdateModels) : IRequest<bool>;

public class UpdateProductionOutputListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductionOutputListCommand, bool>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AcceptanceReportItemLog> _acceptanceReportItemLogRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Product> _productRepository = unitOfWork.GetRepository<Product>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    public async Task<bool> Handle(UpdateProductionOutputListCommand request, CancellationToken cancellationToken)
    {
        var updateIds = request.UpdateModels.Select(x => x.Id).ToList();
        var distinctIds = updateIds.Distinct().ToList();

        if (distinctIds.Count != updateIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.UpdateIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.UpdateIdsEmpty);
        }

        var existProductionOutputs = await _productionOutputRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: q => q
                .Include(x => x.ProductionOutputProcessGroups)
                    .ThenInclude(x => x.ProductionOutputProducts),
            disableTracking: false);

        if (existProductionOutputs == null || !existProductionOutputs.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (existProductionOutputs.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        // Kiểm tra khoảng thời gian trùng lặp trong batch và với bản ghi trong DB theo từng đơn vị (O(n log n))
        var allRecords = await _productionOutputRepository.GetAllAsync(disableTracking: true);
        var periodsByDepartment = request.UpdateModels
            .Select((x, i) => new { Model = x, Index = i })
            .GroupBy(x => x.Model.DepartmentId);

        var hasAnyOverlap = periodsByDepartment.Any(group =>
        {
            var periods = group.Select(x => (x.Model.StartMonth, x.Model.EndMonth, x.Index));
            var existingRecords = allRecords.Where(x => x.DepartmentId == group.Key);
            return OverlapChecker.HasOverlapWithExistingExclude(periods, existingRecords, distinctIds);
        });

        if (hasAnyOverlap)
        {
            throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
        }

        var departmentIds = request.UpdateModels
            .Where(x => x.DepartmentId.HasValue)
            .Select(x => x.DepartmentId!.Value)
            .Distinct()
            .ToList();
        if (departmentIds.Any())
        {
            var existingDepartmentIds = (await _departmentRepository.GetAllAsync(
                predicate: x => departmentIds.Contains(x.Id),
                disableTracking: true)).Select(x => x.Id).ToHashSet();

            if (existingDepartmentIds.Count != departmentIds.Count)
            {
                throw new NotFoundException(CustomResponseMessage.EntityNotFound);
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var updateProductOutputs = new List<ProductionOutput>();
            foreach (var updateModel in request.UpdateModels)
            {
                var existProductionOutput = existProductionOutputs.FirstOrDefault(x => x.Id == updateModel.Id)
                    ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
                var previousDepartmentId = existProductionOutput.DepartmentId;

                var processGroups = await BuildProcessGroups(updateModel, cancellationToken);

                if (processGroups.Any())
                {
                    existProductionOutput.ClearProcessGroups();
                    existProductionOutput.SetProcessGroups(processGroups);
                    existProductionOutput.Update(
                        updateModel.StartMonth,
                        updateModel.EndMonth,
                        existProductionOutput.ProductionMeters,
                        existProductionOutput.StandardProductionMeters,
                        updateModel.DepartmentId);

                    await SyncProductUnitPricesForAdjustment(
                        existProductionOutput.Id,
                        updateModel.DepartmentId,
                        processGroups,
                        cancellationToken);
                }
                else
                {
                    existProductionOutput.Update(
                        updateModel.StartMonth,
                        updateModel.EndMonth,
                        updateModel.ProductionMeters,
                        updateModel.StandardProductionMeters,
                        updateModel.DepartmentId);

                    if (previousDepartmentId != updateModel.DepartmentId)
                    {
                        await SyncProductUnitPriceDepartmentByProductionOutput(
                            existProductionOutput.Id,
                            updateModel.DepartmentId,
                            cancellationToken);
                    }
                }

                await UpdateAffectedAcceptanceReportItemLogs(existProductionOutput, cancellationToken);

                updateProductOutputs.Add(existProductionOutput);
            }

            _productionOutputRepository.Update(updateProductOutputs);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return true;
    }

    private async Task<List<ProductionOutputProcessGroup>> BuildProcessGroups(
        ProductionOutputDto updateModel,
        CancellationToken cancellationToken)
    {
        if (updateModel.ProcessGroups == null || !updateModel.ProcessGroups.Any())
        {
            return new List<ProductionOutputProcessGroup>();
        }

        var processGroupIds = updateModel.ProcessGroups.Select(x => x.ProcessGroupId).Distinct().ToList();
        var products = updateModel.ProcessGroups.SelectMany(x => x.Products).ToList();
        var productIds = products.Select(x => x.ProductId).Distinct().ToList();

        var existingProcessGroupIds = (await _processGroupRepository.GetAllAsync(
            predicate: x => processGroupIds.Contains(x.Id),
            disableTracking: true)).Select(x => x.Id).ToHashSet();

        if (existingProcessGroupIds.Count != processGroupIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
        }

        var existingProducts = await _productRepository.GetAllAsync(
            predicate: x => productIds.Contains(x.Id),
            disableTracking: true);

        if (existingProducts.Count != productIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ProductNotFound);
        }

        var productsById = existingProducts.ToDictionary(x => x.Id);
        var result = new List<ProductionOutputProcessGroup>();

        foreach (var groupDto in updateModel.ProcessGroups)
        {
            if (groupDto.Products == null || !groupDto.Products.Any())
            {
                continue;
            }

            var groupEntity = ProductionOutputProcessGroup.Create(
                groupDto.ProcessGroupId,
                groupDto.PlanProductionMeters,
                groupDto.StandardProductionMeters);

            foreach (var productDto in groupDto.Products)
            {
                if (!productsById.TryGetValue(productDto.ProductId, out var product))
                {
                    throw new NotFoundException(CustomResponseMessage.ProductNotFound);
                }

                if (product.ProcessGroupId != groupDto.ProcessGroupId)
                {
                    throw new ConflictException(CustomResponseMessage.InvalidParams);
                }

                groupEntity.AddProduct(ProductionOutputProduct.Create(
                    productDto.ProductId,
                    productDto.ProductionMeters,
                    productDto.ActualAshContent));
            }

            result.Add(groupEntity);
        }

        return result;
    }

    private async Task SyncProductUnitPricesForAdjustment(
        Guid productionOutputId,
        Guid? departmentId,
        List<ProductionOutputProcessGroup> processGroups,
        CancellationToken cancellationToken)
    {
        var productMeters = processGroups
            .SelectMany(x => x.ProductionOutputProducts)
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ProductionMeters));
        var productIds = productMeters.Keys.ToList();

        var affectedProductUnitPrices = await _productUnitPriceRepository.GetAll()
            .Where(x => x.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                && (x.ProductUnitPriceProductionOutputs.Any(y => y.ProductionOutputId == productionOutputId)
                    || (productMeters.Keys.Contains(x.ProductId) && x.DepartmentId == departmentId)))
            .Include(x => x.ProductUnitPriceProductionOutputs)
            .ToListAsync(cancellationToken);

        foreach (var productUnitPrice in affectedProductUnitPrices)
        {
            if (productUnitPrice.DepartmentId == departmentId
                && productMeters.TryGetValue(productUnitPrice.ProductId, out var meters))
            {
                productUnitPrice.Update(
                    productUnitPrice.ProductId,
                    productUnitPrice.UnitOfMeasureId,
                    departmentId);
                productUnitPrice.AddProductionOutput(productionOutputId, meters);
                _productUnitPriceRepository.Update(productUnitPrice);
                productMeters.Remove(productUnitPrice.ProductId);
            }
            else
            {
                productUnitPrice.RemoveProductionOutput(productionOutputId);
                _productUnitPriceRepository.Update(productUnitPrice);
            }
        }

        foreach (var remaining in productMeters)
        {
            var newProductUnitPrice = Domain.Entities.Pricing.ProductUnitPrice.Create(
                remaining.Key,
                null,
                departmentId,
                ProductUnitPriceScenarioType.Adjustment);
            newProductUnitPrice.AddProductionOutput(productionOutputId, remaining.Value);
            await _productUnitPriceRepository.InsertAsync(newProductUnitPrice, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync();
    }

    private async Task SyncProductUnitPriceDepartmentByProductionOutput(
        Guid productionOutputId,
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var linkedAdjustmentProductUnitPrices = await _productUnitPriceRepository.GetAll()
            .Where(x => x.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                && x.ProductUnitPriceProductionOutputs.Any(y => y.ProductionOutputId == productionOutputId))
            .Include(x => x.ProductUnitPriceProductionOutputs)
            .ToListAsync(cancellationToken);

        foreach (var productUnitPrice in linkedAdjustmentProductUnitPrices)
        {
            var currentLink = productUnitPrice.ProductUnitPriceProductionOutputs
                .FirstOrDefault(x => x.ProductionOutputId == productionOutputId);
            if (currentLink == null)
            {
                continue;
            }

            if (productUnitPrice.DepartmentId == departmentId)
            {
                continue;
            }

            var productionMeters = currentLink.ProductionMeters;
            productUnitPrice.RemoveProductionOutput(productionOutputId);

            if (productUnitPrice.ProductUnitPriceProductionOutputs.Any())
            {
                _productUnitPriceRepository.Update(productUnitPrice);
            }
            else
            {
                _productUnitPriceRepository.Delete(productUnitPrice);
            }

            var targetProductUnitPrice = await _productUnitPriceRepository.GetFirstOrDefaultAsync(
                predicate: x => x.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                    && x.ProductId == productUnitPrice.ProductId
                    && x.DepartmentId == departmentId,
                include: q => q.Include(x => x.ProductUnitPriceProductionOutputs),
                disableTracking: false);

            if (targetProductUnitPrice == null)
            {
                var newPrice = Domain.Entities.Pricing.ProductUnitPrice.Create(
                    productUnitPrice.ProductId,
                    productUnitPrice.UnitOfMeasureId,
                    departmentId,
                    ProductUnitPriceScenarioType.Adjustment);
                newPrice.AddProductionOutput(productionOutputId, productionMeters);
                await _productUnitPriceRepository.InsertAsync(newPrice, cancellationToken);
            }
            else
            {
                targetProductUnitPrice.AddProductionOutput(productionOutputId, productionMeters);
                _productUnitPriceRepository.Update(targetProductUnitPrice);
            }
        }

        await unitOfWork.SaveChangesAsync();
    }

    private async Task UpdateAffectedAcceptanceReportItemLogs(ProductionOutput productionOutput, CancellationToken cancellationToken)
    {
        var acceptanceReportIds = (await _acceptanceReportRepository.GetAllAsync(
            predicate: x => x.ProductionOutputId == productionOutput.Id,
            disableTracking: true))
            .Select(x => x.Id)
            .ToList();

        if (!acceptanceReportIds.Any())
        {
            return;
        }

        var logs = await _acceptanceReportItemLogRepository.GetAllAsync(
            predicate: x => acceptanceReportIds.Contains(x.AcceptanceReportId),
            include: q => q.Include(x => x.AcceptanceReportItem),
            disableTracking: false);

        if (!logs.Any())
        {
            return;
        }

        var outputByProcessGroup = BuildOutputByProcessGroup(productionOutput);

        foreach (var log in logs)
        {
            var actualOutput = productionOutput.ProductionMeters;
            var plannedOutput = log.PlannedOutput;
            var standardOutput = productionOutput.StandardProductionMeters;

            if (log.AcceptanceReportItem?.ProcessGroupId.HasValue == true &&
                outputByProcessGroup.TryGetValue(log.AcceptanceReportItem.ProcessGroupId.Value, out var metrics))
            {
                actualOutput = metrics.ActualOutput;
                plannedOutput = metrics.PlannedOutput;
                standardOutput = metrics.StandardOutput;
            }

            log.UpdateOutputMetrics(actualOutput, plannedOutput, standardOutput, log.Note);
        }

        _acceptanceReportItemLogRepository.Update(logs);
    }

    private static Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> BuildOutputByProcessGroup(ProductionOutput productionOutput)
    {
        var result = new Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)>();

        foreach (var processGroup in productionOutput.ProductionOutputProcessGroups)
        {
            result[processGroup.ProcessGroupId] = (
                processGroup.ProductionMeters,
                processGroup.PlanProductionMeters,
                processGroup.StandardProductionMeters);
        }

        return result;
    }
}
