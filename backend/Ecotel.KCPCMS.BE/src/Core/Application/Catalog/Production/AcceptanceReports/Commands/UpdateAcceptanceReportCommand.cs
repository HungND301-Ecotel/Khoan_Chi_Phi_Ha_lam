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

public record UpdateAcceptanceReportCommand(UpdateAcceptanceReportDto UpdateModel) : IRequest<UpdateAcceptanceReportResponseDto>;

public class UpdateAcceptanceReportCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAcceptanceReportCommand, UpdateAcceptanceReportResponseDto>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AcceptanceReportItem> _acceptanceReportItemRepository = unitOfWork.GetRepository<AcceptanceReportItem>();
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();
    private readonly IWriteRepository<MaintainUnitPriceEquipment> _maintainUnitPriceEquipmentRepository = unitOfWork.GetRepository<MaintainUnitPriceEquipment>();
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<UpdateAcceptanceReportResponseDto> Handle(UpdateAcceptanceReportCommand request, CancellationToken cancellationToken)
    {
        var updateModel = request.UpdateModel;

        // Validate AcceptanceReport exists
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == updateModel.Id,
            include: q => q.Include(a => a.ProductionOutput)
                .ThenInclude(p => p.ProductionOutputProcessGroups),
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

        var allParts = await _partRepository.GetAllAsync(disableTracking: true);
        var allMaintainUnitPriceEquipments = await _maintainUnitPriceEquipmentRepository.GetAllAsync(
            include: q => q.Include(m => m.MaintainUnitPrice),
            disableTracking: true);
        var fixedKeyLookup = await BuildFixedKeyLookupAsync(updateModel.Items, cancellationToken);

        // Get existing items for comparison (include QuotaBasedMaterialQuantities for EF tracking)
        var existingItems = await _acceptanceReportItemRepository.GetAllAsync(
            predicate: i => i.AcceptanceReportId == updateModel.Id,
            include: q => q.Include(i => i.IssuedDetails)
                           .Include(i => i.ShippedDetails)
                           .Include(i => i.QuotaBasedMaterialQuantities),
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
            foreach (var item in updateModel.Items)
            {
                itemsToUpdate[item.Id] = item;
            }

            // Delete items that are not in the update model
            var itemsToDelete = existingItems.Where(e => !itemsToUpdate.ContainsKey(e.Id)).ToList();
            if (itemsToDelete.Any())
            {
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
                    var categoryReference = ProductionReference.Create(
                        updateItem.CategoryProductionOrderId,
                        updateItem.CategoryEquipmentId);
                    var additionalCostReference = ProductionReference.Create(
                        updateItem.AdditionalCostProductionOrderId,
                        updateItem.AdditionalCostEquipmentId);
                    var materialsIncludedInContractRevenue = ResolveMaterialsIncludedInContractRevenue(updateItem, fixedKeyLookup);
                    var additionalCost = ResolveAdditionalCost(updateItem, fixedKeyLookup);
                    var otherMaterialDetail = ResolveOtherMaterialDetail(updateItem, additionalCost, fixedKeyLookup);
                    var quotaBasedMaterial = ResolveQuotaBasedMaterial(updateItem, fixedKeyLookup);
                    var quotaBasedMaterialType = ResolveQuotaBasedMaterialType(updateItem, quotaBasedMaterial, fixedKeyLookup);
                    var asset = ResolveAsset(updateItem, fixedKeyLookup);

                    Guid? resolvedMaintainUnitPriceEquipmentId = existingItem.MaintainUnitPriceEquipmentId;

                    if (existingItem.PartId.HasValue)
                    {
                        var part = allParts.FirstOrDefault(p => p.Id == existingItem.PartId.Value);
                        if (part?.Type == PartType.Part)
                        {
                            resolvedMaintainUnitPriceEquipmentId = ResolveMaintainUnitPriceEquipmentId(
                                existingItem.PartId.Value,
                                categoryReference.EquipmentId,
                                productionOutput.StartMonth,
                                productionOutput.EndMonth,
                                allMaintainUnitPriceEquipments);
                        }
                        else
                        {
                            resolvedMaintainUnitPriceEquipmentId = null;
                        }
                    }

                    existingItem.Update(
                        updateItem.ProcessGroupId,
                        existingItem.MaterialId,
                        existingItem.PartId,
                        resolvedMaintainUnitPriceEquipmentId,
                        updateItem.ItemType,
                        categoryReference,
                        additionalCostReference,
                        materialsIncludedInContractRevenue,
                        updateItem.MaterialsIncludedInContractRevenueQuantity,
                        additionalCost,
                        otherMaterialDetail,
                        updateItem.AdditionalCostQuantity,
                        quotaBasedMaterial,
                        quotaBasedMaterialType,
                        asset,
                        updateItem.AssetMaterialQuantity,
                        MapIssuedDetails(updateItem.IssuedDetails, fixedKeyLookup),
                        MapShippedDetails(updateItem.ShippedDetails, fixedKeyLookup),
                        MapQuotaBasedMaterialQuantities(updateItem.QuotaBasedMaterialQuantities, fixedKeyLookup),
                        updateItem.MaterialsIncludedInContractRevenueFixedKeyId,
                        updateItem.AdditionalCostFixedKeyId,
                        updateItem.OtherMaterialDetailFixedKeyId,
                        updateItem.QuotaBasedMaterialFixedKeyId,
                        updateItem.QuotaBasedMaterialTypeFixedKeyId,
                        updateItem.AssetFixedKeyId);

                    if (materialsIncludedInContractRevenue != Domain.Common.Enums.MaterialsIncludedInContractRevenue.None)
                    {
                        if (!updateItem.ProcessGroupId.HasValue || !processGroupIdsInPeriod.Contains(updateItem.ProcessGroupId.Value))
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

    private async Task<Dictionary<Guid, FixedKey>> BuildFixedKeyLookupAsync(
        IEnumerable<UpdateAcceptanceReportItemDto> items,
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

    private static MaterialsIncludedInContractRevenue ResolveMaterialsIncludedInContractRevenue(
        UpdateAcceptanceReportItemDto item,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => ResolveOptionalFixedKey(
            item.MaterialsIncludedInContractRevenueFixedKeyId,
            fixedKeys,
            nameof(item.MaterialsIncludedInContractRevenueFixedKeyId),
            FixedKeyCodeMapper.ResolveMaterialsIncludedInContractRevenue);

    private static AdditionalCost ResolveAdditionalCost(
        UpdateAcceptanceReportItemDto item,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => ResolveOptionalFixedKey(
            item.AdditionalCostFixedKeyId,
            fixedKeys,
            nameof(item.AdditionalCostFixedKeyId),
            FixedKeyCodeMapper.ResolveAdditionalCost);

    private static OtherMaterialDetail ResolveOtherMaterialDetail(
        UpdateAcceptanceReportItemDto item,
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
        UpdateAcceptanceReportItemDto item,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => ResolveOptionalFixedKey(
            item.QuotaBasedMaterialFixedKeyId,
            fixedKeys,
            nameof(item.QuotaBasedMaterialFixedKeyId),
            FixedKeyCodeMapper.ResolveQuotaBasedMaterial);

    private static QuotaBasedMaterialType? ResolveQuotaBasedMaterialType(
        UpdateAcceptanceReportItemDto item,
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
        UpdateAcceptanceReportItemDto item,
        IReadOnlyDictionary<Guid, FixedKey> fixedKeys)
        => ResolveOptionalFixedKey(
            item.AssetFixedKeyId,
            fixedKeys,
            nameof(item.AssetFixedKeyId),
            FixedKeyCodeMapper.ResolveAsset);

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
}

