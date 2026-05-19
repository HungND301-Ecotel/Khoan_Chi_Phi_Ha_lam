using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Commands;

internal sealed class PlannedProductUnitPriceByDepartmentPayload
{
    public Guid DepartmentId { get; init; }
    public List<PlannedProductUnitPriceByDepartmentProductPayload> Products { get; init; } =
        new();
}

internal sealed class PlannedProductUnitPriceByDepartmentProductPayload
{
    public Guid? ProductUnitPriceId { get; init; }
    public Guid ProductId { get; init; }
    public Guid UnitOfMeasureId { get; init; }
    public List<PlannedProductUnitPriceByDepartmentOutputPayload> Outputs { get; init; } =
        new();
}

internal sealed class PlannedProductUnitPriceByDepartmentOutputPayload
{
    public Guid? OutputId { get; init; }
    public DateOnly Month { get; init; }
    public double ProductionMeters { get; init; }
    public double PlanAshContent { get; init; }
}

internal static class PlannedProductUnitPriceByDepartmentCommandHelper
{
    public static async Task<PlannedProductUnitPriceByDepartmentPayload> BuildCreatePayloadAsync(
        CreatePlannedProductUnitPriceByDepartmentDto request,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<Product> productRepository,
        IWriteRepository<UnitOfMeasure> unitOfMeasureRepository,
        IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> productUnitPriceRepository,
        CancellationToken cancellationToken)
    {
        return await BuildPayloadAsync(
            request.DepartmentId,
            request.Months.Select(month => new MonthPayload(
                month.Month,
                month.Items.Select(item => new ItemPayload(
                    null,
                    null,
                    item.ProductId,
                    item.UnitOfMeasureId,
                    item.ProductionMeters,
                    item.PlanAshContent ?? 0,
                    month.Month)).ToList())).ToList(),
            departmentRepository,
            productRepository,
            unitOfMeasureRepository,
            productUnitPriceRepository,
            cancellationToken,
            isUpdate: false);
    }

    public static async Task<PlannedProductUnitPriceByDepartmentPayload> BuildUpdatePayloadAsync(
        UpdatePlannedProductUnitPriceByDepartmentDto request,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<Product> productRepository,
        IWriteRepository<UnitOfMeasure> unitOfMeasureRepository,
        IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> productUnitPriceRepository,
        CancellationToken cancellationToken)
    {
        return await BuildPayloadAsync(
            request.DepartmentId,
            request.Months.Select(month => new MonthPayload(
                month.Month,
                month.Items.Select(item => new ItemPayload(
                    item.ProductUnitPriceId,
                    item.OutputId,
                    item.ProductId,
                    item.UnitOfMeasureId,
                    item.ProductionMeters,
                    item.PlanAshContent ?? 0,
                    month.Month)).ToList())).ToList(),
            departmentRepository,
            productRepository,
            unitOfMeasureRepository,
            productUnitPriceRepository,
            cancellationToken,
            isUpdate: true);
    }

    private static async Task<PlannedProductUnitPriceByDepartmentPayload> BuildPayloadAsync(
        Guid departmentId,
        IList<MonthPayload> months,
        IWriteRepository<Department> departmentRepository,
        IWriteRepository<Product> productRepository,
        IWriteRepository<UnitOfMeasure> unitOfMeasureRepository,
        IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> productUnitPriceRepository,
        CancellationToken cancellationToken,
        bool isUpdate)
    {
        if (departmentId == Guid.Empty)
        {
            throw new BadRequestException(CustomResponseMessage.EntityNotFound);
        }

        if (!months.Any())
        {
            throw new BadRequestException(CustomResponseMessage.OutputEmpty);
        }

        var duplicatedMonths = months
            .GroupBy(x => x.Month)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicatedMonths.Any())
        {
            throw new ConflictException("Duplicated month in request");
        }

        var departmentExists = await departmentRepository.ExistsAsync(x => x.Id == departmentId);
        if (!departmentExists)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var allItems = months.SelectMany(x => x.Items).ToList();
        if (!allItems.Any())
        {
            throw new BadRequestException(CustomResponseMessage.OutputEmpty);
        }

        if (allItems.Any(x => x.ProductId == Guid.Empty))
        {
            throw new NotFoundException(CustomResponseMessage.ProductNotFound);
        }

        if (allItems.Any(x => x.UnitOfMeasureId == Guid.Empty))
        {
            throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
        }

        if (allItems.Any(x => x.ProductionMeters <= 0))
        {
            throw new BadRequestException("ProductionMeters must be greater than 0");
        }

