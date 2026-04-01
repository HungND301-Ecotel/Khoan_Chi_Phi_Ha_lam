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

        // Validate ProductionOutput exists
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

        // Validate items not empty
        if (createModel.Items == null || !createModel.Items.Any())
        {
            throw new BadRequestException(CustomResponseMessage.UpdateIdsEmpty);
        }

        // Get all materials and parts for validation
        var allMaterials = await _materialRepository.GetAllAsync(disableTracking: true);
        var allParts = await _partRepository.GetAllAsync(
            include: q => q.Include(p => p.Costs),
            disableTracking: true);

        // Get existing AcceptanceReport for this ProductionOutput
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
            bool isNewReport = existingAcceptanceReport == null;

            if (isNewReport)
            {
                // Create new AcceptanceReport
                acceptanceReport = AcceptanceReport.Create(createModel.ProductionOutputId, createModel.FilePath);
                await _acceptanceReportRepository.InsertAsync(acceptanceReport, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Update existing AcceptanceReport
                acceptanceReport = existingAcceptanceReport;
            }

            // Get existing items for this acceptance report (for deletion check)
            var existingItemsInCurrentReport = await _acceptanceReportItemRepository.GetAllAsync(
                predicate: p => p.AcceptanceReportId == acceptanceReport.Id,
                disableTracking: false);

            // Get all items by IDs from request (may belong to other reports - from Excel export of previous period)
            var requestItemIds = createModel.Items
                .Where(x => x.AcceptanceReportItemId.HasValue)
                .Select(x => x.AcceptanceReportItemId.Value)
                .ToList();

            var existingItemsByIds = new List<AcceptanceReportItem>();
            if (requestItemIds.Any())
            {
                existingItemsByIds = (await _acceptanceReportItemRepository.GetAllAsync(
                    predicate: p => requestItemIds.Contains(p.Id),
                    include: p => p.Include(p => p.IssuedDetails)
                                   .Include(p => p.ShippedDetails)
                                   .Include(p => p.QuotaBasedMaterialQuantities),
                    disableTracking: false)).ToList();
            }

            var itemsToCreate = new List<AcceptanceReportItem>();
            var itemsToUpdate = new List<AcceptanceReportItem>();
            var itemsToDelete = new List<AcceptanceReportItem>();

            // Identify items to create and update
            foreach (var item in createModel.Items)
            {
                Guid? materialId = null;
                Guid? partId = null;

                // Check item type and validate based on MaterialOrPartId
                if (item.Type == AcceptanceReportItemType.Material)
                {
                    var material = allMaterials.FirstOrDefault(m => m.Id == item.MaterialOrPartId);
                    if (material == null)
                    {
                        throw new NotFoundException($"Material with Id '{item.MaterialOrPartId}' not found");
                    }

                    materialId = item.MaterialOrPartId;
                }
                else if (item.Type == AcceptanceReportItemType.Part)
                {
                    var part = allParts.FirstOrDefault(p => p.Id == item.MaterialOrPartId);
                    if (part == null)
                    {
                        throw new NotFoundException($"Part with Id '{item.MaterialOrPartId}' not found");
                    }

                    partId = item.MaterialOrPartId;
                }

                var categoryReference = ProductionReference.Create(
                    item.CategoryProductionOrderId,
                    item.CategoryEquipmentId);
                var additionalCostReference = ProductionReference.Create(
                    item.AdditionalCostProductionOrderId,
                    item.AdditionalCostEquipmentId);

                if (item.AcceptanceReportItemId.HasValue)
                {
                    // UPDATE: Item already exists (may belong to another report)
                    var existingItem = existingItemsByIds.FirstOrDefault(x => x.Id == item.AcceptanceReportItemId.Value);
                    if (existingItem == null)
                    {
                        throw new NotFoundException($"AcceptanceReportItem with Id '{item.AcceptanceReportItemId.Value}' not found");
                    }

                    existingItem.Update(
                        item.ProcessGroupId,
                        materialId,
                        partId,
                        item.ItemType,
                        categoryReference,
                        additionalCostReference,
                        item.MaterialsIncludedInContractRevenue,
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
                        MapQuotaBasedMaterialQuantities(item.QuotaBasedMaterialQuantities));

                    itemsToUpdate.Add(existingItem);
                }
                else
                {
                    // CREATE: New item for current report
                    var reportItem = AcceptanceReportItem.Create(
                        acceptanceReport.Id,
                        item.ProcessGroupId,
                        materialId,
                        partId,
                        item.ItemType,
                        categoryReference,
                        additionalCostReference,
                        item.MaterialsIncludedInContractRevenue,
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
                        MapQuotaBasedMaterialQuantities(item.QuotaBasedMaterialQuantities));

                    itemsToCreate.Add(reportItem);
                }

                if (item.MaterialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None)
                {
                    if (!item.ProcessGroupId.HasValue || !processGroupIdsInPeriod.Contains(item.ProcessGroupId.Value))
                    {
                        throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
                    }
                }
            }

            // Identify items to delete (only from current report, not from other reports)
            itemsToDelete.AddRange(existingItemsInCurrentReport.Where(x => !requestItemIds.Contains(x.Id)));

            // Execute delete operations
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

            // Execute create operations
            if (itemsToCreate.Any())
            {
                await _acceptanceReportItemRepository.InsertAsync(itemsToCreate, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }

            // Execute update operations
            if (itemsToUpdate.Any())
            {
                _acceptanceReportItemRepository.Update(itemsToUpdate);
                await unitOfWork.SaveChangesAsync();
            }

            // Create logs for long-term tracking items
            var logsToCreate = new List<AcceptanceReportItemLog>();
            var allProcessedItems = itemsToCreate.Union(itemsToUpdate).ToList();

            foreach (var item in allProcessedItems)
            {
                // Filter: Long-term items (PartId != null && MaterialsIncludedInContractRevenue == Maintain)
                if (item.PartId.HasValue &&
                    item.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain &&
                    item.IssuedQuantity > 0)
                {
                    var part = allParts.FirstOrDefault(p => p.Id == item.PartId);
                    if (part == null)
                    {
                        continue;
                    }

                    // Check if log already exists for this item
                    var existingLog = await _acceptanceReportItemLogRepository.GetFirstOrDefaultAsync(
                        predicate: p => p.AcceptanceReportItemId == item.Id);

                    if (existingLog == null)
                    {
                        var cost = part.Costs?.FirstOrDefault(c =>
                            c.StartMonth <= productionOutput.StartMonth &&
                            c.EndMonth >= productionOutput.EndMonth);

                        var unitPrice = (decimal)(cost?.Amount ?? 0);
                        var usageTime = (double)part.ReplacementTimeStandard;

                        var actualOutput = productionOutput.ProductionMeters;
                        var plannedOutput = 1.0;
                        var standardOutput = productionOutput.StandardProductionMeters;

                        if (item.ProcessGroupId.HasValue && outputByProcessGroup.TryGetValue(item.ProcessGroupId.Value, out var metrics))
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
                            issuedQuantity: item.IssuedQuantity,
                            unitPrice: unitPrice,
                            usageTime: usageTime,
                            allocatedTime: 0,
                            actualOutput: actualOutput,
                            plannedOutput: plannedOutput,
                            standardOutput: standardOutput,
                            allocationRatio: 1.0);

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

    private static IList<(IssuedQuantityType Type, double Quantity)> MapIssuedDetails(List<IssuedDetailDto> dtos)
        => dtos.Select(x => (x.Type, x.Quantity)).ToList();

    private static IList<(ShippedQuantityType Type, double Quantity)> MapShippedDetails(List<ShippedDetailDto> dtos)
        => dtos.Select(x => (x.Type, x.Quantity)).ToList();

    private static IList<(QuotaBasedMaterialType Type, double Quantity)>? MapQuotaBasedMaterialQuantities(List<QuotaBasedMaterialQuantityDto>? dtos)
        => dtos?.Select(x => (x.Type, x.Quantity)).ToList();
}
