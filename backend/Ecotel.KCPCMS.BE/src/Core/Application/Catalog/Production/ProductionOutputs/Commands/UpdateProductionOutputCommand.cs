using Application.Catalog.Pricing.ProductUnitPrice.Commands;
using Application.Catalog.Production.ProductionOutputs.Utilities;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOutput;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.ProductionOutputs.Commands;

public record UpdateProductionOutputCommand(ProductionOutputDto UpdateModel) : IRequest<bool>;

public class UpdateProductionOutputCommandHandler(IUnitOfWork unitOfWork, IMediator mediator) : IRequestHandler<UpdateProductionOutputCommand, bool>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AcceptanceReportItemLog> _acceptanceReportItemLogRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Product> _productRepository = unitOfWork.GetRepository<Product>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    public async Task<bool> Handle(UpdateProductionOutputCommand request, CancellationToken cancellationToken)
    {
        var existProductionOutput = await _productionOutputRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            include: q => q
                .Include(x => x.ProductionOutputProcessGroups)
                    .ThenInclude(x => x.ProductionOutputProducts),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        // Kiểm tra khoảng thời gian trùng lặp với bản ghi trong DB (loại trừ bản ghi hiện tại)
        var allRecords = await _productionOutputRepository.GetAllAsync(disableTracking: true);
        var newPeriod = (request.UpdateModel.StartMonth, request.UpdateModel.EndMonth, Index: 0);

        if (OverlapChecker.HasOverlapWithExistingExclude(
            new[] { newPeriod },
            allRecords,
            new[] { request.UpdateModel.Id }))
        {
            throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var hasProcessGroupsPayload = request.UpdateModel.ProcessGroups is not null;
            var processGroups = await BuildProcessGroups(request.UpdateModel, cancellationToken);

            if (hasProcessGroupsPayload)
            {
                existProductionOutput.ClearProcessGroups();

                if (processGroups.Any())
                {
                    existProductionOutput.SetProcessGroups(processGroups);
                }

                existProductionOutput.Update(
                    request.UpdateModel.StartMonth,
                    request.UpdateModel.EndMonth,
                    existProductionOutput.ProductionMeters,
                    existProductionOutput.StandardProductionMeters);
            }
            else
            {
                existProductionOutput.Update(
                    request.UpdateModel.StartMonth,
                    request.UpdateModel.EndMonth,
                    request.UpdateModel.ProductionMeters,
                    request.UpdateModel.StandardProductionMeters);
            }

            await UpdateAffectedAcceptanceReportItemLogs(existProductionOutput, cancellationToken);

            _productionOutputRepository.Update(existProductionOutput);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            // Sync ProductUnitPrice sau khi commit transaction chính
            if (hasProcessGroupsPayload)
            {
                await SyncProductUnitPricesForAdjustment(existProductionOutput.Id, processGroups, cancellationToken);
            }
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

                groupEntity.AddProduct(ProductionOutputProduct.Create(productDto.ProductId, productDto.ProductionMeters));
            }

            result.Add(groupEntity);
        }

        return result;
    }

    private async Task SyncProductUnitPricesForAdjustment(
        Guid productionOutputId,
        List<ProductionOutputProcessGroup> processGroups,
        CancellationToken cancellationToken)
    {
        var productMeters = processGroups
            .SelectMany(x => x.ProductionOutputProducts)
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ProductionMeters));

        var productIds = productMeters.Keys.ToList();

        var allRelatedPrices = await _productUnitPriceRepository.GetAll()
            .Where(x => x.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                && (x.ProductUnitPriceProductionOutputs.Any(y => y.ProductionOutputId == productionOutputId)
                    || productIds.Contains(x.ProductId)))
            .Include(x => x.ProductUnitPriceProductionOutputs)
            .ToListAsync(cancellationToken);

        if (!productMeters.Any())
        {
            foreach (var price in allRelatedPrices)
            {
                var remainingOutputs = price.ProductUnitPriceProductionOutputs
                    .Where(x => x.ProductionOutputId != productionOutputId)
                    .ToDictionary(x => x.ProductionOutputId, x => x.ProductionMeters);

                if (remainingOutputs.Any())
                {
                    await mediator.Send(new UpdateAdjustmentProductUnitPriceCommand(
                        new UpdateAdjustmentProductUnitPriceDto
                        {
                            Id = price.Id,
                            ProductId = price.ProductId,
                            UnitOfMeasureId = price.UnitOfMeasureId,
                            ProductionOutputs = remainingOutputs
                        }), cancellationToken);
                }
                else
                {
                    _productUnitPriceRepository.Delete(price);
                }
            }

            await unitOfWork.SaveChangesAsync();
            return;
        }

        // Xóa liên kết cho những sản phẩm không còn trong payload
        var pricesToRemove = allRelatedPrices
            .Where(x => !productIds.Contains(x.ProductId))
            .ToList();

        foreach (var price in pricesToRemove)
        {
            var remainingOutputs = price.ProductUnitPriceProductionOutputs
                .Where(x => x.ProductionOutputId != productionOutputId)
                .ToDictionary(x => x.ProductionOutputId, x => x.ProductionMeters);

            if (remainingOutputs.Any())
            {
                await mediator.Send(new UpdateAdjustmentProductUnitPriceCommand(
                    new UpdateAdjustmentProductUnitPriceDto
                    {
                        Id = price.Id,
                        ProductId = price.ProductId,
                        UnitOfMeasureId = price.UnitOfMeasureId,
                        ProductionOutputs = remainingOutputs
                    }), cancellationToken);
            }
            else
            {
                _productUnitPriceRepository.Delete(price);
            }
        }

        var existingProductUnitPrices = allRelatedPrices
            .Where(x => productIds.Contains(x.ProductId))
            .ToList();

        var newProductIds = productIds
            .Except(existingProductUnitPrices.Select(x => x.ProductId))
            .ToList();

        // Tạo mới Adjustment ProductUnitPrice cho sản phẩm chưa có
        foreach (var productId in newProductIds)
        {
            var newPrice = Domain.Entities.Pricing.ProductUnitPrice.Create(
                productId, null, ProductUnitPriceScenarioType.Adjustment);
            newPrice.AddProductionOutput(productionOutputId, productMeters[productId]);
            await _productUnitPriceRepository.InsertAsync(newPrice, cancellationToken);
        }

        // Cập nhật meters cho những sản phẩm đã có Adjustment ProductUnitPrice
        foreach (var price in existingProductUnitPrices)
        {
            var updatedOutputs = price.ProductUnitPriceProductionOutputs
                .ToDictionary(x => x.ProductionOutputId, x => x.ProductionMeters);

            // Ghi đè meters mới cho productionOutputId hiện tại
            updatedOutputs[productionOutputId] = productMeters[price.ProductId];

            await mediator.Send(new UpdateAdjustmentProductUnitPriceCommand(
                new UpdateAdjustmentProductUnitPriceDto
                {
                    Id = price.Id,
                    ProductId = price.ProductId,
                    UnitOfMeasureId = price.UnitOfMeasureId,
                    ProductionOutputs = updatedOutputs
                }), cancellationToken);
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
                0,
                processGroup.StandardProductionMeters);
        }

        return result;
    }
}
