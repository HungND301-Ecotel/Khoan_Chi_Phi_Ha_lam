using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Queries;

public record GetAdjustmentProductUnitPriceByDepartmentQuery(Guid DepartmentId)
    : IRequest<AdjustmentProductUnitPriceByDepartmentDetailDto>;

public class GetAdjustmentProductUnitPriceByDepartmentQueryHandler(
    IUnitOfWork unitOfWork,
    ISender sender)
    : IRequestHandler<
        GetAdjustmentProductUnitPriceByDepartmentQuery,
        AdjustmentProductUnitPriceByDepartmentDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly ISender _sender = sender;

    public async Task<AdjustmentProductUnitPriceByDepartmentDetailDto> Handle(
        GetAdjustmentProductUnitPriceByDepartmentQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _productUnitPriceRepository.GetAll()
            .Where(x =>
                x.ScenarioType == ProductUnitPriceScenarioType.Adjustment &&
                x.DepartmentId == request.DepartmentId)
            .Select(x => new
            {
                ProductUnitPriceId = x.Id,
                DepartmentId = x.DepartmentId,
                DepartmentCode = x.Department != null && x.Department.Code != null
                    ? x.Department.Code.Value
                    : string.Empty,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!rows.Any())
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        var items = new List<(DateOnly Month, AdjustmentProductUnitPriceByDepartmentItemDto Item)>();

        foreach (var row in rows)
        {
            var detail = await _sender.Send(
                new GetAdjustmentProductUnitPriceByIdQuery(row.ProductUnitPriceId),
                cancellationToken);

            foreach (var productionOutput in detail.ProductionOutputs
                         .Where(x => x.StartMonth.HasValue)
                         .OrderBy(x => x.StartMonth))
            {
                var matchedPlannedOutput = detail.Outputs
                    .Where(x => x.StartMonth == productionOutput.StartMonth && x.EndMonth == productionOutput.EndMonth)
                    .OrderBy(x => x.StartMonth)
                    .FirstOrDefault();

                items.Add((
                    productionOutput.StartMonth!.Value,
                    new AdjustmentProductUnitPriceByDepartmentItemDto
                    {
                        ProductUnitPriceId = detail.Id,
                        PlannedOutputId = matchedPlannedOutput?.Id,
                        ProductionOutputId = productionOutput.ProductionOutputId ?? productionOutput.Id,
                        ProductId = detail.ProductId,
                        ProductCode = detail.ProductCode,
                        ProductName = detail.ProductName,
                        ProcessGroupId = detail.ProcessGroupId,
                        ProcessGroupCode = detail.ProcessGroupCode,
                        ProcessGroupName = detail.ProcessGroupName,
                        ProcessGroupType = detail.ProcessGroupType,
                        UnitOfMeasureId = detail.UnitOfMeasureId,
                        UnitOfMeasureName = detail.UnitOfMeasureName,
                        ProductionMeters = productionOutput.ProductionMeters ?? 0,
                        StandardProductionMeters = productionOutput.StandardProductionMeters ?? 0,
                        ActualAshContent = productionOutput.ActualAshContent ?? 0,
                        AdjustmentTotalCost = productionOutput.AdjTotalPrice,
                        AkRate = productionOutput.AkRate,
                        AkRatePercent = productionOutput.AkRatePercent,
                    }));
            }
        }

        var department = rows.First();
        return new AdjustmentProductUnitPriceByDepartmentDetailDto
        {
            DepartmentId = department.DepartmentId ?? Guid.Empty,
            DepartmentCode = department.DepartmentCode,
            DepartmentName = department.DepartmentName,
            Months = items
                .GroupBy(x => x.Month)
                .OrderBy(x => x.Key)
                .Select(monthGroup => new AdjustmentProductUnitPriceByDepartmentMonthDto
                {
                    Month = monthGroup.Key,
                    Items = monthGroup
                        .Select(x => x.Item)
                        .OrderBy(x => x.ProductCode)
                        .ToList(),
                })
                .ToList(),
        };
    }
}
