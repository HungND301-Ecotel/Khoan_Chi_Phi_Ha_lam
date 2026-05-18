using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.Production.LongTermAnchorSeeds;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record GetDetailLongTermTrackingQuery(Guid AcceptanceReportId) : IRequest<GetDetailLongTermTrackingResponseDto>;

public class GetDetailLongTermTrackingQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetDetailLongTermTrackingQuery, GetDetailLongTermTrackingResponseDto>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AcceptanceReportItemLog> _logRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<LongTermAnchorSeed> _seedRepository = unitOfWork.GetRepository<LongTermAnchorSeed>();

    public async Task<GetDetailLongTermTrackingResponseDto> Handle(GetDetailLongTermTrackingQuery request, CancellationToken cancellationToken)
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
            disableTracking: true);

        if (acceptanceReport == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var productionOutput = acceptanceReport.ProductionOutput
            ?? throw new NotFoundException("ProductionOutput not found");

        var outputByProcessGroup = BuildOutputByProcessGroup(productionOutput);
        var seedSnapshots = await BuildAnchorSeedSnapshots(acceptanceReport, cancellationToken);

        // TH1: Logs của kỳ hiện tại (item mới thêm)
        var th1Logs = await _logRepository.GetAllAsync(
            predicate: l => l.AcceptanceReportId == request.AcceptanceReportId,
            include: q => q
                .Include(l => l.AcceptanceReportItemCategoryAllocation)
                    .ThenInclude(i => i.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
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

        // TH2: Logs từ các kỳ trước (item theo dõi dài kỳ)
        var previousLogs = await _logRepository.GetAllAsync(
            predicate: l =>
                l.AcceptanceReportId != request.AcceptanceReportId &&
                l.PeriodEndMonth < productionOutput.StartMonth,
            include: q => q
                .Include(l => l.AcceptanceReportItemCategoryAllocation)
                    .ThenInclude(i => i.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
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
        var th1ItemIds = th1Logs.Select(GetTrackingKey).ToHashSet();

        // Group theo AcceptanceReportItemId, loại trừ các item đã có trong TH1
        var previousLogsGrouped = previousLogs
            .Where(l => !th1ItemIds.Contains(GetTrackingKey(l)))
            .GroupBy(GetTrackingKey)
            .ToList();

        var items = new List<DetailLongTermTrackingItemDto>();

        // Process TH1: Item mới thêm kỳ này
        foreach (var log in th1Logs)
        {
            var item = log.AcceptanceReportItem;
            var part = item?.Part;
            if (!ShouldDisplayLongTermTracking(item, log))
            {
                continue;
            }

            // Phân biệt log thật sự mới vs log override
            var isNewItem = log.IssuedQuantity > 0 || log.TotalAmount > 0;

            var processGroupInfo = ResolveProcessGroupInfo(log);
            var currentActualOutput = log.ActualOutput;
            var currentPlannedOutput = log.PlannedOutput;
            var currentStandardOutput = log.StandardOutput;

            if (processGroupInfo.ProcessGroupId.HasValue && outputByProcessGroup.TryGetValue(processGroupInfo.ProcessGroupId.Value, out var groupOutput))
            {
                currentActualOutput = groupOutput.ActualOutput;
                currentPlannedOutput = groupOutput.PlannedOutput;
                currentStandardOutput = groupOutput.StandardOutput;
            }

            var dynamicValues = CalculateCurrentPeriodValues(
                totalValueToAccount: log.TotalValueToAccount,
                usageTime: log.UsageTime,
                remainingTime: log.RemainingTime,
                plannedOutput: currentPlannedOutput,
                standardOutput: currentStandardOutput,
                allocationRatio: log.AllocationRatio,
                isFullAccounting: log.IsFullAccounting);

            items.Add(new DetailLongTermTrackingItemDto
            {
                Id = log.Id,
                AcceptanceReportItemId = item!.Id,
                ProcessGroupId = processGroupInfo.ProcessGroupId,
                ProcessGroupCode = processGroupInfo.ProcessGroupCode,
                ProcessGroupName = processGroupInfo.ProcessGroupName,
                PartCode = part.Code?.Value ?? string.Empty,
                PartName = part.Name,
                MaterialCode = part.Code?.Value ?? string.Empty,
                MaterialName = part.Name,
                UnitOfMeasureName = part.UnitOfMeasure?.Name ?? string.Empty,
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
                ValueByStandard = dynamicValues.ValueByStandard,
                AllocationRatio = log.AllocationRatio,
                AccountedValueThisPeriod = dynamicValues.AccountedValueThisPeriod,
                PendingValueEndPeriod = dynamicValues.PendingValueEndPeriod,
                Note = log.Note,
                IsNewItem = isNewItem,
                IsFullAccounting = log.IsFullAccounting,
            });
        }

        // Process TH2: Item theo dõi dài kỳ từ các kỳ trước
        foreach (var group in previousLogsGrouped)
        {
            // Check xem đã có log override cho kỳ hiện tại chưa?
            var overrideLog = await _logRepository.GetFirstOrDefaultAsync(
                predicate: l =>
                    l.AcceptanceReportItemId == group.Key.AcceptanceReportItemId &&
                    l.AcceptanceReportItemCategoryAllocationId == group.Key.AcceptanceReportItemCategoryAllocationId &&
                    l.AcceptanceReportId == request.AcceptanceReportId,
                include: q => q
                    .Include(l => l.AcceptanceReportItemCategoryAllocation)
                        .ThenInclude(i => i.ProcessGroup)
                            .ThenInclude(pg => pg.Code)
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

            // Log mới nhất của item này
            var latestLog = group.OrderByDescending(l => l.PeriodEndMonth).First();
            var earliestLog = group.OrderBy(l => l.PeriodEndMonth).First();

            // Cộng dồn AllocationRatio của TẤT CẢ các kỳ trước
            var totalAllocatedTime = group.Sum(l => l.AllocationRatio);

            var item = latestLog.AcceptanceReportItem;
            var part = item?.Part;
            if (!ShouldDisplayLongTermTracking(item, overrideLog ?? latestLog))
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
                || ((overrideLog?.IsFullAccounting == true || latestLog.IsFullAccounting) && pendingValueStart == 0)
                || (pendingValueStart == 0 && (overrideLog == null || overrideLog.TotalAmount == 0))) // ✅ thêm dòng này
            {
                continue;
            }

            var totalValueToAccount = pendingValueStart;

            // Tính lại theo sản lượng kỳ hiện tại
            var actualOutput = productionOutput.ProductionMeters;
            var plannedOutput = latestLog.PlannedOutput;
            var standardOutput = productionOutput.StandardProductionMeters;

            var processGroupInfo = ResolveProcessGroupInfo(overrideLog ?? latestLog);

            if (processGroupInfo.ProcessGroupId.HasValue && outputByProcessGroup.TryGetValue(processGroupInfo.ProcessGroupId.Value, out var groupOutput))
            {
                actualOutput = groupOutput.ActualOutput;
                plannedOutput = groupOutput.PlannedOutput;
                standardOutput = groupOutput.StandardOutput;
            }

            decimal valueByStandard = 0;
            if (usageTime > 0 && standardOutput > 0)
            {
                valueByStandard = (totalValueToAccount / (decimal)usageTime)
                                  * ((decimal)plannedOutput / (decimal)standardOutput);
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

                // Kiểm tra: Nếu AllocationRatio >= RemainingTime → Hạch toán hết
                if (allocationRatio >= remainingTime)
                {
                    accountedValueThisPeriod = totalValueToAccount;
                    pendingValueEnd = 0;
                }
                else
                {
                    accountedValueThisPeriod = valueByStandard * (decimal)allocationRatio;
                    pendingValueEnd = totalValueToAccount - accountedValueThisPeriod;
                }
            }

            var displayAllocatedTime = Math.Abs(remainingTime) < 0.0001
                ? usageTime
                : Math.Min(usageTime, totalAllocatedTime + allocationRatio);
            var displayRemainingTime = Math.Max(0, usageTime - displayAllocatedTime);

            items.Add(new DetailLongTermTrackingItemDto
            {
                Id = logIdToDisplay,
                AcceptanceReportItemId = item!.Id,
                ProcessGroupId = processGroupInfo.ProcessGroupId,
                ProcessGroupCode = processGroupInfo.ProcessGroupCode,
                ProcessGroupName = processGroupInfo.ProcessGroupName,
                PartCode = part.Code?.Value ?? string.Empty,
                PartName = part.Name,
                MaterialCode = part.Code?.Value ?? string.Empty,
                MaterialName = part.Name,
                UnitOfMeasureName = part.UnitOfMeasure?.Name ?? string.Empty,
                PendingValueStartPeriod = pendingValueStart,
                IssuedQuantity = 0,
                UnitPrice = 0,
                TotalAmount = 0,
                OriginAmount = overrideLog?.OriginAmount ?? earliestLog.OriginAmount,
                TotalValueToAccount = totalValueToAccount,
                UsageTime = usageTime,
                AllocatedTime = displayAllocatedTime,
                RemainingTime = displayRemainingTime,
                ActualOutput = actualOutput,
                PlannedOutput = plannedOutput,
                StandardOutput = standardOutput,
                ValueByStandard = valueByStandard,
                AllocationRatio = allocationRatio,
                AccountedValueThisPeriod = accountedValueThisPeriod,
                PendingValueEndPeriod = pendingValueEnd,
                Note = overrideLog?.Note ?? latestLog.Note,
                IsNewItem = false,
                IsFullAccounting = overrideLog?.IsFullAccounting ?? false,
            });
        }

        foreach (var snapshot in seedSnapshots)
        {
            items.Add(new DetailLongTermTrackingItemDto
            {
                Id = snapshot.SeedItemId,
                AcceptanceReportItemId = Guid.Empty,
                ProcessGroupId = snapshot.ProcessGroupId,
                ProcessGroupCode = snapshot.ProcessGroupCode,
                ProcessGroupName = snapshot.ProcessGroupName,
                PartCode = snapshot.PartCode,
                PartName = snapshot.PartName,
                MaterialCode = snapshot.PartCode,
                MaterialName = snapshot.PartName,
                UnitOfMeasureName = snapshot.UnitOfMeasureName,
                PendingValueStartPeriod = snapshot.PendingValueStartPeriod,
                IssuedQuantity = snapshot.IssuedQuantity,
                UnitPrice = snapshot.UnitPrice,
                TotalAmount = snapshot.TotalAmount,
                OriginAmount = snapshot.OriginAmount,
                TotalValueToAccount = snapshot.TotalValueToAccount,
                UsageTime = snapshot.UsageTime,
                AllocatedTime = snapshot.AllocatedTime,
                RemainingTime = snapshot.RemainingTime,
                ActualOutput = snapshot.ActualOutput,
                PlannedOutput = snapshot.PlannedOutput,
                StandardOutput = snapshot.StandardOutput,
                ValueByStandard = snapshot.ValueByStandard,
                AllocationRatio = snapshot.AllocationRatio,
                AccountedValueThisPeriod = snapshot.AccountedValueThisPeriod,
                PendingValueEndPeriod = snapshot.PendingValueEndPeriod,
                Note = snapshot.Note,
                IsNewItem = false,
                IsFullAccounting = false,
                IsAnchorSeed = true
            });
        }

        var sortedItems = items
            .OrderByDescending(i => i.IsNewItem)
            .ThenBy(i => i.MaterialCode ?? i.PartCode)
            .ToList();

        var groupedByProcessGroup = sortedItems
            .Where(x => x.ProcessGroupId.HasValue)
            .GroupBy(x => x.ProcessGroupId!.Value)
            .Select(g =>
            {
                var processGroupInfo = g
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ProcessGroupCode) || !string.IsNullOrWhiteSpace(x.ProcessGroupName))
                    ?? g.First();

                return new DetailLongTermTrackingProcessGroupDto
                {
                    ProcessGroupId = g.Key,
                    ProcessGroupCode = processGroupInfo.ProcessGroupCode ?? string.Empty,
                    ProcessGroupName = processGroupInfo.ProcessGroupName ?? string.Empty,
                    Items = g.ToList()
                };
            })
            .OrderBy(g => g.ProcessGroupCode)
            .ToList();

        return new GetDetailLongTermTrackingResponseDto
        {
            AcceptanceReportId = acceptanceReport.Id,
            Items = sortedItems,
            ProcessGroups = groupedByProcessGroup
        };
    }

    private static (Guid AcceptanceReportItemId, Guid? AcceptanceReportItemCategoryAllocationId) GetTrackingKey(AcceptanceReportItemLog log)
        => (log.AcceptanceReportItemId, log.AcceptanceReportItemCategoryAllocationId);

    private static (Guid? ProcessGroupId, string? ProcessGroupCode, string? ProcessGroupName) ResolveProcessGroupInfo(AcceptanceReportItemLog log)
    {
        if (log.AcceptanceReportItemCategoryAllocation?.ProcessGroup != null)
        {
            return (
                log.AcceptanceReportItemCategoryAllocation.ProcessGroupId,
                log.AcceptanceReportItemCategoryAllocation.ProcessGroup.Code?.Value
                    ?? log.AcceptanceReportItemCategoryAllocation.ProcessGroup.FixedKey?.Key,
                log.AcceptanceReportItemCategoryAllocation.ProcessGroup.Name);
        }

        return (
            log.AcceptanceReportItem?.ProcessGroupId,
            log.AcceptanceReportItem?.ProcessGroup?.Code?.Value
                ?? log.AcceptanceReportItem?.ProcessGroup?.FixedKey?.Key,
            log.AcceptanceReportItem?.ProcessGroup?.Name);
    }

    private static bool ShouldDisplayLongTermTracking(AcceptanceReportItem? item, AcceptanceReportItemLog log)
        => item?.Part != null
            && item.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain
            && item.IsLongTermTracking
            && log.TotalValueToAccount > 0;

    private static (decimal ValueByStandard, decimal AccountedValueThisPeriod, decimal PendingValueEndPeriod) CalculateCurrentPeriodValues(
        decimal totalValueToAccount,
        double usageTime,
        double remainingTime,
        double plannedOutput,
        double standardOutput,
        double allocationRatio,
        bool isFullAccounting)
    {
        decimal valueByStandard = 0;
        if (usageTime > 0 && standardOutput > 0)
        {
            valueByStandard = (totalValueToAccount / (decimal)usageTime)
                * ((decimal)plannedOutput / (decimal)standardOutput);
        }

        if (Math.Abs(remainingTime) < 0.0001 || isFullAccounting)
        {
            return (totalValueToAccount, totalValueToAccount, 0);
        }

        var accountedValueThisPeriod = Math.Min(totalValueToAccount, valueByStandard * (decimal)allocationRatio);
        return (valueByStandard, accountedValueThisPeriod, totalValueToAccount - accountedValueThisPeriod);
    }

    private static Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> BuildOutputByProcessGroup(ProductionOutput productionOutput)
    {
        var result = new Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)>();

        foreach (var processGroup in productionOutput.ProductionOutputProcessGroups)
        {
            var plannedOutput = processGroup.PlanProductionMeters;

            result[processGroup.ProcessGroupId] = (
                processGroup.ProductionMeters,
                plannedOutput,
                processGroup.StandardProductionMeters);
        }

        return result;
    }

    private async Task<List<LongTermAnchorSeedTrackingHelper.TrackingSnapshot>> BuildAnchorSeedSnapshots(
        AcceptanceReport acceptanceReport,
        CancellationToken cancellationToken)
    {
        var departmentId = acceptanceReport.ProductionOutput?.DepartmentId;
        if (!departmentId.HasValue)
        {
            return [];
        }

        var seed = await _seedRepository.GetFirstOrDefaultAsync(
            predicate: s => s.DepartmentId == departmentId.Value,
            include: q => q
                .Include(s => s.Items)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(p => p.Code)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(p => p.UnitOfMeasure)
                .Include(s => s.Items)
                    .ThenInclude(i => i.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
                .Include(s => s.ProcessGroupMetrics),
            disableTracking: true);

        if (seed == null || !seed.Items.Any())
        {
            return [];
        }

        var processGroupMetrics = seed.ProcessGroupMetrics
            .GroupBy(x => x.ProcessGroupId)
            .ToDictionary(
                x => x.Key,
                x => (
                    PlannedOutput: x.First().PlannedOutput,
                    StandardOutput: x.First().StandardOutput));

        var reports = await _acceptanceReportRepository.GetAllAsync(
            predicate: a => a.ProductionOutput.DepartmentId == departmentId.Value,
            include: q => q
                .Include(a => a.ProductionOutput)
                    .ThenInclude(p => p.ProductionOutputProcessGroups)
                        .ThenInclude(pg => pg.ProductionOutputProducts),
            disableTracking: true);

        var orderedReports = reports
            .Where(a => a.ProductionOutput != null)
            .OrderBy(a => a.ProductionOutput!.StartMonth)
            .Select(a => new LongTermAnchorSeedTrackingHelper.ReportContext(
                a.Id,
                a.ProductionOutput!.StartMonth,
                a.ProductionOutput.ProductionMeters,
                a.ProductionOutput.StandardProductionMeters,
                BuildOutputByProcessGroup(a.ProductionOutput)))
            .ToList();

        return LongTermAnchorSeedTrackingHelper.BuildSnapshots(seed.Items, processGroupMetrics, orderedReports, acceptanceReport.Id);
    }
}

