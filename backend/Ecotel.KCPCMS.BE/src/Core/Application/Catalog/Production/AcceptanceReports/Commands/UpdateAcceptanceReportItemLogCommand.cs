// UpdateAcceptanceReportItemLogCommand.cs
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

public record UpdateAcceptanceReportItemLogCommand(UpdateAcceptanceReportItemLogDto UpdateModel) : IRequest<UpdateAcceptanceReportItemLogResponseDto>;

public class UpdateAcceptanceReportItemLogCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAcceptanceReportItemLogCommand, UpdateAcceptanceReportItemLogResponseDto>
{
    private readonly IWriteRepository<AcceptanceReportItemLog> _logRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<UpdateAcceptanceReportItemLogResponseDto> Handle(UpdateAcceptanceReportItemLogCommand request, CancellationToken cancellationToken)
    {
        var updateModel = request.UpdateModel;

        var log = await _logRepository.GetFirstOrDefaultAsync(
            predicate: l => l.Id == updateModel.Id,
            include: q => q
                .Include(l => l.AcceptanceReportItemCategoryAllocation)
                .Include(l => l.AcceptanceReportItem),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == updateModel.AcceptanceReportId,
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

        var outputByProcessGroup = AcceptanceReportItemLogCommandHelper.BuildOutputByProcessGroup(productionOutput);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var requestedUsageTime = updateModel.UsageTime;
            var isNewItemInCurrentPeriod = AcceptanceReportItemLogCommandHelper.IsNewItemInCurrentPeriod(
                log,
                updateModel.AcceptanceReportId);

            AcceptanceReportItemLogCommandHelper.EnsureUsageTimeCanBeUpdated(
                log,
                updateModel.AcceptanceReportId,
                requestedUsageTime);

            // Check: Log này có thuộc kỳ hiện tại không?
            if (log.AcceptanceReportId == updateModel.AcceptanceReportId)
            {
                // Case 1: Log thuộc kỳ hiện tại → Update trực tiếp
                if (isNewItemInCurrentPeriod)
                {
                    log.AcceptanceReportItem.UpdateUsageTime(requestedUsageTime);
                    log.UpdateUsageTime(requestedUsageTime, updateModel.Note);
                }
                else
                {
                    log.UpdateUsageTime(requestedUsageTime, updateModel.Note);
                }

                AcceptanceReportItemLogCommandHelper.RefreshLogOutputMetrics(log, outputByProcessGroup, updateModel.Note);
                log.UpdateAllocationRatio(updateModel.AllocationRatio, updateModel.IsFullAccounting, updateModel.Note);
                _logRepository.Update(log);

                await RecalculateFutureLogs(
                    log.AcceptanceReportItemId,
                    log.AcceptanceReportItemCategoryAllocationId,
                    log.PeriodStartMonth,
                    log.UsageTime,
                    log.PendingValueEndPeriod,
                    cancellationToken);

                await unitOfWork.SaveChangesAsync();
                await unitOfWork.CommitAsync(cancellationToken);

                return new UpdateAcceptanceReportItemLogResponseDto
                {
                    Id = log.Id,
                    AllocationRatio = log.AllocationRatio,
                    ValueByStandard = log.ValueByStandard,
                    AccountedValueThisPeriod = log.AccountedValueThisPeriod,
                    PendingValueEndPeriod = log.PendingValueEndPeriod
                };
            }
            else
            {
                // Case 2: Log thuộc kỳ trước (TH2)
                // Check xem đã có log override cho kỳ hiện tại chưa?
                var existingOverrideLog = await _logRepository.GetFirstOrDefaultAsync(
                    predicate: l =>
                        l.AcceptanceReportItemId == log.AcceptanceReportItemId &&
                        l.AcceptanceReportItemCategoryAllocationId == log.AcceptanceReportItemCategoryAllocationId &&
                        l.AcceptanceReportId == updateModel.AcceptanceReportId,
                    disableTracking: false);

                if (existingOverrideLog != null)
                {
                    // Đã có log override → Update log override đó
                    AcceptanceReportItemLogCommandHelper.EnsureUsageTimeCanBeUpdated(
                        log,
                        updateModel.AcceptanceReportId,
                        requestedUsageTime);

                    existingOverrideLog.UpdateUsageTime(requestedUsageTime, updateModel.Note);
                    AcceptanceReportItemLogCommandHelper.RefreshLogOutputMetrics(existingOverrideLog, outputByProcessGroup, updateModel.Note);
                    existingOverrideLog.UpdateAllocationRatio(updateModel.AllocationRatio, updateModel.IsFullAccounting, updateModel.Note);
                    _logRepository.Update(existingOverrideLog);

                    await RecalculateFutureLogs(
                        existingOverrideLog.AcceptanceReportItemId,
                        existingOverrideLog.AcceptanceReportItemCategoryAllocationId,
                        existingOverrideLog.PeriodStartMonth,
                        existingOverrideLog.UsageTime,
                        existingOverrideLog.PendingValueEndPeriod,
                        cancellationToken);


                    await unitOfWork.SaveChangesAsync();
                    await unitOfWork.CommitAsync(cancellationToken);

                    return new UpdateAcceptanceReportItemLogResponseDto
                    {
                        Id = existingOverrideLog.Id,
                        AllocationRatio = existingOverrideLog.AllocationRatio,
                        ValueByStandard = existingOverrideLog.ValueByStandard,
                        AccountedValueThisPeriod = existingOverrideLog.AccountedValueThisPeriod,
                        PendingValueEndPeriod = existingOverrideLog.PendingValueEndPeriod
                    };
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
                    var finalAllocationRatio = AcceptanceReportItemLogCommandHelper.ResolveFinalAllocationRatio(
                        log,
                        updateModel.AllocationRatio,
                        totalAllocatedTime);

                    var newLog = AcceptanceReportItemLogCommandHelper.CreateOverrideLog(
                        log,
                        updateModel.AcceptanceReportId,
                        productionOutput,
                        outputByProcessGroup,
                        requestedUsageTime,
                        finalAllocationRatio,
                        updateModel.IsFullAccounting,
                        updateModel.Note,
                        totalAllocatedTime);

                    await _logRepository.InsertAsync(newLog);

                    await RecalculateFutureLogs(
                        newLog.AcceptanceReportItemId,
                        newLog.AcceptanceReportItemCategoryAllocationId,
                        newLog.PeriodStartMonth,
                        newLog.UsageTime,
                        newLog.PendingValueEndPeriod,
                        cancellationToken);

                    await unitOfWork.SaveChangesAsync();
                    await unitOfWork.CommitAsync(cancellationToken);

                    return new UpdateAcceptanceReportItemLogResponseDto
                    {
                        Id = newLog.Id,
                        AllocationRatio = newLog.AllocationRatio,
                        ValueByStandard = newLog.ValueByStandard,
                        AccountedValueThisPeriod = newLog.AccountedValueThisPeriod,
                        PendingValueEndPeriod = newLog.PendingValueEndPeriod
                    };
                }
            }
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task RecalculateFutureLogs(
        Guid acceptanceReportItemId,
        Guid? acceptanceReportItemCategoryAllocationId,
        DateOnly fromPeriodStartMonth,
        double usageTime,
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
            futureLog.UpdateUsageTime(usageTime, futureLog.Note);
            futureLog.UpdatePendingValueStartPeriod(currentPendingValue);
            _logRepository.Update(futureLog);
            currentPendingValue = futureLog.PendingValueEndPeriod;
        }
    }

}
