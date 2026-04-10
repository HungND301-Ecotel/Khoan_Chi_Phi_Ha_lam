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
        var allMaintainUnitPriceEquipments = await _maintainUnitPriceEquipmentRepository.GetAllAsync(
            include: q => q.Include(m => m.Part).ThenInclude(p => p.Costs),
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
                                   .Include(p => p.QuotaBasedMaterialQuantities),
                    disableTracking: false)).ToList();
            }

            var itemsToCreate = new List<AcceptanceReportItem>();
            var itemsToUpdate = new List<AcceptanceReportItem>();

            foreach (var item in createModel.Items)
            {
                Guid? materialId = null;
                Guid? maintainUnitPriceEquipmentId = null;

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
                    if (!item.MaintainUnitPriceEquipmentId.HasValue)
                    {
                        throw new NotFoundException("MaintainUnitPriceEquipmentId is required for SCTX item");
                    }

                    var maintainItem = allMaintainUnitPriceEquipments.FirstOrDefault(m => m.Id == item.MaintainUnitPriceEquipmentId.Value);
                    if (maintainItem == null)
                    {
                        throw new NotFoundException($"MaintainUnitPriceEquipment with Id '{item.MaintainUnitPriceEquipmentId.Value}' not found");
                    }

                    maintainUnitPriceEquipmentId = item.MaintainUnitPriceEquipmentId.Value;
                }

                var categoryReference = ProductionReference.Create(item.CategoryProductionOrderId, item.CategoryEquipmentId);
                var additionalCostReference = ProductionReference.Create(item.AdditionalCostProductionOrderId, item.AdditionalCostEquipmentId);

                if (item.AcceptanceReportItemId.HasValue)
                {
                    var existingItem = existingItemsByIds.FirstOrDefault(x => x.Id == item.AcceptanceReportItemId.Value)
                        ?? throw new NotFoundException($"AcceptanceReportItem with Id '{item.AcceptanceReportItemId.Value}' not found");

                    existingItem.Update(
                        item.ProcessGroupId,
                        materialId,
                        maintainUnitPriceEquipmentId,
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
                    var reportItem = AcceptanceReportItem.Create(
                        acceptanceReport.Id,
                        item.ProcessGroupId,
                        materialId,
                        maintainUnitPriceEquipmentId,
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

            var logsToCreate = new List<AcceptanceReportItemLog>();
            var allProcessedItems = itemsToCreate.Union(itemsToUpdate).ToList();

            foreach (var item in allProcessedItems)
            {
                if (item.MaintainUnitPriceEquipmentId.HasValue &&
                    item.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain &&
                    item.IssuedQuantity > 0)
                {
                    var maintainItem = allMaintainUnitPriceEquipments.FirstOrDefault(m => m.Id == item.MaintainUnitPriceEquipmentId.Value);
                    if (maintainItem == null)
                    {
                        continue;
                    }

                    var existingLog = await _acceptanceReportItemLogRepository.GetFirstOrDefaultAsync(
                        predicate: p => p.AcceptanceReportItemId == item.Id);

                    if (existingLog == null)
                    {
                        var cost = maintainItem.Part?.Costs?.FirstOrDefault(c =>
                            c.StartMonth <= productionOutput.StartMonth &&
                            c.EndMonth >= productionOutput.EndMonth);

                        var unitPrice = (decimal)(cost?.Amount ?? 0);
                        var usageTime = (double)maintainItem.ReplacementTimeStandard;

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
