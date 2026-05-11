using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record GetSctxEquipmentRevenueByYearQuery(
    int FromYear,
    int ToYear,
    Guid EquipmentId,
    Guid? DepartmentId = null) : IRequest<SctxEquipmentRevenueResponseDto>;

public class GetSctxEquipmentRevenueByYearQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSctxEquipmentRevenueByYearQuery, SctxEquipmentRevenueResponseDto>
{
    private readonly IWriteRepository<AcceptanceReportItemLog> _logRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    public async Task<SctxEquipmentRevenueResponseDto> Handle(GetSctxEquipmentRevenueByYearQuery request, CancellationToken cancellationToken)
    {
        if (request.FromYear is < 2000 or > 3000
            || request.ToYear is < 2000 or > 3000
            || request.FromYear > request.ToYear)
        {
            throw new BadRequestException("Year không hợp lệ.");
        }

        var logs = await _logRepository.GetAll()
            .Where(x => x.PeriodStartMonth.Year >= request.FromYear
                && x.PeriodStartMonth.Year <= request.ToYear
                && x.AcceptanceReportItem.EquipmentId == request.EquipmentId
                && (!request.DepartmentId.HasValue || x.AcceptanceReport.ProductionOutput.DepartmentId == request.DepartmentId)
                && (x.AcceptanceReportItem.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain
                    || x.AcceptanceReportItem.AdditionalCost == AdditionalCost.Maintain))
            .Select(x => new LogRow(
                x.PeriodStartMonth.Year,
                x.PeriodStartMonth.Month,
                x.UnitPrice))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var startMonth = new DateOnly(request.FromYear, 1, 1);
        var endMonth = new DateOnly(request.ToYear, 12, 1);

        var plannedUsageRaw = await _plannedMaintainFactorRepository.GetAll()
            .Where(x => x.MaintainUnitPrice != null
                && x.MaintainUnitPrice.EquipmentId == request.EquipmentId
                && x.PlannedMaintainCost != null
                && x.PlannedMaintainCost.Output.StartMonth <= endMonth
                && x.PlannedMaintainCost.Output.EndMonth >= startMonth
                && x.PlannedMaintainCost.ProductUnitPrice != null
                && x.PlannedMaintainCost.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan
                && (!request.DepartmentId.HasValue || x.PlannedMaintainCost.ProductUnitPrice.DepartmentId == request.DepartmentId))
            .Select(x => new PlannedUsageRow(
                x.PlannedMaintainCost!.OutputId,
                x.PlannedMaintainCost.ProductUnitPrice!.ProductId,
                x.PlannedMaintainCost.ProductUnitPrice.DepartmentId,
                x.PlannedMaintainCost.Output.StartMonth,
                x.PlannedMaintainCost.Output.EndMonth,
                x.PlannedMaintainCost.Output.ProductionMeters))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var plannedUsages = plannedUsageRaw
            .GroupBy(x => x.OutputId)
            .Select(g => g.First())
            .ToList();

        var productDepartmentKeys = plannedUsages
            .Select(x => new ProductDepartmentKey(x.ProductId, x.DepartmentId))
            .Distinct()
            .ToList();

        var productIds = productDepartmentKeys.Select(x => x.ProductId).Distinct().ToList();
        var departmentIds = productDepartmentKeys.Select(x => x.DepartmentId).Distinct().ToList();

        var actualOutputs = productIds.Any()
            ? await _productUnitPriceRepository.GetAll()
                .Where(x => x.ScenarioType == ProductUnitPriceScenarioType.Adjustment
                    && productIds.Contains(x.ProductId)
                    && departmentIds.Contains(x.DepartmentId)
                    && (!request.DepartmentId.HasValue || x.DepartmentId == request.DepartmentId))
                .SelectMany(x => x.ProductUnitPriceProductionOutputs
                    .Where(link => link.ProductionOutput != null
                        && link.ProductionOutput.StartMonth <= endMonth
                        && link.ProductionOutput.EndMonth >= startMonth)
                    .Select(link => new ActualOutputRow(
                        x.ProductId,
                        x.DepartmentId,
                        link.ProductionOutput!.StartMonth,
                        link.ProductionOutput.EndMonth,
                        link.ProductionMeters)))
                .AsNoTracking()
                .ToListAsync(cancellationToken)
            : new List<ActualOutputRow>();

        var years = Enumerable.Range(request.FromYear, request.ToYear - request.FromYear + 1)
            .Select(year => BuildYearResult(year, logs, plannedUsages, actualOutputs))
            .ToList();

        return new SctxEquipmentRevenueResponseDto
        {
            EquipmentId = request.EquipmentId,
            Years = years
        };
    }

