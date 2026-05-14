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

public record CreateAcceptanceReportCommand(CreateAcceptanceReportDto CreateModel) : IRequest<CreateAcceptanceReportResponseDto>;

public class CreateAcceptanceReportCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateAcceptanceReportCommand, CreateAcceptanceReportResponseDto>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AcceptanceReportItem> _acceptanceReportItemRepository = unitOfWork.GetRepository<AcceptanceReportItem>();
    private readonly IWriteRepository<AcceptanceReportItemLog> _acceptanceReportItemLogRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    private readonly IWriteRepository<Material> _materialRepository = unitOfWork.GetRepository<Material>();
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();

    public async Task<CreateAcceptanceReportResponseDto> Handle(CreateAcceptanceReportCommand request, CancellationToken cancellationToken)
    {
        var createModel = request.CreateModel;

        var productionOutput = await _productionOutputRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == createModel.ProductionOutputId,
            include: p => p.Include(p => p.ProductUnitPriceProductionOutputs)
                .ThenInclude(p => p.ProductUnitPrice)
                .ThenInclude(p => p.Outputs)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(p => p.ProcessGroup)
                        .ThenInclude(p => p.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(p => p.ProductionOutputProducts),
            disableTracking: true);

        if (productionOutput == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (createModel.Items == null || !createModel.Items.Any())
        {
            throw new BadRequestException(CustomResponseMessage.UpdateIdsEmpty);
        }

        var allMaterials = await _materialRepository.GetAllAsync(disableTracking: true);
        var allParts = await _partRepository.GetAllAsync(
            include: q => q.Include(p => p.Costs),
            disableTracking: true);

        var existingAcceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: p => p.ProductionOutputId == createModel.ProductionOutputId,
            disableTracking: false);

        var processGroupIdsInPeriod = productionOutput.ProductionOutputProcessGroups
            .Select(x => x.ProcessGroupId)
            .ToHashSet();

        var outputByProcessGroup = BuildOutputByProcessGroup(productionOutput);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            AcceptanceReport acceptanceReport;
            if (existingAcceptanceReport == null)
            {
                acceptanceReport = AcceptanceReport.Create(createModel.ProductionOutputId, createModel.FilePath);
                await _acceptanceReportRepository.InsertAsync(acceptanceReport, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }
            else
            {
                acceptanceReport = existingAcceptanceReport;
            }

            var existingItemsInCurrentReport = await _acceptanceReportItemRepository.GetAllAsync(
                predicate: p => p.AcceptanceReportId == acceptanceReport.Id,
                disableTracking: false);

            var requestItemIds = createModel.Items
                .Where(x => x.AcceptanceReportItemId.HasValue)
                .Select(x => x.AcceptanceReportItemId!.Value)
                .ToList();

            var existingItemsByIds = new List<AcceptanceReportItem>();
            if (requestItemIds.Any())
            {
                existingItemsByIds = (await _acceptanceReportItemRepository.GetAllAsync(
                    predicate: p => requestItemIds.Contains(p.Id),
                    include: p => p.Include(p => p.IssuedDetails)
                                   .Include(p => p.ShippedDetails)
                                   .Include(p => p.QuotaBasedMaterialQuantities)
                                   .Include(p => p.CategoryAllocations)
                                       .ThenInclude(p => p.Equipments),
                    disableTracking: false)).ToList();
            }

            var itemsToCreate = new List<AcceptanceReportItem>();
            var itemsToUpdate = new List<AcceptanceReportItem>();

            for (var itemIndex = 0; itemIndex < createModel.Items.Count; itemIndex++)
            {
                var item = createModel.Items[itemIndex];
                var categoryReference = ProductionReference.Create(item.CategoryProductionOrderId, item.CategoryEquipmentId);
                var additionalCostReference = ProductionReference.Create(item.AdditionalCostProductionOrderId, item.AdditionalCostEquipmentId);
                var processGroupId = item.Type == AcceptanceReportItemType.Part
                    ? item.ProcessGroupId
                    : null;
                var categoryAllocations = MapCategoryAllocations(item.CategoryAllocations);

                Guid? materialId = null;
                Guid? partId = null;
                Part? part = null;

                if (item.Type == AcceptanceReportItemType.Material)
                {
                    if (!item.MaterialId.HasValue)
                    {
                        throw new NotFoundException("MaterialId is required for material item");
                    }

                    var material = allMaterials.FirstOrDefault(m => m.Id == item.MaterialId.Value);
                    if (material == null)
                    {
                        throw new NotFoundException($"Material with Id '{item.MaterialId.Value}' not found");
                    }

                    materialId = item.MaterialId.Value;
                }
                else if (item.Type == AcceptanceReportItemType.Part)
                {
                    if (!item.PartId.HasValue)
                    {
                        throw new NotFoundException("PartId is required for SCTX item");
                    }

                    part = allParts.FirstOrDefault(p => p.Id == item.PartId.Value);
                    if (part == null)
                    {
                        throw new NotFoundException($"Part with Id '{item.PartId.Value}' not found");
                    }

                    partId = part.Id;
                }

                if (item.AcceptanceReportItemId.HasValue)
                {
                    var existingItem = existingItemsByIds.FirstOrDefault(x => x.Id == item.AcceptanceReportItemId.Value)
                        ?? throw new NotFoundException($"AcceptanceReportItem with Id '{item.AcceptanceReportItemId.Value}' not found");

                    existingItem.Update(
                        itemIndex,
                        processGroupId,
                        materialId,
                        partId,
                        item.UsageTime,
                        item.ItemType,
                        categoryReference,
                        additionalCostReference,
                        item.MaterialsIncludedInContractRevenue,
                        item.IsLongTermTracking,
                        item.MaterialsIncludedInContractRevenueQuantity,
                        item.AdditionalCost,
                        item.OtherMaterialDetail,
                        item.AdditionalCostQuantity,
                        item.QuotaBasedMaterial,
                        item.QuotaBasedMaterialType,
                        item.Asset,
                        item.AssetMaterialQuantity,
                        MapIssuedDetails(item.IssuedDetails),
                        MapShippedDetails(item.ShippedDetails),
                        MapQuotaBasedMaterialQuantities(item.QuotaBasedMaterialQuantities),
                        categoryAllocations);

                    itemsToUpdate.Add(existingItem);
                }
                else
                {
                    var reportItem = AcceptanceReportItem.Create(
                        acceptanceReport.Id,
                        itemIndex,
                        processGroupId,
                        materialId,
                        partId,
                        item.UsageTime,
                        item.ItemType,
                        categoryReference,
                        additionalCostReference,
                        item.MaterialsIncludedInContractRevenue,
                        item.IsLongTermTracking,
                        item.MaterialsIncludedInContractRevenueQuantity,
                        item.AdditionalCost,
                        item.OtherMaterialDetail,
                        item.AdditionalCostQuantity,
                        item.QuotaBasedMaterial,
                        item.QuotaBasedMaterialType,
                        item.Asset,
                        item.AssetMaterialQuantity,
                        MapIssuedDetails(item.IssuedDetails),
                        MapShippedDetails(item.ShippedDetails),
                        MapQuotaBasedMaterialQuantities(item.QuotaBasedMaterialQuantities),
                        categoryAllocations);

                    itemsToCreate.Add(reportItem);
                }

                if (item.Type == AcceptanceReportItemType.Part &&
                    item.MaterialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None)
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
            }

            var itemsToDelete = existingItemsInCurrentReport.Where(x => !requestItemIds.Contains(x.Id)).ToList();

            if (itemsToDelete.Any())
            {
                _acceptanceReportItemRepository.Delete(itemsToDelete);

                var logIdsToDelete = itemsToDelete.Select(x => x.Id).ToList();
                var logsToDelete = await _acceptanceReportItemLogRepository.GetAllAsync(
                    predicate: p => logIdsToDelete.Contains(p.AcceptanceReportItemId));
                if (logsToDelete.Any())
                {
                    _acceptanceReportItemLogRepository.Delete(logsToDelete);
                }

                await unitOfWork.SaveChangesAsync();
            }

            if (itemsToCreate.Any())
            {
                await _acceptanceReportItemRepository.InsertAsync(itemsToCreate, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }

            if (itemsToUpdate.Any())
            {
                _acceptanceReportItemRepository.Update(itemsToUpdate);
                await unitOfWork.SaveChangesAsync();
            }

            var processedItemIds = itemsToCreate.Select(x => x.Id)
                .Concat(itemsToUpdate.Select(x => x.Id))
                .Distinct()
                .ToList();

            if (processedItemIds.Any())
            {
                var existingLogs = await _acceptanceReportItemLogRepository.GetAllAsync(
                    predicate: p => processedItemIds.Contains(p.AcceptanceReportItemId));

                if (existingLogs.Any())
                {
                    _acceptanceReportItemLogRepository.Delete(existingLogs);
                    await unitOfWork.SaveChangesAsync();
                }
            }

            var logsToCreate = new List<AcceptanceReportItemLog>();
            var allProcessedItems = itemsToCreate.Union(itemsToUpdate).ToList();

            foreach (var item in allProcessedItems)
            {
                var residualQuantity = item.IssuedQuantity - item.ShippedQuantity;

                if (ShouldCreateLongTermTracking(item, residualQuantity))
                {
                    var part = allParts.FirstOrDefault(p => p.Id == item.PartId.Value);
                    if (part == null)
                    {
                        continue;
                    }

                    var cost = part.Costs?.FirstOrDefault(c =>
                        c.StartMonth <= productionOutput.StartMonth &&
                        c.EndMonth >= productionOutput.EndMonth);

                    var unitPrice = (decimal)(cost?.Amount ?? 0);
                    var usageTime = item.UsageTime;

                    foreach (var trackingAllocation in BuildTrackingAllocations(item, residualQuantity))
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

                        var log = AcceptanceReportItemLog.Create(
                            acceptanceReportItemId: item.Id,
                            acceptanceReportId: acceptanceReport.Id,
                            periodStartMonth: productionOutput.StartMonth,
                            periodEndMonth: productionOutput.EndMonth,
                            pendingValueStartPeriod: 0,
                            issuedQuantity: trackingAllocation.Quantity,
                            unitPrice: unitPrice,
                            usageTime: usageTime,
                            allocatedTime: 0,
                            actualOutput: actualOutput,
                            plannedOutput: plannedOutput,
                            standardOutput: standardOutput,
                            allocationRatio: 1.0,
                            acceptanceReportItemCategoryAllocationId: trackingAllocation.CategoryAllocationId);

                        logsToCreate.Add(log);
                    }
                }
            }

            if (logsToCreate.Any())
            {
                await _acceptanceReportItemLogRepository.InsertAsync(logsToCreate, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new CreateAcceptanceReportResponseDto
            {
                Id = acceptanceReport.Id,
                ProductionOutputId = acceptanceReport.ProductionOutputId,
                FilePath = acceptanceReport.FilePath,
                ItemCount = itemsToCreate.Count + itemsToUpdate.Count
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

    private static IList<(IssuedQuantityType Type, double Quantity)> MapIssuedDetails(List<IssuedDetailDto> dtos)
        => dtos.Select(x => (x.Type, x.Quantity)).ToList();

    private static IList<(ShippedQuantityType Type, double Quantity)> MapShippedDetails(List<ShippedDetailDto> dtos)
        => dtos.Select(x => (x.Type, x.Quantity)).ToList();

    private static IList<(QuotaBasedMaterialType Type, double Quantity)>? MapQuotaBasedMaterialQuantities(List<QuotaBasedMaterialQuantityDto>? dtos)
        => dtos?.Select(x => (x.Type, x.Quantity)).ToList();

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
            && item.IsLongTermTracking
            && residualQuantity > 0;

}
