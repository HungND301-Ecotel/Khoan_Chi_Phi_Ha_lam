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
    private readonly IWriteRepository<Material> _materialRepository = unitOfWork.GetRepository<Material>();

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
        var outputByProcessGroup = AcceptanceReportTrackingLogBuilder.BuildOutputByProcessGroup(productionOutput);
        var allMaterials = await _materialRepository.GetAllAsync(
            include: q => q.Include(m => m.Costs),
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

            var itemsToUpdate = new Dictionary<Guid, UpdateAcceptanceReportItemDto>();
            var itemsToCreate = new List<(int SortOrder, UpdateAcceptanceReportItemDto Item)>();
            var itemSortOrders = new Dictionary<Guid, int>();
            for (var itemIndex = 0; itemIndex < updateModel.Items.Count; itemIndex++)
            {
                var item = updateModel.Items[itemIndex];
                if (item.Id.HasValue && existingItems.Any(existingItem => existingItem.Id == item.Id.Value))
                {
                    itemsToUpdate[item.Id.Value] = item;
                    itemSortOrders[item.Id.Value] = itemIndex;
                }
                else
                {
                    itemsToCreate.Add((itemIndex, item));
                }
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

                    var materialsIncludedInContractRevenue = AcceptanceReportCommandItemHelper.ResolveMaterialsIncludedInContractRevenue(
                        updateItem.MaterialsIncludedInContractRevenueType,
                        updateItem.MaterialsIncludedInContractRevenue);
                    var additionalCost = AcceptanceReportCommandItemHelper.ResolveAdditionalCost(
                        updateItem.AdditionalCostClassification,
                        updateItem.AdditionalCost);
                    var trackedItemType = AcceptanceReportCommandItemHelper.ResolveTrackedItemType(
                        updateItem.Type,
                        updateItem.MaterialsIncludedInContractRevenueType,
                        materialsIncludedInContractRevenue,
                        additionalCost);
                    var categoryAssignmentCodeId =
                        updateItem.CategoryAssignmentCodeId ?? updateItem.CategoryEquipmentId;
                    var additionalCostAssignmentCodeId =
                        updateItem.AdditionalCostAssignmentCodeId ?? updateItem.AdditionalCostEquipmentId;
                    var categoryReference = ProductionReference.CreateForAssignmentCode(
                        updateItem.CategoryProductionOrderId,
                        categoryAssignmentCodeId);
                    var additionalCostReference = ProductionReference.CreateForAssignmentCode(
                        updateItem.AdditionalCostProductionOrderId,
                        additionalCostAssignmentCodeId);
                    var processGroupId = AcceptanceReportCommandItemHelper.IsTrackedSctxItem(trackedItemType)
                        ? updateItem.ProcessGroupId
                        : null;
                    var categoryAllocations = AcceptanceReportCommandItemHelper.MapCategoryAllocations(updateItem.CategoryAllocations);
                    var (materialId, partId) = AcceptanceReportCommandItemHelper.ResolveTrackedItemIds(
                        trackedItemType,
                        updateItem.TrackedMaterialId,
                        updateItem.MaterialId,
                        updateItem.PartId,
                        allMaterials);
                    var trackedMaterialId = materialId ?? partId;

                    existingItem.UpdateForTrackedMaterial(
                        itemSortOrders[existingItem.Id],
                        updateItem.DocumentNumber,
                        updateItem.PostingDate,
                        processGroupId,
                        trackedMaterialId,
                        trackedItemType,
                        updateItem.UsageTime,
                        updateItem.ItemType,
                        categoryReference,
                        additionalCostReference,
                        materialsIncludedInContractRevenue,
                        updateItem.IsLongTermTracking,
                        updateItem.MaterialsIncludedInContractRevenueQuantity,
                        additionalCost,
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

                    if (AcceptanceReportCommandItemHelper.ShouldValidateProcessGroup(
                        trackedItemType,
                        materialsIncludedInContractRevenue,
                        processGroupId,
                        categoryAllocations))
                    {
                        AcceptanceReportCommandItemHelper.ValidateProcessGroupIds(
                            processGroupId,
                            categoryAllocations,
                            processGroupIdsInPeriod);
                    }

                    _acceptanceReportItemRepository.Update(existingItem);
                }
            }

            if (itemsToUpdate.Any())
            {
                await unitOfWork.SaveChangesAsync();
            }

            var createdItems = new List<AcceptanceReportItem>();
            foreach (var (sortOrder, createItem) in itemsToCreate)
            {
                ValidateQuantityTotals(createItem);

                var materialsIncludedInContractRevenue = AcceptanceReportCommandItemHelper.ResolveMaterialsIncludedInContractRevenue(
                    createItem.MaterialsIncludedInContractRevenueType,
                    createItem.MaterialsIncludedInContractRevenue);
                var additionalCost = AcceptanceReportCommandItemHelper.ResolveAdditionalCost(
                    createItem.AdditionalCostClassification,
                    createItem.AdditionalCost);
                var trackedItemType = AcceptanceReportCommandItemHelper.ResolveTrackedItemType(
                    createItem.Type,
                    createItem.MaterialsIncludedInContractRevenueType,
                    materialsIncludedInContractRevenue,
                    additionalCost);
                var categoryAssignmentCodeId =
                    createItem.CategoryAssignmentCodeId ?? createItem.CategoryEquipmentId;
                var additionalCostAssignmentCodeId =
                    createItem.AdditionalCostAssignmentCodeId ?? createItem.AdditionalCostEquipmentId;
                var categoryReference = ProductionReference.CreateForAssignmentCode(
                    createItem.CategoryProductionOrderId,
                    categoryAssignmentCodeId);
                var additionalCostReference = ProductionReference.CreateForAssignmentCode(
                    createItem.AdditionalCostProductionOrderId,
                    additionalCostAssignmentCodeId);
                var processGroupId = AcceptanceReportCommandItemHelper.IsTrackedSctxItem(trackedItemType)
                    ? createItem.ProcessGroupId
                    : null;
                var categoryAllocations = AcceptanceReportCommandItemHelper.MapCategoryAllocations(createItem.CategoryAllocations);
                var (materialId, partId) = AcceptanceReportCommandItemHelper.ResolveTrackedItemIds(
                    trackedItemType,
                    createItem.TrackedMaterialId,
                    createItem.MaterialId,
                    createItem.PartId,
                    allMaterials);

                if (AcceptanceReportCommandItemHelper.ShouldValidateProcessGroup(
                    trackedItemType,
                    materialsIncludedInContractRevenue,
                    processGroupId,
                    categoryAllocations))
                {
                    AcceptanceReportCommandItemHelper.ValidateProcessGroupIds(processGroupId, categoryAllocations, processGroupIdsInPeriod);
                }

                var trackedMaterialId = materialId ?? partId;
                var reportItem = AcceptanceReportItem.CreateForTrackedMaterial(
                    acceptanceReport.Id,
                    sortOrder,
                    createItem.DocumentNumber,
                    createItem.PostingDate,
                    processGroupId,
                    trackedMaterialId,
                    trackedItemType,
                    createItem.UsageTime,
                    createItem.ItemType,
                    categoryReference,
                    additionalCostReference,
                    materialsIncludedInContractRevenue,
                    createItem.IsLongTermTracking,
                    createItem.MaterialsIncludedInContractRevenueQuantity,
                    additionalCost,
                    createItem.OtherMaterialDetail,
                    createItem.AdditionalCostQuantity,
                    createItem.QuotaBasedMaterial,
                    createItem.QuotaBasedMaterialType,
                    createItem.Asset,
                    createItem.AssetMaterialQuantity,
                    createItem.IssuedDetails.Select(x => (x.Type, x.Quantity)).ToList(),
                    createItem.ShippedDetails.Select(x => (x.Type, x.Quantity)).ToList(),
                    createItem.QuotaBasedMaterialQuantities?.Select(x => (x.Type, x.Quantity)).ToList(),
                    categoryAllocations);

                createdItems.Add(reportItem);
            }

            if (createdItems.Any())
            {
                await _acceptanceReportItemRepository.InsertAsync(createdItems, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }

            var updatedItemIds = existingItems
                .Where(i => itemsToUpdate.ContainsKey(i.Id))
                .Select(i => i.Id)
                .Distinct()
                .ToList();
            var processedItemIds = updatedItemIds
                .Concat(createdItems.Select(item => item.Id))
                .Distinct()
                .ToList();

            if (processedItemIds.Any())
            {
                var existingLogs = await _acceptanceReportItemLogRepository.GetAllAsync(
                    predicate: p => processedItemIds.Contains(p.AcceptanceReportItemId),
                    disableTracking: false);

                if (existingLogs.Any())
                {
                    _acceptanceReportItemLogRepository.Delete(existingLogs);
                    await unitOfWork.SaveChangesAsync();
                }
            }

            var logsToCreate = AcceptanceReportTrackingLogBuilder.BuildTrackingLogs(
                acceptanceReport.Id,
                existingItems.Where(i => itemsToUpdate.ContainsKey(i.Id)).Concat(createdItems),
                allMaterials,
                productionOutput,
                outputByProcessGroup);

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
                ItemCount = itemsToUpdate.Count + createdItems.Count
            };
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
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

}

