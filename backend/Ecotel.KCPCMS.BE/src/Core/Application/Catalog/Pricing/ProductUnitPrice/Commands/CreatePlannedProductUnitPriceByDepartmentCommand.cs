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

public record CreatePlannedProductUnitPriceByDepartmentCommand(
    CreatePlannedProductUnitPriceByDepartmentDto CreateModel) : IRequest<bool>;

public class CreatePlannedProductUnitPriceByDepartmentCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<CreatePlannedProductUnitPriceByDepartmentCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Department> _departmentRepository =
        unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<Product> _productRepository =
        unitOfWork.GetRepository<Product>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository =
        unitOfWork.GetRepository<UnitOfMeasure>();

    public async Task<bool> Handle(
        CreatePlannedProductUnitPriceByDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var payload = await PlannedProductUnitPriceByDepartmentCommandHelper.BuildCreatePayloadAsync(
            request.CreateModel,
            _departmentRepository,
            _productRepository,
            _unitOfMeasureRepository,
            _productUnitPriceRepository,
            cancellationToken);

        var existingCount = await _productUnitPriceRepository.GetAll()
            .CountAsync(
                x => x.ScenarioType == ProductUnitPriceScenarioType.Plan &&
                     x.DepartmentId == payload.DepartmentId,
                cancellationToken);
        if (existingCount > 0)
        {
            throw new ConflictException(CustomResponseMessage.ProductUnitPriceWithProductIdAlreadyExists);
        }

        var newEntities = payload.Products.Select(product =>
        {
            var productUnitPrice = Domain.Entities.Pricing.ProductUnitPrice.Create(
                product.ProductId,
                product.UnitOfMeasureId,
                payload.DepartmentId,
                ProductUnitPriceScenarioType.Plan);

            var outputs = product.Outputs.SelectMany(output => new[]
            {
                Output.Create(
                    0,
                    output.Month,
                    output.Month,
                    OutputType.ActualOutput),
                Output.Create(
                    output.ProductionMeters,
                    output.Month,
                    output.Month,
                    OutputType.PlanOutput,
                    output.PlanAshContent),
            });

            productUnitPrice.AddOutputs(outputs);
            return productUnitPrice;
        }).ToList();

        await _productUnitPriceRepository.InsertAsync(newEntities, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
