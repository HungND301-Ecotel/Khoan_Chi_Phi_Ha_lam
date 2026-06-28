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
    private readonly IWriteRepository<LongTermAnchorSeed> _seedRepository = unitOfWork.GetRepository<LongTermAnchorSeed>();
    private readonly IWriteRepository<LongTermAnchorSeedItem> _seedItemRepository = unitOfWork.GetRepository<LongTermAnchorSeedItem>();
    private readonly IWriteRepository<LongTermAnchorSeedProcessGroupMetric> _processGroupMetricRepository = unitOfWork.GetRepository<LongTermAnchorSeedProcessGroupMetric>();

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

            var metricRequests = request.Request.ProcessGroupMetrics
                .GroupBy(x => x.ProcessGroupId)
                .Select(x => x.First())
                .ToList();
            var existingMetrics = await _processGroupMetricRepository.GetAllAsync(
                predicate: x => x.LongTermAnchorSeedId == seed.Id,
                disableTracking: false);

            foreach (var metricRequest in metricRequests)
            {
                var existingMetric = existingMetrics.FirstOrDefault(x => x.ProcessGroupId == metricRequest.ProcessGroupId);
                if (existingMetric == null)
                {
                    var newMetric = LongTermAnchorSeedProcessGroupMetric.Create(
                        seed.Id,
                        metricRequest.ProcessGroupId,
                        metricRequest.PlannedOutput,
                        metricRequest.StandardOutput);

                    await _processGroupMetricRepository.InsertAsync(newMetric, cancellationToken);
                    continue;
                }

                existingMetric.Update(metricRequest.PlannedOutput, metricRequest.StandardOutput);
                _processGroupMetricRepository.Update(existingMetric);
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
