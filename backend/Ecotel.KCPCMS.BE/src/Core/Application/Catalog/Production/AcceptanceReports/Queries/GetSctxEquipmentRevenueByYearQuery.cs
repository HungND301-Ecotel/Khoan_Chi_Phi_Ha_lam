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
    Guid EquipmentId) : IRequest<SctxEquipmentRevenueResponseDto>;

public class GetSctxEquipmentRevenueByYearQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSctxEquipmentRevenueByYearQuery, SctxEquipmentRevenueResponseDto>
{
    private readonly IWriteRepository<AcceptanceReportItemLog> _logRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();

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
                && (x.AcceptanceReportItem.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain
                    || x.AcceptanceReportItem.AdditionalCost == AdditionalCost.Maintain))
            .Select(x => new LogRow(
                x.PeriodStartMonth.Year,
                x.PeriodStartMonth.Month,
                x.UnitPrice,
                x.ActualOutput,
                x.AcceptanceReportItem.ProcessGroupId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var processGroupIds = logs
            .Where(x => x.ProcessGroupId.HasValue)
            .Select(x => x.ProcessGroupId!.Value)
            .Distinct()
            .ToList();

        var plannedOutputs = processGroupIds.Count == 0
            ? []
            : await _outputRepository.GetAll()
                .Where(x => x.OutputType == OutputType.PlanOutput
                    && x.ProductUnitPrice != null
                    && x.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan
                    && processGroupIds.Contains(x.ProductUnitPrice.Product!.ProcessGroupId)
                    && x.StartMonth.Year <= request.ToYear
                    && x.EndMonth.Year >= request.FromYear)
                .Select(x => new PlannedOutputRow(
                    x.ProductUnitPrice!.Product!.ProcessGroupId,
                    x.StartMonth,
                    x.EndMonth,
                    x.ProductionMeters))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        var years = Enumerable.Range(request.FromYear, request.ToYear - request.FromYear + 1)
            .Select(year => BuildYearResult(year, logs, plannedOutputs))
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
        List<PlannedOutputRow> plannedOutputs)
    {
        var yearLogs = logs.Where(x => x.Year == year).ToList();

        var plannedOutputByMonth = yearLogs
            .Where(x => x.ProcessGroupId.HasValue)
            .Select(x => new { x.Month, ProcessGroupId = x.ProcessGroupId!.Value })
            .Distinct()
            .GroupBy(x => x.Month)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(mp => plannedOutputs
                    .Where(p => p.ProcessGroupId == mp.ProcessGroupId
                        && IsMonthWithinRange(p.StartMonth, p.EndMonth, year, mp.Month))
                    .Sum(p => p.ProductionMeters)));

        var monthMap = yearLogs
            .GroupBy(x => x.Month)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var plannedOutput = plannedOutputByMonth.GetValueOrDefault(g.Key, 0);
                    var actualOutput = g.Sum(i => i.ActualOutput);

                    var rawAdjustedRevenue = g.Sum(i => i.UnitPrice * (decimal)i.ActualOutput);

                    var unitPrice = 0m;
                    if (actualOutput > 0)
                    {
                        unitPrice = rawAdjustedRevenue / (decimal)actualOutput;
                    }
                    else
                    {
                        unitPrice = g.Select(i => i.UnitPrice).DefaultIfEmpty(0).Average();
                    }

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
            .Select(m => monthMap.TryGetValue(m, out var row)
                ? row
                : new SctxEquipmentRevenueByMonthDto
                {
                    Month = m,
                    UnitPrice = 0,
                    PlannedOutput = 0,
                    ActualOutput = 0,
                    InitialRevenue = 0,
                    AdjustedRevenue = 0
                })
            .ToList();

        return new SctxEquipmentRevenueByYearDto
        {
            Year = year,
            Months = months
        };
    }

    private static bool IsMonthWithinRange(DateOnly startDate, DateOnly endDate, int year, int month)
    {
        var startIndex = (startDate.Year * 12) + startDate.Month - 1;
        var endIndex = (endDate.Year * 12) + endDate.Month - 1;
        var targetIndex = (year * 12) + month - 1;

        return targetIndex >= startIndex && targetIndex <= endIndex;
    }

    private sealed record LogRow(
        int Year,
        int Month,
        decimal UnitPrice,
        double ActualOutput,
        Guid? ProcessGroupId);

    private sealed record PlannedOutputRow(
        Guid ProcessGroupId,
        DateOnly StartMonth,
        DateOnly EndMonth,
        double ProductionMeters);
}
