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
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Product> _productRepository = unitOfWork.GetRepository<Product>();
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

        // Kiểm tra khoảng thời gian trùng lặp trong batch và với bản ghi trong DB (O(n log n))
        var allRecords = await _productionOutputRepository.GetAllAsync(disableTracking: true);
        var periods = request.UpdateModels
            .Select((x, i) => (x.StartMonth, x.EndMonth, Index: i))
            .ToList();

        if (OverlapChecker.HasOverlapWithExistingExclude(periods, allRecords, distinctIds))
        {
            throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var updateProductOutputs = new List<ProductionOutput>();
            foreach (var updateModel in request.UpdateModels)
            {
                var existProductionOutput = existProductionOutputs.FirstOrDefault(x => x.Id == updateModel.Id)
                    ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

                var processGroups = await BuildProcessGroups(updateModel, cancellationToken);

                if (processGroups.Any())
                {
                    existProductionOutput.ClearProcessGroups();
                    existProductionOutput.SetProcessGroups(processGroups);
                    existProductionOutput.Update(
                        updateModel.StartMonth,
                        updateModel.EndMonth,
                        existProductionOutput.ProductionMeters,
                        existProductionOutput.StandardProductionMeters);

                    await SyncProductUnitPricesForAdjustment(existProductionOutput.Id, processGroups, cancellationToken);
                }
                else
                {
                    existProductionOutput.Update(
                        updateModel.StartMonth,
                        updateModel.EndMonth,
                        updateModel.ProductionMeters,
                        updateModel.StandardProductionMeters);
                }

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

        var affectedProductUnitPrices = await _productUnitPriceRepository.GetAll()
            .Where(x => x.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                && (x.ProductUnitPriceProductionOutputs.Any(y => y.ProductionOutputId == productionOutputId)
                    || productMeters.Keys.Contains(x.ProductId)))
            .Include(x => x.ProductUnitPriceProductionOutputs)
            .ToListAsync(cancellationToken);

        foreach (var productUnitPrice in affectedProductUnitPrices)
        {
            if (productMeters.TryGetValue(productUnitPrice.ProductId, out var meters))
            {
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
            var newProductUnitPrice = Domain.Entities.Pricing.ProductUnitPrice.Create(remaining.Key, null, ProductUnitPriceScenarioType.Adjustment);
            newProductUnitPrice.AddProductionOutput(productionOutputId, remaining.Value);
            await _productUnitPriceRepository.InsertAsync(newProductUnitPrice, cancellationToken);
        }
    }
}
