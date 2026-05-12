using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Commands;

public record UpdateAcceptanceReportCommand(UpdateAcceptanceReportDto UpdateModel) : IRequest<UpdateAcceptanceReportResponseDto>;

public class UpdateAcceptanceReportCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAcceptanceReportCommand, UpdateAcceptanceReportResponseDto>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AcceptanceReportItem> _acceptanceReportItemRepository = unitOfWork.GetRepository<AcceptanceReportItem>();
    private readonly IWriteRepository<AcceptanceReportItemLog> _acceptanceReportItemLogRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();

    public async Task<UpdateAcceptanceReportResponseDto> Handle(UpdateAcceptanceReportCommand request, CancellationToken cancellationToken)
    {
        var updateModel = request.UpdateModel;

        // Validate AcceptanceReport exists
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == updateModel.Id,
            include: q => q.Include(a => a.ProductionOutput)
                    .ThenInclude(p => p.ProductUnitPriceProductionOutputs)
                        .ThenInclude(p => p.ProductUnitPrice)
                            .ThenInclude(p => p.Outputs)
                .Include(a => a.ProductionOutput)
                    .ThenInclude(p => p.ProductionOutputProcessGroups)
                        .ThenInclude(p => p.ProductionOutputProducts),
            disableTracking: false);

        if (acceptanceReport == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        // Validate items not empty
        if (updateModel.Items == null || !updateModel.Items.Any())
        {
            throw new BadRequestException(CustomResponseMessage.UpdateIdsEmpty);
        }

        // Get ProductionOutput for validation
        var productionOutput = acceptanceReport.ProductionOutput;
        if (productionOutput == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var processGroupIdsInPeriod = productionOutput.ProductionOutputProcessGroups
            .Select(x => x.ProcessGroupId)
            .ToHashSet();
        var outputByProcessGroup = BuildOutputByProcessGroup(productionOutput);
        var allParts = await _partRepository.GetAllAsync(
            include: q => q.Include(p => p.Costs),
            disableTracking: true);

        // Get existing items for comparison (include QuotaBasedMaterialQuantities for EF tracking)
        var existingItems = await _acceptanceReportItemRepository.GetAllAsync(
            predicate: i => i.AcceptanceReportId == updateModel.Id,
            include: q => q.Include(i => i.IssuedDetails)
                           .Include(i => i.ShippedDetails)
                           .Include(i => i.QuotaBasedMaterialQuantities)
                           .Include(i => i.CategoryAllocations)
                               .ThenInclude(i => i.Equipments),
            disableTracking: false);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            // Update AcceptanceReport
            acceptanceReport.Update(updateModel.FilePath);
            _acceptanceReportRepository.Update(acceptanceReport);
            await unitOfWork.SaveChangesAsync();

            // Group update model items by Id
            var itemsToUpdate = new Dictionary<Guid, UpdateAcceptanceReportItemDto>();
            var itemSortOrders = new Dictionary<Guid, int>();
            for (var itemIndex = 0; itemIndex < updateModel.Items.Count; itemIndex++)
            {
                var item = updateModel.Items[itemIndex];
                itemsToUpdate[item.Id] = item;
                itemSortOrders[item.Id] = itemIndex;
            }

            // Delete items that are not in the update model
            var itemsToDelete = existingItems.Where(e => !itemsToUpdate.ContainsKey(e.Id)).ToList();
            if (itemsToDelete.Any())
            {
                var deleteItemIds = itemsToDelete.Select(x => x.Id).ToList();
                var logsToDelete = await _acceptanceReportItemLogRepository.GetAllAsync(
                    predicate: p => deleteItemIds.Contains(p.AcceptanceReportItemId),
                    disableTracking: false);

                if (logsToDelete.Any())
                {
                    _acceptanceReportItemLogRepository.Delete(logsToDelete);
                }

                foreach (var item in itemsToDelete)
                {
                    _acceptanceReportItemRepository.Delete(item);
                }
                await unitOfWork.SaveChangesAsync();
            }

            // Update existing items
            foreach (var existingItem in existingItems)
            {
                if (itemsToUpdate.TryGetValue(existingItem.Id, out var updateItem))
                {
                    ValidateQuantityTotals(updateItem);

                    var categoryReference = ProductionReference.Create(
                        updateItem.CategoryProductionOrderId,
                        updateItem.CategoryEquipmentId);
                    var additionalCostReference = ProductionReference.Create(
                        updateItem.AdditionalCostProductionOrderId,
                        updateItem.AdditionalCostEquipmentId);
                    var processGroupId = existingItem.PartId.HasValue
                        ? updateItem.ProcessGroupId
                        : null;
                    var categoryAllocations = MapCategoryAllocations(updateItem.CategoryAllocations);

                    existingItem.Update(
                        itemSortOrders[existingItem.Id],
                        processGroupId,
                        existingItem.MaterialId,
                        existingItem.PartId,
                        updateItem.UsageTime,
                        updateItem.ItemType,
                        categoryReference,
                        additionalCostReference,
                        updateItem.MaterialsIncludedInContractRevenue,
                        updateItem.MaterialsIncludedInContractRevenueQuantity,
                        updateItem.AdditionalCost,
                        updateItem.OtherMaterialDetail,
                        updateItem.AdditionalCostQuantity,
                        updateItem.QuotaBasedMaterial,
                        updateItem.QuotaBasedMaterialType,
                        updateItem.Asset,
                        updateItem.AssetMaterialQuantity,
                        updateItem.IssuedDetails.Select(x => (x.Type, x.Quantity)).ToList(),
                        updateItem.ShippedDetails.Select(x => (x.Type, x.Quantity)).ToList(),
                        updateItem.QuotaBasedMaterialQuantities?.Select(x => (x.Type, x.Quantity)).ToList(),
                        categoryAllocations);

                    if (existingItem.PartId.HasValue &&
                        updateItem.MaterialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None)
                    {
                        var processGroupIdsToValidate = categoryAllocations != null && categoryAllocations.Any()
                            ? categoryAllocations.Select(x => x.ProcessGroupId)
                            : processGroupId.HasValue
                                ? new[] { processGroupId.Value }
                                : [];

                        if (!processGroupIdsToValidate.Any() || processGroupIdsToValidate.Any(id => !processGroupIdsInPeriod.Contains(id)))
                        {
                            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
                        }
                    }

                    _acceptanceReportItemRepository.Update(existingItem);
                }
            }

            if (itemsToUpdate.Any())
            {
                await unitOfWork.SaveChangesAsync();
            }

            var updatedItemIds = existingItems
                .Where(i => itemsToUpdate.ContainsKey(i.Id))
                .Select(i => i.Id)
                .Distinct()
                .ToList();

            if (updatedItemIds.Any())
            {
                var existingLogs = await _acceptanceReportItemLogRepository.GetAllAsync(
                    predicate: p => updatedItemIds.Contains(p.AcceptanceReportItemId),
                    disableTracking: false);

                if (existingLogs.Any())
                {
                    _acceptanceReportItemLogRepository.Delete(existingLogs);
                    await unitOfWork.SaveChangesAsync();
                }
            }

            var logsToCreate = new List<AcceptanceReportItemLog>();
            foreach (var existingItem in existingItems.Where(i => itemsToUpdate.ContainsKey(i.Id)))
            {
                var residualQuantity = existingItem.IssuedQuantity - existingItem.ShippedQuantity;
                if (!ShouldCreateLongTermTracking(existingItem, residualQuantity))
                {
                    continue;
                }

                var part = allParts.FirstOrDefault(p => p.Id == existingItem.PartId.Value);
                if (part == null)
                {
                    continue;
                }

                var cost = part.Costs?.FirstOrDefault(c =>
                    c.StartMonth <= productionOutput.StartMonth &&
                    c.EndMonth >= productionOutput.EndMonth);

                var unitPrice = (decimal)(cost?.Amount ?? 0);

                foreach (var trackingAllocation in BuildTrackingAllocations(existingItem, residualQuantity))
                {
                    var actualOutput = productionOutput.ProductionMeters;
                    var plannedOutput = 1.0;
                    var standardOutput = productionOutput.StandardProductionMeters;

                    if (trackingAllocation.ProcessGroupId.HasValue
                        && outputByProcessGroup.TryGetValue(trackingAllocation.ProcessGroupId.Value, out var metrics))
                    {
                        actualOutput = metrics.ActualOutput;
                        plannedOutput = metrics.PlannedOutput;
                        standardOutput = metrics.StandardOutput;
                    }

                    logsToCreate.Add(AcceptanceReportItemLog.Create(
                        acceptanceReportItemId: existingItem.Id,
                        acceptanceReportId: acceptanceReport.Id,
                        periodStartMonth: productionOutput.StartMonth,
                        periodEndMonth: productionOutput.EndMonth,
                        pendingValueStartPeriod: 0,
                        issuedQuantity: trackingAllocation.Quantity,
                        unitPrice: unitPrice,
                        usageTime: existingItem.UsageTime,
                        allocatedTime: 0,
                        actualOutput: actualOutput,
                        plannedOutput: plannedOutput,
                        standardOutput: standardOutput,
                        allocationRatio: 1.0,
                        acceptanceReportItemCategoryAllocationId: trackingAllocation.CategoryAllocationId));
                }
            }

            if (logsToCreate.Any())
            {
                await _acceptanceReportItemLogRepository.InsertAsync(logsToCreate, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new UpdateAcceptanceReportResponseDto
            {
                Id = acceptanceReport.Id,
                ProductionOutputId = acceptanceReport.ProductionOutputId,
                FilePath = acceptanceReport.FilePath,
                ItemCount = itemsToUpdate.Count
            };
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

    private static void ValidateQuantityTotals(UpdateAcceptanceReportItemDto updateItem)
    {
        var issuedDetailTotal = updateItem.IssuedDetails.Sum(x => x.Quantity);
        if (Math.Abs(issuedDetailTotal - updateItem.IssuedQuantity) >= 0.01)
        {
            throw new BadRequestException("Tổng số lượng lĩnh chi tiết phải bằng số lượng lĩnh.");
        }

        var shippedDetailTotal = updateItem.ShippedDetails.Sum(x => x.Quantity);
        if (Math.Abs(shippedDetailTotal - updateItem.ShippedQuantity) >= 0.01)
        {
            throw new BadRequestException("Tổng số lượng xuất chi tiết phải bằng số lượng xuất.");
        }
    }

    private static IList<(Guid ProcessGroupId, double Quantity, IList<Guid> EquipmentIds)>? MapCategoryAllocations(
        List<AcceptanceReportCategoryAllocationDto>? dtos)
        => dtos?.Select(x => (x.ProcessGroupId, x.Quantity, (IList<Guid>)x.EquipmentIds.ToList())).ToList();

    private static IList<(Guid? CategoryAllocationId, Guid? ProcessGroupId, double Quantity)> BuildTrackingAllocations(
        AcceptanceReportItem item,
        double residualQuantity)
    {
        if (residualQuantity <= 0)
        {
            return [];
        }

        if (item.CategoryAllocations.Any())
        {
            var totalAllocationQuantity = item.CategoryAllocations.Sum(x => x.Quantity);
            if (totalAllocationQuantity <= 0)
            {
                return
                [
                    (
                        CategoryAllocationId: (Guid?)null,
                        ProcessGroupId: item.ProcessGroupId,
                        Quantity: residualQuantity
                    )
                ];
            }

            return item.CategoryAllocations
                .Select(allocation => (
                    CategoryAllocationId: (Guid?)allocation.Id,
                    ProcessGroupId: (Guid?)allocation.ProcessGroupId,
                    Quantity: residualQuantity * allocation.Quantity / totalAllocationQuantity))
                .Where(x => x.Quantity > 0)
                .ToList();
        }

        return
        [
            (
                CategoryAllocationId: (Guid?)null,
                ProcessGroupId: item.ProcessGroupId,
                Quantity: residualQuantity
            )
        ];
    }

    private static bool ShouldCreateLongTermTracking(AcceptanceReportItem item, double residualQuantity)
        => item.PartId.HasValue
            && item.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain
            && residualQuantity > 0;
}