    private static SctxEquipmentRevenueByYearDto BuildYearResult(
        int year,
        List<LogRow> logs,
        List<PlannedUsageRow> plannedUsages,
        List<ActualOutputRow> actualOutputs)
    {
        var yearLogs = logs.Where(x => x.Year == year).ToList();

        var monthMap = yearLogs
            .GroupBy(x => x.Month)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var monthDate = new DateOnly(year, g.Key, 1);
                    var monthlyProductKeys = plannedUsages
                        .Where(x => x.StartMonth <= monthDate && x.EndMonth >= monthDate)
                        .Select(x => new ProductDepartmentKey(x.ProductId, x.DepartmentId))
                        .Distinct()
                        .ToList();

                    var plannedOutput = plannedUsages
                        .Where(x => x.StartMonth <= monthDate && x.EndMonth >= monthDate)
                        .Sum(x => x.PlannedOutput);

                    var actualOutput = monthlyProductKeys.Sum(key => actualOutputs
                        .Where(x => x.ProductId == key.ProductId
                            && x.DepartmentId == key.DepartmentId
                            && x.StartMonth <= monthDate
                            && x.EndMonth >= monthDate)
                        .Sum(x => x.ActualOutput));

                    var unitPrice = 0m;
                    unitPrice = g.Select(i => i.UnitPrice).DefaultIfEmpty(0).Average();

                    var initialRevenue = unitPrice * (decimal)plannedOutput;
                    var adjustedRevenue = unitPrice * (decimal)actualOutput;

                    return new SctxEquipmentRevenueByMonthDto
                    {
                        Month = g.Key,
                        UnitPrice = unitPrice,
                        PlannedOutput = plannedOutput,
                        ActualOutput = actualOutput,
                        InitialRevenue = initialRevenue,
                        AdjustedRevenue = adjustedRevenue
                    };
                });

        var months = Enumerable.Range(1, 12)
            .Select(m =>
            {
                if (monthMap.TryGetValue(m, out var row))
                {
                    return row;
                }

                var monthDate = new DateOnly(year, m, 1);
                var monthlyProductKeys = plannedUsages
                    .Where(x => x.StartMonth <= monthDate && x.EndMonth >= monthDate)
                    .Select(x => new ProductDepartmentKey(x.ProductId, x.DepartmentId))
                    .Distinct()
                    .ToList();

                var plannedOutput = plannedUsages
                    .Where(x => x.StartMonth <= monthDate && x.EndMonth >= monthDate)
                    .Sum(x => x.PlannedOutput);

                var actualOutput = monthlyProductKeys.Sum(key => actualOutputs
                    .Where(x => x.ProductId == key.ProductId
                        && x.DepartmentId == key.DepartmentId
                        && x.StartMonth <= monthDate
                        && x.EndMonth >= monthDate)
                    .Sum(x => x.ActualOutput));

                return new SctxEquipmentRevenueByMonthDto
                {
                    Month = m,
                    UnitPrice = 0,
                    PlannedOutput = plannedOutput,
                    ActualOutput = actualOutput,
                    InitialRevenue = 0,
                    AdjustedRevenue = 0
                };
            })
            .ToList();

        return new SctxEquipmentRevenueByYearDto
        {
            Year = year,
            Months = months
        };
    }

    private sealed record LogRow(
        int Year,
        int Month,
        decimal UnitPrice);

    private sealed record PlannedUsageRow(
        Guid OutputId,
        Guid ProductId,
        Guid? DepartmentId,
        DateOnly StartMonth,
        DateOnly EndMonth,
        double PlannedOutput);

    private sealed record ActualOutputRow(
        Guid ProductId,
        Guid? DepartmentId,
        DateOnly StartMonth,
        DateOnly EndMonth,
        double ActualOutput);

    private sealed record ProductDepartmentKey(Guid ProductId, Guid? DepartmentId);
}
