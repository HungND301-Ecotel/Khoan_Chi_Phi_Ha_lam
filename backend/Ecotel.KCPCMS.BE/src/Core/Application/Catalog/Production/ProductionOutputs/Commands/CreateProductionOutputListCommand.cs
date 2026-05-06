using Application.Catalog.Production.ProductionOutputs.Utilities;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOutput;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.ProductionOutputs.Commands;

public record CreateProductionOutputListCommand(IList<CreateProductionOutputDto> CreateModels) : IRequest<bool>;

public class CreateProductionOutputListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateProductionOutputListCommand, bool>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Product> _productRepository = unitOfWork.GetRepository<Product>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<ProductUnitPrice>();

    public async Task<bool> Handle(CreateProductionOutputListCommand request, CancellationToken cancellationToken)
    {
        if (!request.CreateModels.Any())
        {
            throw new BadRequestException(CustomResponseMessage.UpdateIdsEmpty);
        }

        // Kiểm tra khoảng thời gian trùng lặp trong batch và với bản ghi trong DB theo từng đơn vị (O(n log n))
        var allRecords = await _productionOutputRepository.GetAllAsync(disableTracking: true);
        var periodsByDepartment = request.CreateModels
            .Select((x, i) => new { Model = x, Index = i })
            .GroupBy(x => x.Model.DepartmentId);

        var hasAnyOverlap = periodsByDepartment.Any(group =>
        {
            var periods = group.Select(x => (x.Model.StartMonth, x.Model.EndMonth, x.Index));
            var existingRecords = allRecords.Where(x => x.DepartmentId == group.Key);
            return OverlapChecker.HasOverlapWithExisting(periods, existingRecords);
        });

        if (hasAnyOverlap)
        {
            throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
        }

        var departmentIds = request.CreateModels
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
            var productionOutputs = new List<ProductionOutput>();
            var processGroupsByOutput = new Dictionary<Guid, List<ProductionOutputProcessGroup>>();

            foreach (var createModel in request.CreateModels)
            {
                var processGroups = await BuildProcessGroups(createModel, cancellationToken);

                var totalProductionMeters = processGroups.Any()
                    ? processGroups.Sum(x => x.ProductionMeters)
                    : createModel.ProductionMeters;

                var totalStandardProductionMeters = processGroups.Any()
                    ? processGroups.Sum(x => x.StandardProductionMeters)
                    : createModel.StandardProductionMeters;

                var newProductionOutput = ProductionOutput.Create(
                    createModel.StartMonth,
                    createModel.EndMonth,
                    totalProductionMeters,
                    totalStandardProductionMeters,
                    createModel.DepartmentId);

                if (processGroups.Any())
                {
                    newProductionOutput.SetProcessGroups(processGroups);
                    processGroupsByOutput[newProductionOutput.Id] = processGroups;
                }

                productionOutputs.Add(newProductionOutput);
            }

            await _productionOutputRepository.InsertAsync(productionOutputs, cancellationToken);

            foreach (var productionOutput in productionOutputs)
            {
                if (processGroupsByOutput.TryGetValue(productionOutput.Id, out var processGroups) && processGroups.Any())
                {
                    await UpsertProductUnitPricesForAdjustment(productionOutput, processGroups, cancellationToken);
                }
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
