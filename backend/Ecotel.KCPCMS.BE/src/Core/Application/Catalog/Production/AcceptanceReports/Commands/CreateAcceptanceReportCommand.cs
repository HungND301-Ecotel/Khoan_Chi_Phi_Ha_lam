using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
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
    private readonly IWriteRepository<MaintainUnitPriceEquipment> _maintainUnitPriceEquipmentRepository = unitOfWork.GetRepository<MaintainUnitPriceEquipment>();

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

        // Get all materials and maintain unit price equipment for validation
        var allMaterials = await _materialRepository.GetAllAsync(disableTracking: true);
        var allMaintainUnitPriceEquipments = await _maintainUnitPriceEquipmentRepository.GetAllAsync(
            include: q => q
                .Include(m => m.Part)
                    .ThenInclude(p => p.Costs)
                .Include(m => m.MaintainUnitPrice),
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
                    disableTracking: false)).ToList();
            }

            var itemsToCreate = new List<AcceptanceReportItem>();
            var itemsToUpdate = new List<AcceptanceReportItem>();
            var itemsToDelete = new List<AcceptanceReportItem>();

            // Identify items to create and update
            foreach (var item in createModel.Items)
            {
                Guid? materialId = null;
                Guid? maintainUnitPriceEquipmentId = null;

                // Check item type and validate based on MaterialOrPartId
                if (item.Type == AcceptanceReportItemType.Material)
                {
                    // MaterialOrPartId is MaterialId
                    var material = allMaterials.FirstOrDefault(m => m.Id == item.MaterialOrPartId);
                    if (material == null)
                    {
                        throw new NotFoundException($"Material with Id '{item.MaterialOrPartId}' not found");
                    }

                    materialId = item.MaterialOrPartId;
                }
                else if (item.Type == AcceptanceReportItemType.Part)
                {
                    // MaterialOrPartId is PartId, find MaintainUnitPriceEquipment with matching PartId and time range
                    var maintainEquipment = allMaintainUnitPriceEquipments.FirstOrDefault(m =>
                        m.PartId == item.MaterialOrPartId &&
                        m.MaintainUnitPrice != null &&
                        m.MaintainUnitPrice.StartMonth <= productionOutput.StartMonth &&
                        m.MaintainUnitPrice.EndMonth >= productionOutput.EndMonth);

                    if (maintainEquipment == null)
                    {
                        throw new NotFoundException($"MaintainUnitPriceEquipment not found for PartId '{item.MaterialOrPartId}' in the time range (Start: {productionOutput.StartMonth}, End: {productionOutput.EndMonth})");
                    }

                    maintainUnitPriceEquipmentId = maintainEquipment.Id;
                }

                if (item.AcceptanceReportItemId.HasValue)
                {
                    // UPDATE: Item already exists (may belong to another report)
                    var existingItem = existingItemsByIds.FirstOrDefault(x => x.Id == item.AcceptanceReportItemId.Value);
                    if (existingItem == null)
                    {
                        throw new NotFoundException($"AcceptanceReportItem with Id '{item.AcceptanceReportItemId.Value}' not found");
                    }

                    // Update item with all properties
                    existingItem.Update(
                        item.ProcessGroupId,
                        materialId,
                        maintainUnitPriceEquipmentId,
                        item.MaterialsIncludedInContractRevenue,
                        item.MaterialsIncludedInContractRevenueQuantity,
                        item.AdditionalCost,
                        item.AdditionalCostQuantity,
                        item.QuotaBasedMaterial,
                        item.QuotaBasedMaterialType,
                        item.QuotaBasedMaterialQuantity,
                        item.Asset,
                        item.AssetMaterialQuantity,
                        item.IssuedQuantity,
                        item.ShippedQuantity);
                    itemsToUpdate.Add(existingItem);
                }
                else
                {
                    // CREATE: New item for current report
                    var reportItem = AcceptanceReportItem.Create(
                        acceptanceReport.Id,
                        item.ProcessGroupId,
                        materialId,
                        maintainUnitPriceEquipmentId,
                        item.MaterialsIncludedInContractRevenue,
                        item.MaterialsIncludedInContractRevenueQuantity,
                        item.AdditionalCost,
                        item.AdditionalCostQuantity,
                        item.QuotaBasedMaterial,
                        item.QuotaBasedMaterialType,
                        item.QuotaBasedMaterialQuantity,
                        item.Asset,
                        item.AssetMaterialQuantity,
                        item.IssuedQuantity,
                        item.ShippedQuantity);

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

                // Also delete associated logs
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

            // Create/Update logs for long-term tracking items
            var logsToCreate = new List<AcceptanceReportItemLog>();
            var allProcessedItems = itemsToCreate.Union(itemsToUpdate).ToList();

            foreach (var item in allProcessedItems)
            {
                // Filter: Long-term items (MaintainUnitPriceEquipmentId != null && MaterialsIncludedInContractRevenue == Maintain)
                if (item.MaintainUnitPriceEquipmentId.HasValue &&
                    item.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain &&
                    item.IssuedQuantity > 0)
                {
                    var equipment = allMaintainUnitPriceEquipments.FirstOrDefault(e => e.Id == item.MaintainUnitPriceEquipmentId);
                    if (equipment?.Part == null)
                    {
                        continue;
                    }

                    // Check if log already exists for this item
                    var existingLog = await _acceptanceReportItemLogRepository.GetFirstOrDefaultAsync(
                        predicate: p => p.AcceptanceReportItemId == item.Id);

                    if (existingLog == null)
                    {
                        // Calculate unit price from Cost
                        var cost = equipment.Part.Costs?.FirstOrDefault(c =>
                            c.StartMonth <= productionOutput.StartMonth &&
                            c.EndMonth >= productionOutput.EndMonth);

                        var unitPrice = (decimal)(cost?.Amount ?? 0);
                        var usageTime = (double)equipment.ReplacementTimeStandard;

                        var actualOutput = productionOutput.ProductionMeters;
                        var plannedOutput = 1.0;
                        var standardOutput = productionOutput.StandardProductionMeters;

                        if (item.ProcessGroupId.HasValue && outputByProcessGroup.TryGetValue(item.ProcessGroupId.Value, out var metrics))
                        {
                            actualOutput = metrics.ActualOutput;
                            plannedOutput = metrics.PlannedOutput;
                            standardOutput = metrics.StandardOutput;
                        }

                        // Create log for new item
                        var log = AcceptanceReportItemLog.Create(
                            acceptanceReportItemId: item.Id,
                            acceptanceReportId: acceptanceReport.Id,
                            periodStartMonth: productionOutput.StartMonth,
                            periodEndMonth: productionOutput.EndMonth,
                            pendingValueStartPeriod: 0, // TH1: Start from 0
                            issuedQuantity: item.IssuedQuantity,
                            unitPrice: unitPrice,
                            usageTime: usageTime,
                            allocatedTime: 0, // TH1: No allocated time yet
                            actualOutput: actualOutput,
                            plannedOutput: plannedOutput,
                            standardOutput: standardOutput,
                            allocationRatio: 1.0); // Default, can be updated later

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
}
