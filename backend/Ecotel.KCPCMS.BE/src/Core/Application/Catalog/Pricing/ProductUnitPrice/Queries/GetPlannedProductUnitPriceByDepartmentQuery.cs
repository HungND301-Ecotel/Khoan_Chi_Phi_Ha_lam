using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Queries;

public record GetPlannedProductUnitPriceByDepartmentQuery(Guid DepartmentId)
    : IRequest<PlannedProductUnitPriceByDepartmentDetailDto>;

public class GetPlannedProductUnitPriceByDepartmentQueryHandler(IUnitOfWork unitOfWork, ISender sender)
    : IRequestHandler<GetPlannedProductUnitPriceByDepartmentQuery, PlannedProductUnitPriceByDepartmentDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly ISender _sender = sender;

    public async Task<PlannedProductUnitPriceByDepartmentDetailDto> Handle(
        GetPlannedProductUnitPriceByDepartmentQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _productUnitPriceRepository.GetAll()
            .Where(x =>
                x.ScenarioType == ProductUnitPriceScenarioType.Plan &&
                x.DepartmentId == request.DepartmentId)
            .SelectMany(x => x.Outputs
                .Where(o => o.OutputType == OutputType.PlanOutput)
                .Select(o => new
                {
                    ProductUnitPriceId = x.Id,
                    OutputId = o.Id,
                    DepartmentId = x.DepartmentId,
                    DepartmentCode = x.Department != null && x.Department.Code != null
                        ? x.Department.Code.Value
                        : string.Empty,
                    DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                    ProductId = x.ProductId,
                    ProductCode = x.Product != null ? x.Product.Code.Value : string.Empty,
                    ProductName = x.Product != null ? x.Product.Name : string.Empty,
                    ProcessGroupId = x.Product != null ? x.Product.ProcessGroupId : Guid.Empty,
                    ProcessGroupCode = x.Product != null && x.Product.ProcessGroup != null && x.Product.ProcessGroup.FixedKey != null
                        ? x.Product.ProcessGroup.FixedKey.Key
                        : string.Empty,
                    ProcessGroupName = x.Product != null && x.Product.ProcessGroup != null
                        ? x.Product.ProcessGroup.Name
                        : string.Empty,
                    ProcessGroupType = x.Product != null && x.Product.ProcessGroup != null && x.Product.ProcessGroup.FixedKey != null
                        ? x.Product.ProcessGroup.FixedKey.Type.ToProcessGroupType()
                        : ProcessGroupType.None,
                    UnitOfMeasureId = x.UnitOfMeasureId,
                    UnitOfMeasureName = x.UnitOfMeasure != null ? x.UnitOfMeasure.Name : string.Empty,
                    Month = o.StartMonth,
                    o.ProductionMeters,
                    o.PlanAshContent,
                }))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!rows.Any())
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        var outputTotalCosts = new Dictionary<Guid, double>();
        foreach (var productUnitPriceId in rows.Select(x => x.ProductUnitPriceId).Distinct())
        {
            var detail = await _sender.Send(
                new GetPlannedProductUnitPriceByIdQuery(productUnitPriceId),
                cancellationToken);

            foreach (var output in detail.Outputs)
            {
                outputTotalCosts[output.Id] = output.TotalPrice;
            }
        }

        var department = rows.First();
        return new PlannedProductUnitPriceByDepartmentDetailDto
        {
            DepartmentId = department.DepartmentId ?? Guid.Empty,
            DepartmentCode = department.DepartmentCode,
            DepartmentName = department.DepartmentName,
            Months = rows
                .GroupBy(x => x.Month)
                .OrderBy(x => x.Key)
                .Select(monthGroup => new PlannedProductUnitPriceByDepartmentMonthDto
                {
                    Month = monthGroup.Key,
                    Items = monthGroup
                        .OrderBy(x => x.ProductCode)
                        .Select(item => new PlannedProductUnitPriceByDepartmentItemDto
                        {
                            ProductUnitPriceId = item.ProductUnitPriceId,
                            OutputId = item.OutputId,
                            ProductId = item.ProductId,
                            ProductCode = item.ProductCode,
                            ProductName = item.ProductName,
                            ProcessGroupId = item.ProcessGroupId,
                            ProcessGroupCode = item.ProcessGroupCode,
                            ProcessGroupName = item.ProcessGroupName,
                            ProcessGroupType = item.ProcessGroupType,
                            UnitOfMeasureId = item.UnitOfMeasureId ?? Guid.Empty,
                            UnitOfMeasureName = item.UnitOfMeasureName,
                            ProductionMeters = item.ProductionMeters,
                            PlannedTotalCost = outputTotalCosts.GetValueOrDefault(item.OutputId, 0),
                            PlanAshContent = item.PlanAshContent,
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }
}
