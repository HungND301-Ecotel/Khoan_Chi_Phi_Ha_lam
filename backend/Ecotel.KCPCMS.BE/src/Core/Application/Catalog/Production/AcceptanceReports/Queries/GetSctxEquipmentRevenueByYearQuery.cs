using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record GetSctxEquipmentRevenueByYearQuery(
    Guid EquipmentId,
    Guid? DepartmentId = null,
    string? FromMonth = null,
    string? ToMonth = null) : IRequest<SctxEquipmentRevenueResponseDto>;

public class GetSctxEquipmentRevenueByYearQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSctxEquipmentRevenueByYearQuery, SctxEquipmentRevenueResponseDto>
{
    private readonly IWriteRepository<PlannedMaintainCostAdjustmentFactor> _plannedMaintainFactorRepository = unitOfWork.GetRepository<PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    public async Task<SctxEquipmentRevenueResponseDto> Handle(GetSctxEquipmentRevenueByYearQuery request, CancellationToken cancellationToken)
    {
        var hasFromMonth = !string.IsNullOrWhiteSpace(request.FromMonth);
        var hasToMonth = !string.IsNullOrWhiteSpace(request.ToMonth);

        if (hasFromMonth != hasToMonth)
        {
            throw new BadRequestException("Tháng bắt đầu và tháng kết thúc phải được cung cấp cùng nhau.");
        }

        if (!hasFromMonth)
        {
            throw new BadRequestException("Vui lòng cung cấp FromMonth và ToMonth.");
        }

        var startMonth = ParseMonth(request.FromMonth!);
        var endMonth = ParseMonth(request.ToMonth!);

        if (startMonth > endMonth)
        {
            throw new BadRequestException("Khoảng tháng không hợp lệ.");
        }

        var plannedFactors = await _plannedMaintainFactorRepository.GetAll()
            .Where(x => x.MaintainUnitPrice != null
                && x.MaintainUnitPrice.EquipmentId == request.EquipmentId
                && x.PlannedMaintainCost != null
                && x.PlannedMaintainCost.Output.StartMonth <= endMonth
                && x.PlannedMaintainCost.Output.EndMonth >= startMonth
                && x.PlannedMaintainCost.ProductUnitPrice != null
                && x.PlannedMaintainCost.ProductUnitPrice.ScenarioType == ProductUnitPriceScenarioType.Plan
                && (!request.DepartmentId.HasValue || x.PlannedMaintainCost.ProductUnitPrice.DepartmentId == request.DepartmentId))
            .Include(x => x.MaintainUnitPrice)
                .ThenInclude(m => m.MaintainUnitPriceEquipments)
                .ThenInclude(m => m.Part)
                .ThenInclude(p => p.Costs)
            .Include(x => x.PlannedMaintainCost)
                .ThenInclude(m => m.Output)
            .Include(x => x.PlannedMaintainCost)
                .ThenInclude(m => m.ProductUnitPrice)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var plannedUsageRaw = plannedFactors
            .Select(x => new PlannedUsageRow(
                x.PlannedMaintainCost!.OutputId,
                x.PlannedMaintainCost.ProductUnitPrice!.ProductId,
                x.PlannedMaintainCost.ProductUnitPrice.DepartmentId,
                x.PlannedMaintainCost.Output.StartMonth,
                x.PlannedMaintainCost.Output.EndMonth,
                x.PlannedMaintainCost.Output.ProductionMeters))
            .ToList();

        var plannedUnitPrices = plannedFactors
            .Select(x => new PlannedUnitPriceRow(
                x.PlannedMaintainCost!.Output.StartMonth,
                x.PlannedMaintainCost.Output.EndMonth,
                x.MaintainUnitPrice!.GetMaintainTotalPrice()
                    * (double)x.Quantity
                    * x.K6AdjustmentFactorValue))
            .ToList();

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

        var years = Enumerable.Range(startMonth.Year, endMonth.Year - startMonth.Year + 1)
            .Select(year => BuildYearResult(year, plannedUsages, actualOutputs, plannedUnitPrices, startMonth, endMonth))
            .ToList();

        return new SctxEquipmentRevenueResponseDto
        {
            EquipmentId = request.EquipmentId,
            Years = years
        };
    }

    private static SctxEquipmentRevenueByYearDto BuildYearResult(
        int year,
        List<PlannedUsageRow> plannedUsages,
        List<ActualOutputRow> actualOutputs,
        List<PlannedUnitPriceRow> plannedUnitPrices,
        DateOnly startMonth,
        DateOnly endMonth)
    {
        var startMonthNumber = year == startMonth.Year ? startMonth.Month : 1;
        var endMonthNumber = year == endMonth.Year ? endMonth.Month : 12;

        var months = Enumerable.Range(startMonthNumber, endMonthNumber - startMonthNumber + 1)
            .Select(m =>
            {
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

                var unitPrice = plannedUnitPrices
                    .Where(x => x.StartMonth <= monthDate && x.EndMonth >= monthDate)
                    .Sum(x => x.UnitPrice);

                var initialRevenue = unitPrice * plannedOutput;
                var adjustedRevenue = unitPrice * actualOutput;

                return new SctxEquipmentRevenueByMonthDto
                {
                    Month = m,
                    UnitPrice = unitPrice,
                    PlannedOutput = plannedOutput,
                    ActualOutput = actualOutput,
                    InitialRevenue = initialRevenue,
                    AdjustedRevenue = adjustedRevenue
                };
            })
            .ToList();

        return new SctxEquipmentRevenueByYearDto
        {
            Year = year,
            Months = months
        };
    }

    private sealed record PlannedUsageRow(
        Guid OutputId,
        Guid ProductId,
        Guid? DepartmentId,
        DateOnly StartMonth,
        DateOnly EndMonth,
        double PlannedOutput);

    private sealed record PlannedUnitPriceRow(
        DateOnly StartMonth,
        DateOnly EndMonth,
        double UnitPrice);

    private sealed record ActualOutputRow(
        Guid ProductId,
        Guid? DepartmentId,
        DateOnly StartMonth,
        DateOnly EndMonth,
        double ActualOutput);

    private sealed record ProductDepartmentKey(Guid ProductId, Guid? DepartmentId);

    private static DateOnly ParseMonth(string value)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return new DateOnly(parsed.Year, parsed.Month, 1);
        }

        throw new BadRequestException("Định dạng tháng không hợp lệ. Dùng yyyy-MM.");
    }
}
