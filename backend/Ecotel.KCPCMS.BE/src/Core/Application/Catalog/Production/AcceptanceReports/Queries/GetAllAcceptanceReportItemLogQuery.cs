using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record GetAllAcceptanceReportItemLogQuery(Guid AcceptanceReportId) : IRequest<GetAllAcceptanceReportItemLogResponseDto>;

public class GetAllAcceptanceReportItemLogQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllAcceptanceReportItemLogQuery, GetAllAcceptanceReportItemLogResponseDto>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AcceptanceReportItemLog> _logRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();

    public async Task<GetAllAcceptanceReportItemLogResponseDto> Handle(GetAllAcceptanceReportItemLogQuery request, CancellationToken cancellationToken)
    {
        // Get AcceptanceReport with ProductionOutput
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == request.AcceptanceReportId,
            include: q => q
                .Include(a => a.ProductionOutput)
                    .ThenInclude(p => p.ProductUnitPriceProductionOutputs)
                        .ThenInclude(p => p.ProductUnitPrice)
                            .ThenInclude(p => p.Outputs)
                .Include(a => a.ProductionOutput)
                    .ThenInclude(p => p.ProductionOutputProcessGroups)
                        .ThenInclude(pg => pg.ProcessGroup)
                            .ThenInclude(pg => pg.Code)
                .Include(a => a.ProductionOutput)
                    .ThenInclude(p => p.ProductionOutputProcessGroups)
                        .ThenInclude(pg => pg.ProductionOutputProducts),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound); ;


        var productionOutput = acceptanceReport.ProductionOutput
            ?? throw new NotFoundException("ProductionOutput not found");

        var outputByProcessGroup = BuildOutputByProcessGroup(productionOutput);

        var th1Logs = await _logRepository.GetAllAsync(
            predicate: l => l.AcceptanceReportId == request.AcceptanceReportId,
            include: q => q
                .Include(l => l.AcceptanceReportItem)
                    .ThenInclude(i => i.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
                .Include(l => l.AcceptanceReportItem)
                        .ThenInclude(m => m.Part)
                            .ThenInclude(p => p.Code)
                .Include(l => l.AcceptanceReportItem)
                        .ThenInclude(m => m.Part)
                            .ThenInclude(p => p.UnitOfMeasure),
            disableTracking: true);

        var previousLogs = await _logRepository.GetAllAsync(
            predicate: l =>
                l.AcceptanceReportId != request.AcceptanceReportId &&
                l.PeriodEndMonth < productionOutput.StartMonth,
            include: q => q
                .Include(l => l.AcceptanceReportItem)
                    .ThenInclude(i => i.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
                .Include(l => l.AcceptanceReportItem)
                        .ThenInclude(m => m.Part)
                            .ThenInclude(p => p.Code)
                .Include(l => l.AcceptanceReportItem)
                        .ThenInclude(m => m.Part)
                            .ThenInclude(p => p.UnitOfMeasure),
            disableTracking: true);

        // Lấy AcceptanceReportItemId của TH1 để loại trừ khỏi TH2
        // (tránh trường hợp item vừa có log TH1 vừa có log cũ)
        var th1ItemIds = th1Logs.Select(l => l.AcceptanceReportItemId).ToHashSet();

        // Group theo AcceptanceReportItemId, loại trừ các item đã có trong TH1
        var previousLogsGrouped = previousLogs
            .Where(l => !th1ItemIds.Contains(l.AcceptanceReportItemId))
            .GroupBy(l => l.AcceptanceReportItemId)
            .ToList();

        var logDtos = new List<AcceptanceReportItemLogDto>();

        foreach (var log in th1Logs)
        {
            var item = log.AcceptanceReportItem;
            var part = item?.Part;
            if (part == null)
            {
                continue;
            }

            // Phân biệt log thật sự mới vs log override
            var isNewItem = log.IssuedQuantity > 0 || log.TotalAmount > 0 || log.PendingValueStartPeriod == 0;

            var currentActualOutput = log.ActualOutput;
            var currentPlannedOutput = log.PlannedOutput;
            var currentStandardOutput = log.StandardOutput;

            if (item?.ProcessGroupId.HasValue == true && outputByProcessGroup.TryGetValue(item.ProcessGroupId.Value, out var groupOutput))
            {
                currentActualOutput = groupOutput.ActualOutput;
                currentPlannedOutput = groupOutput.PlannedOutput;
                currentStandardOutput = groupOutput.StandardOutput;
            }

            logDtos.Add(new AcceptanceReportItemLogDto
            {
                Id = log.Id,
                AcceptanceReportItemId = item!.Id,
                ProcessGroupId = item.ProcessGroupId,
                ProcessGroupCode = item.ProcessGroup?.Code?.Value,
                ProcessGroupName = item.ProcessGroup?.Name,
                PartCode = part.Code?.Value,
                PartName = part.Name,
                UnitOfMeasureName = part.UnitOfMeasure?.Name,
                PendingValueStartPeriod = log.PendingValueStartPeriod,
                IssuedQuantity = log.IssuedQuantity,
                UnitPrice = log.UnitPrice,
                TotalAmount = log.TotalAmount,
                OriginAmount = log.OriginAmount,
                TotalValueToAccount = log.TotalValueToAccount,
                UsageTime = log.UsageTime,
                AllocatedTime = log.AllocatedTime,
                RemainingTime = log.RemainingTime,
                ActualOutput = currentActualOutput,
                PlannedOutput = currentPlannedOutput,
                StandardOutput = currentStandardOutput,
                ValueByStandard = log.ValueByStandard,
                AllocationRatio = log.AllocationRatio,
                AccountedValueThisPeriod = log.AccountedValueThisPeriod,
                PendingValueEndPeriod = log.PendingValueEndPeriod,
                Note = log.Note,
                IsNewItem = isNewItem,
                IsFullAccounting = log.IsFullAccounting
            });
        }

        foreach (var group in previousLogsGrouped)
        {
            // Check xem đã có log override cho kỳ hiện tại chưa?
            var overrideLog = await _logRepository.GetFirstOrDefaultAsync(
                predicate: l =>
                    l.AcceptanceReportItemId == group.Key &&
                    l.AcceptanceReportId == request.AcceptanceReportId,
                include: q => q
                    .Include(l => l.AcceptanceReportItem)
                        .ThenInclude(i => i.ProcessGroup)
                            .ThenInclude(pg => pg.Code)
                    .Include(l => l.AcceptanceReportItem)
                            .ThenInclude(m => m.Part)
                                .ThenInclude(p => p.Code)
                    .Include(l => l.AcceptanceReportItem)
                            .ThenInclude(m => m.Part)
                                .ThenInclude(p => p.UnitOfMeasure),
                disableTracking: true);

            // Log mới nhất của item này → lấy PendingValueEndPeriod làm PendingValueStart kỳ này
            var latestLog = group.OrderByDescending(l => l.PeriodEndMonth).First();
            var earliestLog = group.OrderBy(l => l.PeriodEndMonth).First();

            // Cộng dồn AllocationRatio của TẤT CẢ các kỳ trước để tính AllocatedTime chính xác
            var totalAllocatedTime = group.Sum(l => l.AllocationRatio);

            var item = latestLog.AcceptanceReportItem;
            var part = item?.Part;
            if (part == null)
            {
                continue;
            }

            var usageTime = latestLog.UsageTime;
            var remainingTime = usageTime - totalAllocatedTime;

            // GIÁ TRỊ CHỜ HẠCH TOÁN ĐẦU KỲ = GIÁ TRỊ CUỐI KỲ của kỳ gần nhất
            var pendingValueStart = latestLog.PendingValueEndPeriod;

            // Nếu đã hết thời gian sử dụng VÀ không còn giá trị để hạch toán thì bỏ qua
            if (remainingTime < 0
                || (Math.Abs(remainingTime) < 0.0001 && pendingValueStart == 0)
                || (pendingValueStart == 0 && overrideLog == null))  // ✅ thêm dòng này
            {
                continue;
            }

            // Thành tiền = 0 (TH2), TỔNG GIÁ TRỊ CẦN HẠCH TOÁN = PendingValueStart + 0
            var totalValueToAccount = pendingValueStart;

            // Tính lại theo sản lượng kỳ hiện tại
            var actualOutput = productionOutput.ProductionMeters;
            var plannedOutput = latestLog.PlannedOutput;
            var standardOutput = productionOutput.StandardProductionMeters;

            if (item?.ProcessGroupId.HasValue == true && outputByProcessGroup.TryGetValue(item.ProcessGroupId.Value, out var groupOutput))
            {
                actualOutput = groupOutput.ActualOutput;
                plannedOutput = groupOutput.PlannedOutput;
                standardOutput = groupOutput.StandardOutput;
            }

            decimal valueByStandard = 0;
            if (usageTime > 0 && standardOutput > 0)
            {
                valueByStandard = (totalValueToAccount / (decimal)usageTime)
                                  * ((decimal)actualOutput / (decimal)standardOutput);
            }

            // Ưu tiên AllocationRatio từ log override (nếu có)
            double allocationRatio;
            Guid logIdToDisplay;
            decimal accountedValueThisPeriod;
            decimal pendingValueEnd;

            // Nếu RemainingTime = 0 → Kỳ cuối, hạch toán hết
            if (Math.Abs(remainingTime) < 0.0001)
            {
                // Tỉ lệ phân bổ mặc định = 1 (nếu không có override)
                allocationRatio = overrideLog?.AllocationRatio ?? 1.0;
                logIdToDisplay = overrideLog?.Id ?? latestLog.Id;

                // Kỳ cuối: đồng bộ theo rule business, hiển thị hạch toán hết tại kỳ này
                valueByStandard = totalValueToAccount;

                // Hạch toán hết giá trị còn lại
                accountedValueThisPeriod = totalValueToAccount;
                pendingValueEnd = 0;
            }
            else
            {
                // RemainingTime > 0 → Lấy AllocationRatio
                if (overrideLog != null)
                {
                    allocationRatio = overrideLog.AllocationRatio;
                    logIdToDisplay = overrideLog.Id;
                }
                else
                {
                    allocationRatio = latestLog.AllocationRatio;
                    logIdToDisplay = latestLog.Id;
                }

                accountedValueThisPeriod = Math.Min(totalValueToAccount, valueByStandard * (decimal)allocationRatio);
                pendingValueEnd = totalValueToAccount - accountedValueThisPeriod;
            }

            logDtos.Add(new AcceptanceReportItemLogDto
            {
                Id = logIdToDisplay,
                AcceptanceReportItemId = item!.Id,
                ProcessGroupId = item.ProcessGroupId,
                ProcessGroupCode = item.ProcessGroup?.Code?.Value,
                ProcessGroupName = item.ProcessGroup?.Name,
                PartCode = part.Code?.Value,
                PartName = part.Name,
                UnitOfMeasureName = part.UnitOfMeasure?.Name,
                PendingValueStartPeriod = pendingValueStart,
                IssuedQuantity = 0,
                UnitPrice = 0,
                TotalAmount = 0,
                OriginAmount = overrideLog?.OriginAmount ?? earliestLog.OriginAmount,
                TotalValueToAccount = totalValueToAccount,
                UsageTime = usageTime,
                AllocatedTime = totalAllocatedTime,
                RemainingTime = remainingTime,
                ActualOutput = actualOutput,
                PlannedOutput = plannedOutput,
                StandardOutput = standardOutput,
                ValueByStandard = valueByStandard,
                AllocationRatio = allocationRatio,
                AccountedValueThisPeriod = accountedValueThisPeriod,
                PendingValueEndPeriod = pendingValueEnd,
                Note = overrideLog?.Note,
                IsNewItem = false,
                IsFullAccounting = overrideLog?.IsFullAccounting ?? false
            });
        }

        var sortedItems = logDtos
            .OrderByDescending(l => l.IsNewItem)
            .ThenBy(l => l.PartCode)
            .ToList();

        var groupedByProcessGroup = sortedItems
            .Where(x => x.ProcessGroupId.HasValue)
            .GroupBy(x => new { x.ProcessGroupId, x.ProcessGroupCode, x.ProcessGroupName })
            .Select(g => new AcceptanceReportItemLogProcessGroupDto
            {
                ProcessGroupId = g.Key.ProcessGroupId!.Value,
                ProcessGroupCode = g.Key.ProcessGroupCode ?? string.Empty,
                ProcessGroupName = g.Key.ProcessGroupName ?? string.Empty,
                Items = g.ToList()
            })
            .OrderBy(g => g.ProcessGroupCode)
            .ToList();

        return new GetAllAcceptanceReportItemLogResponseDto
        {
            AcceptanceReportId = acceptanceReport.Id,
            PeriodStartMonth = productionOutput.StartMonth,
            PeriodEndMonth = productionOutput.EndMonth,
            Items = sortedItems,
            ProcessGroups = groupedByProcessGroup
        };
    }

    private static Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> BuildOutputByProcessGroup(ProductionOutput productionOutput)
    {
        var result = new Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)>();

        foreach (var processGroup in productionOutput.ProductionOutputProcessGroups)
        {
            var plannedOutput = 0.0;

            foreach (var product in processGroup.ProductionOutputProducts)
            {
                var productUnitPriceLink = productionOutput.ProductUnitPriceProductionOutputs
                    .FirstOrDefault(x => x.ProductUnitPrice?.ProductId == product.ProductId);

                if (productUnitPriceLink?.ProductUnitPrice?.Outputs == null)
                {
                    continue;
                }

                var matchingPlan = productUnitPriceLink.ProductUnitPrice.Outputs
                    .FirstOrDefault(o => o.OutputType == OutputType.PlanOutput
                        && o.StartMonth == productionOutput.StartMonth
                        && o.EndMonth == productionOutput.EndMonth
                        && Math.Abs(o.ProductionMeters - productUnitPriceLink.ProductionMeters) < 0.0001);

                if (matchingPlan != null)
                {
                    plannedOutput += matchingPlan.ProductionMeters;
                }
            }

            result[processGroup.ProcessGroupId] = (
                processGroup.ProductionMeters,
                plannedOutput,
                processGroup.StandardProductionMeters);
        }

        return result;
    }
}