        foreach (var month in months)
        {
            var duplicatedProducts = month.Items
                .GroupBy(x => x.ProductId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
            if (duplicatedProducts.Any())
            {
                throw new ConflictException("Duplicated product in month");
            }
        }

        var productUnitPairs = allItems
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.UnitOfMeasureId).Distinct().ToList());

        if (productUnitPairs.Any(x => x.Value.Count > 1))
        {
            throw new ConflictException("The same product must use a single unit of measure in one department");
        }

        var productIds = allItems.Select(x => x.ProductId).Distinct().ToList();
        var unitOfMeasureIds = allItems.Select(x => x.UnitOfMeasureId).Distinct().ToList();
        var productUnitPriceIds = allItems
            .Where(x => x.ProductUnitPriceId.HasValue)
            .Select(x => x.ProductUnitPriceId!.Value)
            .Distinct()
            .ToList();
        var outputIds = allItems
            .Where(x => x.OutputId.HasValue)
            .Select(x => x.OutputId!.Value)
            .Distinct()
            .ToList();

        var products = await productRepository.GetAllAsync(
            predicate: x => productIds.Contains(x.Id),
            disableTracking: true);
        if (products.Count != productIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ProductNotFound);
        }

        var units = await unitOfMeasureRepository.GetAllAsync(
            predicate: x => unitOfMeasureIds.Contains(x.Id),
            disableTracking: true);
        if (units.Count != unitOfMeasureIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
        }

        if (isUpdate && productUnitPriceIds.Any())
        {
            var existingProductUnitPrices = await productUnitPriceRepository.GetAll()
                .Where(x => productUnitPriceIds.Contains(x.Id))
                .Select(x => new { x.Id, x.DepartmentId, x.ProductId, x.ScenarioType })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (existingProductUnitPrices.Count != productUnitPriceIds.Count)
            {
                throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
            }

            if (existingProductUnitPrices.Any(x =>
                    x.ScenarioType != ProductUnitPriceScenarioType.Plan ||
                    x.DepartmentId != departmentId))
            {
                throw new ConflictException(CustomResponseMessage.InvalidParams);
            }

            foreach (var item in allItems.Where(x => x.ProductUnitPriceId.HasValue))
            {
                var existing = existingProductUnitPrices
                    .First(x => x.Id == item.ProductUnitPriceId!.Value);
                if (existing.ProductId != item.ProductId)
                {
                    throw new ConflictException(CustomResponseMessage.InvalidParams);
                }
            }
        }

        if (isUpdate && outputIds.Any())
        {
            var existingOutputs = await productUnitPriceRepository.GetAll()
                .Where(x => x.ScenarioType == ProductUnitPriceScenarioType.Plan && x.DepartmentId == departmentId)
                .SelectMany(x => x.Outputs
                    .Where(o => outputIds.Contains(o.Id) && o.OutputType == OutputType.PlanOutput)
                    .Select(o => new { o.Id, o.ProductUnitPriceId, x.ProductId }))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (existingOutputs.Count != outputIds.Count)
            {
                throw new NotFoundException(MessageCommon.DataNotFound);
            }

            foreach (var item in allItems.Where(x => x.OutputId.HasValue))
            {
                var existing = existingOutputs.First(x => x.Id == item.OutputId!.Value);
                if (item.ProductUnitPriceId.HasValue && existing.ProductUnitPriceId != item.ProductUnitPriceId.Value)
                {
                    throw new ConflictException(CustomResponseMessage.InvalidParams);
                }
                if (existing.ProductId != item.ProductId)
                {
                    throw new ConflictException(CustomResponseMessage.InvalidParams);
                }
            }
        }

        var groupedProducts = allItems
            .GroupBy(x => x.ProductId)
            .Select(group => new PlannedProductUnitPriceByDepartmentProductPayload
            {
                ProductUnitPriceId = group.Select(x => x.ProductUnitPriceId).FirstOrDefault(x => x.HasValue),
                ProductId = group.Key,
                UnitOfMeasureId = group.First().UnitOfMeasureId,
                Outputs = group
                    .Select(x => new PlannedProductUnitPriceByDepartmentOutputPayload
                    {
                        OutputId = x.OutputId,
                        Month = x.Month,
                        ProductionMeters = x.ProductionMeters,
                        PlanAshContent = x.PlanAshContent,
                    })
                    .OrderBy(x => x.Month)
                    .ToList(),
            })
            .ToList();

        return new PlannedProductUnitPriceByDepartmentPayload
        {
            DepartmentId = departmentId,
            Products = groupedProducts,
        };
    }

    private sealed record MonthPayload(DateOnly Month, IList<ItemPayload> Items);

    private sealed record ItemPayload(
        Guid? ProductUnitPriceId,
        Guid? OutputId,
        Guid ProductId,
        Guid UnitOfMeasureId,
        double ProductionMeters,
        double PlanAshContent,
        DateOnly Month);
}
