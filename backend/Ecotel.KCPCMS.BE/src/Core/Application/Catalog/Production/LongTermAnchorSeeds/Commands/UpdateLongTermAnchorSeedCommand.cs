using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.LongTermAnchorSeeds.Commands;

public record UpdateLongTermAnchorSeedCommand(UpdateLongTermAnchorSeedRequestDto Request) : IRequest<bool>;

public class UpdateLongTermAnchorSeedCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLongTermAnchorSeedCommand, bool>
{
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<ProductionOrder>();
    private readonly IWriteRepository<LongTermAnchorSeed> _seedRepository = unitOfWork.GetRepository<LongTermAnchorSeed>();
    private readonly IWriteRepository<LongTermAnchorSeedItem> _seedItemRepository = unitOfWork.GetRepository<LongTermAnchorSeedItem>();
    private readonly IWriteRepository<LongTermAnchorSeedItemLog> _seedItemLogRepository = unitOfWork.GetRepository<LongTermAnchorSeedItemLog>();
    private readonly IWriteRepository<LongTermAnchorSeedProcessGroupMetric> _processGroupMetricRepository = unitOfWork.GetRepository<LongTermAnchorSeedProcessGroupMetric>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<bool> Handle(UpdateLongTermAnchorSeedCommand request, CancellationToken cancellationToken)
    {
        if (request.Request.Items == null || request.Request.Items.Count == 0)
        {
            throw new BadRequestException("Danh sách mốc gốc không được để trống");
        }

        if (request.Request.DepartmentId == Guid.Empty)
        {
            throw new BadRequestException("Đơn vị không hợp lệ");
        }

        var departmentId = request.Request.DepartmentId;
        var departmentExists = await _departmentRepository.ExistsAsync(x => x.Id == departmentId);
        if (!departmentExists)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var assignmentCodeIds = request.Request.Items
            .Select(x => x.CategoryAssignmentCodeId ?? x.CategoryEquipmentId)
            .Where(x => x.HasValue && x.Value != Guid.Empty)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var productionOrderIds = request.Request.Items
            .Select(x => x.CategoryProductionOrderId)
            .Where(x => x.HasValue && x.Value != Guid.Empty)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (assignmentCodeIds.Count > 0)
        {
            var existingAssignmentCodeIds = await _assignmentCodeRepository.GetAllAsync(
                predicate: x => assignmentCodeIds.Contains(x.Id),
                disableTracking: true);
            if (existingAssignmentCodeIds.Count != assignmentCodeIds.Count)
            {
                throw new NotFoundException("Nhóm vật tư, tài sản không tồn tại");
            }
        }

        if (productionOrderIds.Count > 0)
        {
            var existingProductionOrderIds = await _productionOrderRepository.GetAllAsync(
                predicate: x => productionOrderIds.Contains(x.Id),
                disableTracking: true);
            if (existingProductionOrderIds.Count != productionOrderIds.Count)
            {
                throw new NotFoundException("Lệnh sản xuất không tồn tại");
            }
        }

        var seed = await _seedRepository.GetFirstOrDefaultAsync(
            predicate: s => s.DepartmentId == departmentId,
            include: q => q
                .Include(s => s.Items)
                .Include(s => s.ProcessGroupMetrics),
            disableTracking: false);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (seed == null)
            {
                seed = LongTermAnchorSeed.Create(departmentId);
                await _seedRepository.InsertAsync(seed, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }

            var itemIds = request.Request.Items.Select(x => x.Id).ToList();
            var existingItems = await _seedItemRepository.GetAllAsync(
                predicate: i => itemIds.Contains(i.Id),
                disableTracking: false);

            if (existingItems.Count != request.Request.Items.Count)
            {
                throw new NotFoundException(CustomResponseMessage.EntityNotFound);
            }

            foreach (var (item, index) in request.Request.Items.Select((item, index) => (item, index)))
            {
                var entity = existingItems.First(x => x.Id == item.Id);
                if (entity.LongTermAnchorSeedId != seed.Id)
                {
                    throw new BadRequestException("Dòng mốc gốc không thuộc đơn vị hiện tại");
                }

                var trackedMaterialId = item.TrackedMaterialId ?? item.MaterialId ?? item.PartId;
                if (!trackedMaterialId.HasValue || trackedMaterialId.Value == Guid.Empty)
                {
                    throw new BadRequestException("Vật tư không hợp lệ");
                }

                entity.UpdateForMaterial(
                    item.ProcessGroupId,
                    trackedMaterialId.Value,
                    item.CategoryAssignmentCodeId ?? item.CategoryEquipmentId,
                    item.CategoryProductionOrderId,
                    index,
                    item.IssuedQuantity,
                    item.UnitPrice,
                    item.PendingValueStartPeriod,
                    item.UsageTime,
                    item.AllocatedTime,
                    entity.AllocationRatio,
                    item.Note);

                _seedItemRepository.Update(entity);
            }

            var existingMetrics = await _processGroupMetricRepository.GetAllAsync(
                predicate: x => x.LongTermAnchorSeedId == seed.Id,
                disableTracking: false);
            if (existingMetrics.Count > 0)
            {
                _processGroupMetricRepository.Delete(existingMetrics);
            }

            if (request.Request.ProcessGroupMetrics.Count > 0)
            {
                var metricsToInsert = request.Request.ProcessGroupMetrics
                    .Select(metric => LongTermAnchorSeedProcessGroupMetric.Create(
                        seed.Id,
                        metric.ProcessGroupId,
                        metric.PlannedOutput,
                        metric.StandardOutput))
                    .ToList();

                foreach (var metric in metricsToInsert)
                {
                    await _processGroupMetricRepository.InsertAsync(metric, cancellationToken);
                }
            }

            await unitOfWork.SaveChangesAsync();

            var refreshedSeed = await _seedRepository.GetFirstOrDefaultAsync(
                predicate: s => s.Id == seed.Id,
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
                disableTracking: true)
                ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            var acceptanceReports = await _acceptanceReportRepository.GetAllAsync(
                predicate: a => a.ProductionOutput.DepartmentId == departmentId,
                include: q => q
                    .Include(a => a.ProductionOutput)
                        .ThenInclude(p => p.ProductionOutputProcessGroups)
                            .ThenInclude(pg => pg.ProductionOutputProducts),
                disableTracking: true);

            var existingLogs = await _seedItemLogRepository.GetAllAsync(
                predicate: x => x.LongTermAnchorSeedItem.LongTermAnchorSeedId == seed.Id,
                disableTracking: false);
            if (existingLogs.Count > 0)
            {
                _seedItemLogRepository.Delete(existingLogs);
            }

            var rebuiltLogs = LongTermAnchorSeedLogPersistenceHelper.BuildLogs(
                refreshedSeed,
                acceptanceReports);
            foreach (var log in rebuiltLogs)
            {
                await _seedItemLogRepository.InsertAsync(log, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
