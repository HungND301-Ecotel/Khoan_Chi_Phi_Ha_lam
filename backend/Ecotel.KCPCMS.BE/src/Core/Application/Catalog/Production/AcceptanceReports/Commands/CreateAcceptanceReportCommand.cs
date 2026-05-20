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

        var outputByProcessGroup = AcceptanceReportTrackingLogBuilder.BuildOutputByProcessGroup(productionOutput);

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
                var categoryAssignmentCodeId = item.CategoryAssignmentCodeId ?? item.CategoryEquipmentId;
                var additionalCostAssignmentCodeId = item.AdditionalCostAssignmentCodeId ?? item.AdditionalCostEquipmentId;
                var categoryReference = ProductionReference.CreateForAssignmentCode(item.CategoryProductionOrderId, categoryAssignmentCodeId);
                var additionalCostReference = ProductionReference.CreateForAssignmentCode(item.AdditionalCostProductionOrderId, additionalCostAssignmentCodeId);
                var processGroupId = AcceptanceReportCommandItemHelper.IsTrackedSctxItem(item.Type)
                    ? item.ProcessGroupId
                    : null;
                var categoryAllocations = AcceptanceReportCommandItemHelper.MapCategoryAllocations(item.CategoryAllocations);
                var (materialId, partId) = AcceptanceReportCommandItemHelper.ResolveTrackedItemIds(
                    item.Type,
                    item.TrackedMaterialId,
                    null,
                    null,
                    allMaterials,
                    allParts);
                var trackedMaterialId = materialId ?? partId;

                if (item.AcceptanceReportItemId.HasValue)
                {
                    var existingItem = existingItemsByIds.FirstOrDefault(x => x.Id == item.AcceptanceReportItemId.Value)
                        ?? throw new NotFoundException($"AcceptanceReportItem with Id '{item.AcceptanceReportItemId.Value}' not found");

                    existingItem.UpdateForTrackedMaterial(
                        itemIndex,
                        processGroupId,
                        trackedMaterialId,
                        item.Type,
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
                    var reportItem = AcceptanceReportItem.CreateForTrackedMaterial(
                        acceptanceReport.Id,
                        itemIndex,
                        processGroupId,
                        trackedMaterialId,
                        item.Type,
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

                if (AcceptanceReportCommandItemHelper.RequiresProcessGroupValidation(item.Type, item.MaterialsIncludedInContractRevenue))
                {
                    AcceptanceReportCommandItemHelper.ValidateProcessGroupIds(processGroupId, categoryAllocations, processGroupIdsInPeriod);
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

            var allProcessedItems = itemsToCreate.Union(itemsToUpdate).ToList();
            var logsToCreate = AcceptanceReportTrackingLogBuilder.BuildTrackingLogs(
                acceptanceReport.Id,
                allProcessedItems,
                allParts,
                productionOutput,
                outputByProcessGroup);

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

    private static IList<(IssuedQuantityType Type, double Quantity)> MapIssuedDetails(List<IssuedDetailDto> dtos)
        => dtos.Select(x => (x.Type, x.Quantity)).ToList();

    private static IList<(ShippedQuantityType Type, double Quantity)> MapShippedDetails(List<ShippedDetailDto> dtos)
        => dtos.Select(x => (x.Type, x.Quantity)).ToList();

    private static IList<(QuotaBasedMaterialType Type, double Quantity)>? MapQuotaBasedMaterialQuantities(List<QuotaBasedMaterialQuantityDto>? dtos)
        => dtos?.Select(x => (x.Type, x.Quantity)).ToList();

}
