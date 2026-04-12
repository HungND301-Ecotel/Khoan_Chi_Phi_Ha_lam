using Application.Catalog.Production.ProductionOutputs.Utilities;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOutput;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.ProductionOutputs.Commands;

public record CreateProductionOutputCommand(CreateProductionOutputDto CreateModel) : IRequest<bool>;

public class CreateProductionOutputCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateProductionOutputCommand, bool>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Product> _productRepository = unitOfWork.GetRepository<Product>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    public async Task<bool> Handle(CreateProductionOutputCommand request, CancellationToken cancellationToken)
    {
        // Kiểm tra khoảng thời gian trùng lặp với bản ghi trong DB cùng đơn vị
        var allRecords = await _productionOutputRepository.GetAllAsync(disableTracking: true);
        var existingRecordsByDepartment = allRecords
            .Where(x => x.DepartmentId == request.CreateModel.DepartmentId)
            .ToList();
        var newPeriod = (request.CreateModel.StartMonth, request.CreateModel.EndMonth, Index: 0);

        if (OverlapChecker.HasOverlapWithExisting(new[] { newPeriod }, existingRecordsByDepartment))
        {
            throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
        }

        if (request.CreateModel.DepartmentId.HasValue)
        {
            var checkDepartmentExisted = await _departmentRepository.ExistsAsync(x => x.Id == request.CreateModel.DepartmentId.Value);
            if (!checkDepartmentExisted)
            {
                throw new NotFoundException(CustomResponseMessage.EntityNotFound);
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var processGroups = await BuildProcessGroups(request.CreateModel, cancellationToken);

            var totalProductionMeters = processGroups.Any()
                ? processGroups.Sum(x => x.ProductionMeters)
                : request.CreateModel.ProductionMeters;

            var totalStandardProductionMeters = processGroups.Any()
                ? processGroups.Sum(x => x.StandardProductionMeters)
                : request.CreateModel.StandardProductionMeters;

            var newProductionOutput = ProductionOutput.Create(
                request.CreateModel.StartMonth,
                request.CreateModel.EndMonth,
                totalProductionMeters,
                totalStandardProductionMeters,
                request.CreateModel.DepartmentId);

            if (processGroups.Any())
            {
                newProductionOutput.SetProcessGroups(processGroups);
            }

            await _productionOutputRepository.InsertAsync(newProductionOutput, cancellationToken);

            if (processGroups.Any())
            {
                await UpsertProductUnitPricesForAdjustment(newProductionOutput, processGroups, cancellationToken);
            }

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
        CreateProductionOutputDto createModel,
        CancellationToken cancellationToken)
    {
        if (createModel.ProcessGroups == null || !createModel.ProcessGroups.Any())
        {
            return new List<ProductionOutputProcessGroup>();
        }

        var processGroupIds = createModel.ProcessGroups.Select(x => x.ProcessGroupId).Distinct().ToList();
        var products = createModel.ProcessGroups.SelectMany(x => x.Products).ToList();
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

        foreach (var groupDto in createModel.ProcessGroups)
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

    private async Task UpsertProductUnitPricesForAdjustment(
        ProductionOutput productionOutput,
        List<ProductionOutputProcessGroup> processGroups,
        CancellationToken cancellationToken)
    {
        var productMeters = processGroups
            .SelectMany(x => x.ProductionOutputProducts)
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ProductionMeters));

        if (!productMeters.Any())
        {
            return;
        }

        var productIds = productMeters.Keys.ToList();

        var existingProductUnitPrices = await _productUnitPriceRepository.GetAll()
            .Where(x => productIds.Contains(x.ProductId)
                && x.DepartmentId == productionOutput.DepartmentId
                && x.ScenarioType == ProductUnitPriceScenarioType.Adjustment)
            .Include(x => x.ProductUnitPriceProductionOutputs)
            .ToListAsync(cancellationToken);

        foreach (var productId in productIds)
        {
            var productionMeters = productMeters[productId];
            var productUnitPrice = existingProductUnitPrices.FirstOrDefault(x => x.ProductId == productId);

            if (productUnitPrice == null)
            {
                productUnitPrice = Domain.Entities.Pricing.ProductUnitPrice.Create(
                    productId,
                    null,
                    productionOutput.DepartmentId,
                    ProductUnitPriceScenarioType.Adjustment);
                productUnitPrice.AddProductionOutput(productionOutput.Id, productionMeters);
                await _productUnitPriceRepository.InsertAsync(productUnitPrice, cancellationToken);
            }
            else
            {
                productUnitPrice.Update(
                    productUnitPrice.ProductId,
                    productUnitPrice.UnitOfMeasureId,
                    productionOutput.DepartmentId);
                productUnitPrice.AddProductionOutput(productionOutput.Id, productionMeters);
                _productUnitPriceRepository.Update(productUnitPrice);
            }
        }
    }
}
