// UpdateAcceptanceReportItemLogListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Commands;

public record UpdateAcceptanceReportItemLogListCommand(IList<UpdateAcceptanceReportItemLogDto> UpdateModels) : IRequest<IList<UpdateAcceptanceReportItemLogResponseDto>>;

public class UpdateAcceptanceReportItemLogListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAcceptanceReportItemLogListCommand, IList<UpdateAcceptanceReportItemLogResponseDto>>
{
    private readonly IWriteRepository<AcceptanceReportItemLog> _logRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<IList<UpdateAcceptanceReportItemLogResponseDto>> Handle(UpdateAcceptanceReportItemLogListCommand request, CancellationToken cancellationToken)
    {
        var updateModels = request.UpdateModels;

        if (updateModels == null || updateModels.Count == 0)
        {
            throw new BadRequestException("Update models cannot be empty");
        }

        // Validate all items have the same AcceptanceReportId
        var acceptanceReportIds = updateModels.Select(m => m.AcceptanceReportId).Distinct().ToList();
        if (acceptanceReportIds.Count > 1)
        {
            throw new BadRequestException("All update models must belong to the same AcceptanceReport");
        }

        var currentAcceptanceReportId = acceptanceReportIds.First();

        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == currentAcceptanceReportId,
            include: q => q
                .Include(a => a.ProductionOutput)
                    .ThenInclude(p => p.ProductUnitPriceProductionOutputs)
                        .ThenInclude(p => p.ProductUnitPrice)
                            .ThenInclude(p => p.Outputs)
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

        var logIds = updateModels.Select(m => m.Id).ToList();

        var logs = await _logRepository.GetAllAsync(
            predicate: l => logIds.Contains(l.Id),
            include: q => q
                .Include(l => l.AcceptanceReportItemCategoryAllocation)
                .Include(l => l.AcceptanceReportItem),
            disableTracking: false);

        if (logs.Count != updateModels.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var results = new List<UpdateAcceptanceReportItemLogResponseDto>();

            foreach (var updateModel in updateModels)
            {
                var log = logs.FirstOrDefault(l => l.Id == updateModel.Id);
                if (log == null)
                {
                    continue;
                }

                var requestedUsageTime = updateModel.UsageTime;
                var isNewItemInCurrentPeriod = log.AcceptanceReportId == currentAcceptanceReportId
                    && (log.IssuedQuantity > 0 || log.TotalAmount > 0 || log.PendingValueStartPeriod == 0);

                if (!isNewItemInCurrentPeriod && Math.Abs(requestedUsageTime - log.AcceptanceReportItem.UsageTime) > 0.0001)
                {
                    throw new BadRequestException("Chỉ vật tư dài kỳ mới của kỳ hiện tại mới được cập nhật thời gian sử dụng");
                }

                // Check: Log này có thuộc kỳ hiện tại không?
                if (log.AcceptanceReportId == currentAcceptanceReportId)
                {
                    // Case 1: Log thuộc kỳ hiện tại → Update trực tiếp
                    if (isNewItemInCurrentPeriod)
                    {
                        log.AcceptanceReportItem.UpdateUsageTime(requestedUsageTime);
                        log.UpdateUsageTime(requestedUsageTime, updateModel.Note);
                    }

                    RefreshLogOutputMetrics(log, outputByProcessGroup, updateModel.Note);
                    log.UpdateAllocationRatio(updateModel.AllocationRatio, updateModel.IsFullAccounting, updateModel.Note);
                    _logRepository.Update(log);

                    await RecalculateFutureLogs(
                        log.AcceptanceReportItemId,
                        log.AcceptanceReportItemCategoryAllocationId,
                        log.PeriodStartMonth,
                        log.PendingValueEndPeriod,
                        cancellationToken);

                    results.Add(new UpdateAcceptanceReportItemLogResponseDto
                    {
                        Id = log.Id,
                        AllocationRatio = log.AllocationRatio,
                        ValueByStandard = log.ValueByStandard,
                        AccountedValueThisPeriod = log.AccountedValueThisPeriod,
                        PendingValueEndPeriod = log.PendingValueEndPeriod
                    });
                }
                else
                {
                    // Case 2: Log thuộc kỳ trước (TH2)
                    // Check xem đã có log override cho kỳ hiện tại chưa?
                    var existingOverrideLog = await _logRepository.GetFirstOrDefaultAsync(
                        predicate: l =>
                            l.AcceptanceReportItemId == log.AcceptanceReportItemId &&
                            l.AcceptanceReportItemCategoryAllocationId == log.AcceptanceReportItemCategoryAllocationId &&
                            l.AcceptanceReportId == currentAcceptanceReportId,
                        disableTracking: false);

                    if (existingOverrideLog != null)
                    {
                        // Đã có log override → Update log override đó
                        if (Math.Abs(requestedUsageTime - log.AcceptanceReportItem.UsageTime) > 0.0001)
                        {
                            throw new BadRequestException("Chỉ vật tư dài kỳ mới của kỳ hiện tại mới được cập nhật thời gian sử dụng");
                        }

                        RefreshLogOutputMetrics(existingOverrideLog, outputByProcessGroup, updateModel.Note);
                        existingOverrideLog.UpdateAllocationRatio(updateModel.AllocationRatio, updateModel.IsFullAccounting, updateModel.Note);
                        _logRepository.Update(existingOverrideLog);

                        await RecalculateFutureLogs(
                            existingOverrideLog.AcceptanceReportItemId,
                            existingOverrideLog.AcceptanceReportItemCategoryAllocationId,
                            existingOverrideLog.PeriodStartMonth,
                            existingOverrideLog.PendingValueEndPeriod,
                            cancellationToken);

                        results.Add(new UpdateAcceptanceReportItemLogResponseDto
                        {
                            Id = existingOverrideLog.Id,
                            AllocationRatio = existingOverrideLog.AllocationRatio,
                            ValueByStandard = existingOverrideLog.ValueByStandard,
                            AccountedValueThisPeriod = existingOverrideLog.AccountedValueThisPeriod,
                            PendingValueEndPeriod = existingOverrideLog.PendingValueEndPeriod
                        });
                    }
                    else
                    {
                        // Chưa có log override → Tạo log mới cho kỳ hiện tại
                        // Lấy tất cả logs trước đó của item này để tính AllocatedTime
                        var allPreviousLogs = await _logRepository.GetAllAsync(
                            predicate: l =>
                                l.AcceptanceReportItemId == log.AcceptanceReportItemId &&
                                l.AcceptanceReportItemCategoryAllocationId == log.AcceptanceReportItemCategoryAllocationId &&
                                l.PeriodEndMonth < productionOutput.StartMonth,
                            disableTracking: true);

                        var totalAllocatedTime = allPreviousLogs.Sum(l => l.AllocationRatio);
                        var pendingValueStart = log.PendingValueEndPeriod;
                        var usageTime = log.UsageTime;
                        var remainingTime = usageTime - totalAllocatedTime;

                        // Nếu RemainingTime = 0, set AllocationRatio = 1 (nếu user không override)
                        var finalAllocationRatio = Math.Abs(remainingTime) < 0.0001 && updateModel.AllocationRatio == 0
                            ? 1.0
                            : updateModel.AllocationRatio;

                        var actualOutput = productionOutput.ProductionMeters;
                        var plannedOutput = log.PlannedOutput;
                        var standardOutput = productionOutput.StandardProductionMeters;

                        var processGroupId = ResolveProcessGroupId(log);
                        if (processGroupId.HasValue
                            && outputByProcessGroup.TryGetValue(processGroupId.Value, out var metrics))
                        {
                            actualOutput = metrics.ActualOutput;
                            plannedOutput = metrics.PlannedOutput;
                            standardOutput = metrics.StandardOutput;
                        }

                        var newLog = AcceptanceReportItemLog.Create(
                            acceptanceReportItemId: log.AcceptanceReportItemId,
                            acceptanceReportId: currentAcceptanceReportId,
                            periodStartMonth: productionOutput.StartMonth,
                            periodEndMonth: productionOutput.EndMonth,
                            pendingValueStartPeriod: pendingValueStart,
                            issuedQuantity: 0,
                            unitPrice: 0,
                            usageTime: log.AcceptanceReportItem.UsageTime,
                            allocatedTime: totalAllocatedTime,
                            actualOutput: actualOutput,
                            plannedOutput: plannedOutput,
                            standardOutput: standardOutput,
                            allocationRatio: finalAllocationRatio,
                            acceptanceReportItemCategoryAllocationId: log.AcceptanceReportItemCategoryAllocationId,
                            isFullAccounting: updateModel.IsFullAccounting,
                            note: updateModel.Note);

                        await _logRepository.InsertAsync(newLog);

                        results.Add(new UpdateAcceptanceReportItemLogResponseDto
                        {
                            Id = newLog.Id,
                            AllocationRatio = newLog.AllocationRatio,
                            ValueByStandard = newLog.ValueByStandard,
                            AccountedValueThisPeriod = newLog.AccountedValueThisPeriod,
                            PendingValueEndPeriod = newLog.PendingValueEndPeriod
                        });
                    }
                }
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            return results;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
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

    private async Task RecalculateFutureLogs(
            Guid acceptanceReportItemId,
            Guid? acceptanceReportItemCategoryAllocationId,
            DateOnly fromPeriodStartMonth,
            decimal newPendingValueEnd,
            CancellationToken cancellationToken)
    {
        // Lấy tất cả logs tương lai, sắp xếp theo thời gian tăng dần
        var futureLogs = await _logRepository.GetAllAsync(
            predicate: l =>
                l.AcceptanceReportItemId == acceptanceReportItemId &&
                l.AcceptanceReportItemCategoryAllocationId == acceptanceReportItemCategoryAllocationId &&
                l.PeriodStartMonth > fromPeriodStartMonth,
            disableTracking: false);

        if (futureLogs == null || futureLogs.Count == 0)
        {
            return;
        }

        var orderedLogs = futureLogs.OrderBy(l => l.PeriodStartMonth).ToList();

        var currentPendingValue = newPendingValueEnd;

        foreach (var futureLog in orderedLogs)
        {
            futureLog.UpdatePendingValueStartPeriod(currentPendingValue);
            _logRepository.Update(futureLog);
            currentPendingValue = futureLog.PendingValueEndPeriod;
        }
    }

    private static Guid? ResolveProcessGroupId(AcceptanceReportItemLog log)
        => log.AcceptanceReportItemCategoryAllocation?.ProcessGroupId ?? log.AcceptanceReportItem?.ProcessGroupId;

    private static void RefreshLogOutputMetrics(
        AcceptanceReportItemLog log,
        Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> outputByProcessGroup,
        string note)
    {
        var processGroupId = ResolveProcessGroupId(log);
        if (!processGroupId.HasValue || !outputByProcessGroup.TryGetValue(processGroupId.Value, out var metrics))
        {
            return;
        }

        log.UpdateOutputMetrics(metrics.ActualOutput, metrics.PlannedOutput, metrics.StandardOutput, note);
    }
}
