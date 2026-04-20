using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.MasterData.FixedKeys;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.MasterData;
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
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();
    private readonly IWriteRepository<MaintainUnitPriceEquipment> _maintainUnitPriceEquipmentRepository = unitOfWork.GetRepository<MaintainUnitPriceEquipment>();
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

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
        var allMaintainUnitPriceEquipments = await _maintainUnitPriceEquipmentRepository.GetAllAsync(
            include: q => q.Include(m => m.Part).ThenInclude(p => p.Costs)
                           .Include(m => m.MaintainUnitPrice),
            disableTracking: true);
        var fixedKeyLookup = await BuildFixedKeyLookupAsync(createModel.Items, cancellationToken);

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
                var categoryReference = ProductionReference.Create(item.CategoryProductionOrderId, item.CategoryEquipmentId);
                var additionalCostReference = ProductionReference.Create(item.AdditionalCostProductionOrderId, item.AdditionalCostEquipmentId);
                var materialsIncludedInContractRevenue = ResolveMaterialsIncludedInContractRevenue(item, fixedKeyLookup);
                var additionalCost = ResolveAdditionalCost(item, fixedKeyLookup);
                var otherMaterialDetail = ResolveOtherMaterialDetail(item, additionalCost, fixedKeyLookup);
                var quotaBasedMaterial = ResolveQuotaBasedMaterial(item, fixedKeyLookup);
                var quotaBasedMaterialType = ResolveQuotaBasedMaterialType(item, quotaBasedMaterial, fixedKeyLookup);
                var asset = ResolveAsset(item, fixedKeyLookup);

                Guid? materialId = null;
                Guid? partId = null;
                Guid? maintainUnitPriceEquipmentId = null;
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
                    if (item.PartId.HasValue)
                    {
                        part = allParts.FirstOrDefault(p => p.Id == item.PartId.Value);
                        if (part == null)
                        {
                            throw new NotFoundException($"Part with Id '{item.PartId.Value}' not found");
                        }

                        partId = part.Id;
                    }
                    else if (item.MaintainUnitPriceEquipmentId.HasValue)
                    {
                        var maintainItem = allMaintainUnitPriceEquipments.FirstOrDefault(m => m.Id == item.MaintainUnitPriceEquipmentId.Value);
                        if (maintainItem == null)
                        {
                            throw new NotFoundException($"MaintainUnitPriceEquipment with Id '{item.MaintainUnitPriceEquipmentId.Value}' not found");
                        }

                        part = maintainItem.Part;
                        partId = maintainItem.PartId;
                        maintainUnitPriceEquipmentId = maintainItem.Id;
                    }
                    else
                    {
                        throw new NotFoundException("PartId is required for SCTX item");
                    }

                    if (part == null || !partId.HasValue)
                    {
                        throw new NotFoundException("Part not found for SCTX item");
                    }

                    if (part.Type == PartType.Part)
                    {
                        maintainUnitPriceEquipmentId = ResolveMaintainUnitPriceEquipmentId(
                            partId.Value,
                            categoryReference.EquipmentId,
                            productionOutput.StartMonth,
                            productionOutput.EndMonth,
                            allMaintainUnitPriceEquipments);
                    }
                    else
                    {
                        maintainUnitPriceEquipmentId = null;
                    }
                }

                if (item.AcceptanceReportItemId.HasValue)
                {
                    var existingItem = existingItemsByIds.FirstOrDefault(x => x.Id == item.AcceptanceReportItemId.Value)
                        ?? throw new NotFoundException($"AcceptanceReportItem with Id '{item.AcceptanceReportItemId.Value}' not found");

                    existingItem.Update(
                        item.ProcessGroupId,
                        materialId,
                        partId,
                        maintainUnitPriceEquipmentId,
                        item.ItemType,
                        categoryReference,
                        additionalCostReference,
                        materialsIncludedInContractRevenue,
                        item.MaterialsIncludedInContractRevenueQuantity,
                        additionalCost,
                        otherMaterialDetail,
                        item.AdditionalCostQuantity,
                        quotaBasedMaterial,
                        quotaBasedMaterialType,
                        asset,
                        item.AssetMaterialQuantity,
                        MapIssuedDetails(item.IssuedDetails, fixedKeyLookup),
                        MapShippedDetails(item.ShippedDetails, fixedKeyLookup),
                        MapQuotaBasedMaterialQuantities(item.QuotaBasedMaterialQuantities, fixedKeyLookup),
                        item.MaterialsIncludedInContractRevenueFixedKeyId,
                        item.AdditionalCostFixedKeyId,
                        item.OtherMaterialDetailFixedKeyId,
                        item.QuotaBasedMaterialFixedKeyId,
                        item.QuotaBasedMaterialTypeFixedKeyId,
                        item.AssetFixedKeyId);

                    itemsToUpdate.Add(existingItem);
                }
                else
                {
                    var reportItem = AcceptanceReportItem.Create(
                        acceptanceReport.Id,
                        item.ProcessGroupId,
                        materialId,
                        partId,
                        maintainUnitPriceEquipmentId,
                        item.ItemType,
                        categoryReference,
                        additionalCostReference,
                        materialsIncludedInContractRevenue,
                        item.MaterialsIncludedInContractRevenueQuantity,
                        additionalCost,
                        otherMaterialDetail,
                        item.AdditionalCostQuantity,
                        quotaBasedMaterial,
                        quotaBasedMaterialType,
                        asset,
                        item.AssetMaterialQuantity,
                        MapIssuedDetails(item.IssuedDetails, fixedKeyLookup),
                        MapShippedDetails(item.ShippedDetails, fixedKeyLookup),
                        MapQuotaBasedMaterialQuantities(item.QuotaBasedMaterialQuantities, fixedKeyLookup),
                        item.MaterialsIncludedInContractRevenueFixedKeyId,
                        item.AdditionalCostFixedKeyId,
                        item.OtherMaterialDetailFixedKeyId,
                        item.QuotaBasedMaterialFixedKeyId,
                        item.QuotaBasedMaterialTypeFixedKeyId,
                        item.AssetFixedKeyId);

                    itemsToCreate.Add(reportItem);
                }

                if (materialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None)
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

    private async Task<Dictionary<Guid, FixedKey>> BuildFixedKeyLookupAsync(
        IEnumerable<CreateAcceptanceReportItemDto> items,
        CancellationToken cancellationToken)
    {
        var ids = items
            .SelectMany(item => new Guid?[]
            {
                item.MaterialsIncludedInContractRevenueFixedKeyId,
                item.AdditionalCostFixedKeyId,
                item.OtherMaterialDetailFixedKeyId,
                item.QuotaBasedMaterialFixedKeyId,
                item.QuotaBasedMaterialTypeFixedKeyId,
                item.AssetFixedKeyId,
            }
            .Concat(item.IssuedDetails.Select(x => x.FixedKeyId))
            .Concat(item.ShippedDetails.Select(x => x.FixedKeyId))
            .Concat(item.QuotaBasedMaterialQuantities?.Select(x => x.FixedKeyId) ?? []))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (!ids.Any())
        {
            return new Dictionary<Guid, FixedKey>();
        }

        var fixedKeys = await _fixedKeyRepository.GetAllAsync(
            predicate: x => ids.Contains(x.Id),
            disableTracking: true);

        return fixedKeys.ToDictionary(x => x.Id);
    }

    private static IList<(IssuedQuantityType Type, Guid? FixedKeyId, double Quantity)> MapIssuedDetails(
        List<IssuedDetailDto> dtos,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => dtos.Select(x =>
        {
            var fixedKey = GetRequiredFixedKey(x.FixedKeyId, fixedKeys, nameof(x.FixedKeyId));
            var resolvedType = FixedKeyCodeMapper.ToIssuedQuantityType(fixedKey);
            return (Type: resolvedType, FixedKeyId: (Guid?)fixedKey.Id, Quantity: x.Quantity);
        }).ToList();

    private static IList<(ShippedQuantityType Type, Guid? FixedKeyId, double Quantity)> MapShippedDetails(
        List<ShippedDetailDto> dtos,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => dtos.Select(x =>
        {
            var fixedKey = GetRequiredFixedKey(x.FixedKeyId, fixedKeys, nameof(x.FixedKeyId));
            var resolvedType = FixedKeyCodeMapper.ToShippedQuantityType(fixedKey);
            return (Type: resolvedType, FixedKeyId: (Guid?)fixedKey.Id, Quantity: x.Quantity);
        }).ToList();

    private static IList<(QuotaBasedMaterialType Type, Guid? FixedKeyId, double Quantity)>? MapQuotaBasedMaterialQuantities(
        List<QuotaBasedMaterialQuantityDto>? dtos,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => dtos?.Select(x =>
        {
            var fixedKey = GetRequiredFixedKey(x.FixedKeyId, fixedKeys, nameof(x.FixedKeyId));
            var resolvedType = FixedKeyCodeMapper.ToQuotaBasedMaterialType(fixedKey);
            return (Type: resolvedType, FixedKeyId: (Guid?)fixedKey.Id, Quantity: x.Quantity);
        }).ToList();

    private static MaterialsIncludedInContractRevenue ResolveMaterialsIncludedInContractRevenue(
        CreateAcceptanceReportItemDto item,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => ResolveOptionalFixedKey(
            item.MaterialsIncludedInContractRevenueFixedKeyId,
            fixedKeys,
            nameof(item.MaterialsIncludedInContractRevenueFixedKeyId),
            FixedKeyCodeMapper.ResolveMaterialsIncludedInContractRevenue);

    private static AdditionalCost ResolveAdditionalCost(
        CreateAcceptanceReportItemDto item,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => ResolveOptionalFixedKey(
            item.AdditionalCostFixedKeyId,
            fixedKeys,
            nameof(item.AdditionalCostFixedKeyId),
            FixedKeyCodeMapper.ResolveAdditionalCost);

    private static OtherMaterialDetail ResolveOtherMaterialDetail(
        CreateAcceptanceReportItemDto item,
        AdditionalCost additionalCost,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
    {
        if (additionalCost != AdditionalCost.SafeAndWelfare)
        {
            return OtherMaterialDetail.None;
        }

        var fixedKey = GetRequiredFixedKey(
            item.OtherMaterialDetailFixedKeyId,
            fixedKeys,
            nameof(item.OtherMaterialDetailFixedKeyId));
        return FixedKeyCodeMapper.ToOtherMaterialDetail(fixedKey);
    }

    private static QuotaBasedMaterial ResolveQuotaBasedMaterial(
        CreateAcceptanceReportItemDto item,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => ResolveOptionalFixedKey(
            item.QuotaBasedMaterialFixedKeyId,
            fixedKeys,
            nameof(item.QuotaBasedMaterialFixedKeyId),
            FixedKeyCodeMapper.ResolveQuotaBasedMaterial);

    private static QuotaBasedMaterialType? ResolveQuotaBasedMaterialType(
        CreateAcceptanceReportItemDto item,
        QuotaBasedMaterial quotaBasedMaterial,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
    {
        if (quotaBasedMaterial == QuotaBasedMaterial.None)
        {
            if (item.QuotaBasedMaterialQuantities?.Any() == true)
            {
                throw new BadRequestException("Quota-based material quantities are not allowed when quota-based material is empty.");
            }

            return null;
        }

        if (item.QuotaBasedMaterialTypeFixedKeyId.HasValue &&
            fixedKeys.TryGetValue(item.QuotaBasedMaterialTypeFixedKeyId.Value, out var fixedKey))
        {
            return FixedKeyCodeMapper.ToQuotaBasedMaterialType(fixedKey);
        }

        var quantityFixedKeyId = item.QuotaBasedMaterialQuantities?.FirstOrDefault(x => x.FixedKeyId.HasValue)?.FixedKeyId;
        if (quantityFixedKeyId.HasValue && fixedKeys.TryGetValue(quantityFixedKeyId.Value, out var quantityFixedKey))
        {
            return FixedKeyCodeMapper.ToQuotaBasedMaterialType(quantityFixedKey);
        }

        throw new BadRequestException("Quota-based material type fixed key is required.");
    }

    private static Asset ResolveAsset(
        CreateAcceptanceReportItemDto item,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => ResolveOptionalFixedKey(
            item.AssetFixedKeyId,
            fixedKeys,
            nameof(item.AssetFixedKeyId),
            FixedKeyCodeMapper.ResolveAsset);

    private static TEnum ResolveOptionalFixedKey<TEnum>(
        Guid? fixedKeyId,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys,
        string fieldName,
        Func<FixedKey?, TEnum> resolver)
        where TEnum : struct, Enum
        => resolver(fixedKeyId.HasValue ? GetRequiredFixedKey(fixedKeyId, fixedKeys, fieldName) : null);

    private static FixedKey GetRequiredFixedKey(
        Guid? fixedKeyId,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys,
        string fieldName)
    {
        if (!fixedKeyId.HasValue)
        {
            throw new BadRequestException($"{fieldName} is required.");
        }

        if (!fixedKeys.TryGetValue(fixedKeyId.Value, out var fixedKey))
        {
            throw new NotFoundException($"Fixed key '{fixedKeyId.Value}' not found.");
        }

        return fixedKey;
    }

    private static Guid? ResolveMaintainUnitPriceEquipmentId(
        Guid partId,
        Guid? equipmentId,
        DateOnly startMonth,
        DateOnly endMonth,
        IEnumerable<MaintainUnitPriceEquipment> maintainItems)
    {
        if (!equipmentId.HasValue)
        {
            return null;
        }

        return maintainItems
            .Where(m => m.PartId == partId
                        && m.MaintainUnitPrice != null
                        && m.MaintainUnitPrice.EquipmentId == equipmentId.Value
                        && m.MaintainUnitPrice.StartMonth <= startMonth
                        && m.MaintainUnitPrice.EndMonth >= endMonth)
            .OrderByDescending(m => m.MaintainUnitPrice!.StartMonth)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefault();
    }
}
