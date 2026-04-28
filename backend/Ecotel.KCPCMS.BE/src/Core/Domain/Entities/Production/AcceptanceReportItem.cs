using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Production;

public class AcceptanceReportItem : AuditableEntity<Guid>
{
    public Guid AcceptanceReportId { get; protected set; }
    public Guid? ProcessGroupId { get; protected set; }
    public Guid? PartId { get; protected set; }
    public Guid? EquipmentId { get; protected set; }
    public Guid? MaterialId { get; protected set; }
    public double UsageTime { get; protected set; }

    public ItemType ItemType { get; protected set; }
    public Guid? ProductionOrderId { get; protected set; }
    public Guid? AdditionalCostProductionOrderId { get; protected set; }
    public Guid? AdditionalCostEquipmentId { get; protected set; }

    public double IssuedQuantity => _issuedDetails.Sum(x => x.Quantity);   // tự tính tổng

    public double ShippedQuantity => _shippedDetails.Sum(x => x.Quantity);  // tự tính tổng

    //Vật tư tính vào doanh thu khoán
    public MaterialsIncludedInContractRevenue MaterialsIncludedInContractRevenue { get; protected set; }
    public double MaterialsIncludedInContractRevenueQuantity { get; protected set; }

    //Bổ sung chi phí
    public AdditionalCost AdditionalCost { get; protected set; }
    public OtherMaterialDetail OtherMaterialDetail { get; protected set; }
    public double AdditionalCostQuantity { get; protected set; }

    //Vật tư theo hạn mức
    public QuotaBasedMaterial QuotaBasedMaterial { get; protected set; }
    public QuotaBasedMaterialType QuotaBasedMaterialType { get; protected set; }
    public double QuotaBasedMaterialQuantity => _quotaBasedMaterialQuantities.Sum(x => x.Quantity);

    //Tài sản
    public Asset Asset { get; protected set; }
    public double AssetMaterialQuantity { get; protected set; }

    // Navigation properties
    public virtual AcceptanceReport AcceptanceReport { get; protected set; }
    public virtual ProcessGroup? ProcessGroup { get; protected set; }
    public virtual Part? Part { get; protected set; }
    public virtual Equipment? Equipment { get; protected set; }
    public virtual Material? Material { get; protected set; }
    public virtual ProductionOrder ProductionOrder { get; protected set; }

    public ProductionReference CategoryProductionReference
        => ProductionReference.Create(ProductionOrderId, EquipmentId);

    public ProductionReference AdditionalCostProductionReference
        => ProductionReference.Create(AdditionalCostProductionOrderId, AdditionalCostEquipmentId);

    private IList<AcceptanceReportItemIssuedDetail> _issuedDetails = new List<AcceptanceReportItemIssuedDetail>();
    public virtual IReadOnlyCollection<AcceptanceReportItemIssuedDetail> IssuedDetails => _issuedDetails.AsReadOnly();

    private IList<AcceptanceReportItemQuotaBasedMaterialQuantity> _quotaBasedMaterialQuantities = new List<AcceptanceReportItemQuotaBasedMaterialQuantity>();
    public virtual IReadOnlyCollection<AcceptanceReportItemQuotaBasedMaterialQuantity> QuotaBasedMaterialQuantities => _quotaBasedMaterialQuantities.AsReadOnly();

    private IList<AcceptanceReportItemShippedDetail> _shippedDetails = new List<AcceptanceReportItemShippedDetail>();
    public virtual IReadOnlyCollection<AcceptanceReportItemShippedDetail> ShippedDetails => _shippedDetails.AsReadOnly();

    private IList<AcceptanceReportItemLog> _acceptanceReportItemLogs = new List<AcceptanceReportItemLog>();
    public virtual IReadOnlyCollection<AcceptanceReportItemLog> AcceptanceReportItemLogs => _acceptanceReportItemLogs.AsReadOnly();

    private static void ValidateIds(
        Guid? processGroupId,
        Guid? materialId,
        Guid? partId,
        ProductionReference categoryProductionReference,
        ProductionReference additionalCostProductionReference,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue,
        AdditionalCost additionalCost,
        QuotaBasedMaterial quotaBasedMaterial)
    {
        var categoryReference = categoryProductionReference ?? ProductionReference.Empty();
        var additionalReference = additionalCostProductionReference ?? ProductionReference.Empty();

        if ((categoryReference.EquipmentId != null || additionalReference.EquipmentId != null) && partId == null)
        {
            throw new ArgumentException("Phải chỉ rõ Phụ tùng thuộc Thiết bị");
        }

        bool requiresMaintain =
            materialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain ||
            additionalCost == AdditionalCost.Maintain;

        bool requiresMaterial =
            materialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Material ||
            additionalCost == AdditionalCost.Material ||
            additionalCost == AdditionalCost.SafeAndWelfare;

        if (requiresMaintain && partId == null)
        {
            throw new ArgumentException(CustomResponseMessage.MaintainIdRequired);
        }

        if (requiresMaterial && materialId == null)
        {
            throw new ArgumentException(CustomResponseMessage.MaterialIdRequired);
        }

        if ((materialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None) && processGroupId == null)
        {
            throw new ArgumentException(CustomResponseMessage.ProcessGroupNotFound);
        }
    }

    private static void ValidateQuantityDetails(
        IList<(IssuedQuantityType Type, double Quantity)> issuedDetails,
        IList<(ShippedQuantityType Type, double Quantity)> shippedDetails,
        IList<(QuotaBasedMaterialType Type, double Quantity)>? quotaBasedMaterialQuantities,
        QuotaBasedMaterial quotaBasedMaterial)
    {
        if (issuedDetails == null || !issuedDetails.Any())
        {
            throw new ArgumentException("Phải có ít nhất 1 dòng số lượng lĩnh");
        }

        if (issuedDetails.Any(x => x.Quantity < 0))
        {
            throw new ArgumentException("Số lượng lĩnh không được âm");
        }

        if (shippedDetails == null || !shippedDetails.Any())
        {
            throw new ArgumentException("Phải có ít nhất 1 dòng số lượng xuất");
        }

        if (shippedDetails.Any(x => x.Quantity < 0))
        {
            throw new ArgumentException("Số lượng xuất không được âm");
        }

        var duplicateIssued = issuedDetails
            .GroupBy(x => x.Type)
            .Any(g => g.Count() > 1);
        if (duplicateIssued)
        {
            throw new ArgumentException("Không được trùng loại số lượng lĩnh");
        }

        var duplicateShipped = shippedDetails
            .GroupBy(x => x.Type)
            .Any(g => g.Count() > 1);
        if (duplicateShipped)
        {
            throw new ArgumentException("Không được trùng loại số lượng xuất");
        }

        // Validate QuotaBasedMaterialQuantities
        if (quotaBasedMaterial != QuotaBasedMaterial.None)
        {
            if (quotaBasedMaterialQuantities == null || !quotaBasedMaterialQuantities.Any())
            {
                throw new ArgumentException("Vật tư theo hạn mức phải có ít nhất 1 dòng số lượng (Mới hoặc Tái sử dụng)");
            }

            if (quotaBasedMaterialQuantities.Any(x => x.Quantity < 0))
            {
                throw new ArgumentException("Số lượng vật tư theo hạn mức không được âm");
            }

            var duplicateQuotaBased = quotaBasedMaterialQuantities
                .GroupBy(x => x.Type)
                .Any(g => g.Count() > 1);
            if (duplicateQuotaBased)
            {
                throw new ArgumentException("Không được trùng loại số lượng vật tư theo hạn mức");
            }

            double totalShippedQuantity = shippedDetails.Sum(x => x.Quantity);
            double totalQuotaBasedQuantity = quotaBasedMaterialQuantities.Sum(x => x.Quantity);

            if (totalQuotaBasedQuantity > totalShippedQuantity)
            {
                throw new ArgumentException("Tổng số lượng vật tư theo hạn mức không được vượt quá số lượng xuất");
            }
        }
    }

    private static void ValidateUsageTime(double usageTime)
    {
        if (usageTime < 0)
        {
            throw new ArgumentException("Thời gian sử dụng không được âm");
        }
    }

    public static AcceptanceReportItem Create(
        Guid acceptanceReportId,
        Guid? processGroupId,
        Guid? materialId,
        Guid? partId,
        double usageTime,
        ItemType itemType,
        ProductionReference categoryProductionReference,
        ProductionReference additionalCostProductionReference,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue,
        double materialsIncludedInContractRevenueQuantity,
        AdditionalCost additionalCost,
        OtherMaterialDetail otherMaterialDetail,
        double additionalCostQuantity,
        QuotaBasedMaterial quotaBasedMaterial,
        QuotaBasedMaterialType quotaBasedMaterialType,
        Asset asset,
        double assetMaterialQuantity,
        IList<(IssuedQuantityType Type, double Quantity)> issuedDetails,
        IList<(ShippedQuantityType Type, double Quantity)> shippedDetails,
        IList<(QuotaBasedMaterialType Type, double Quantity)>? quotaBasedMaterialQuantities)
    {
        ValidateIds(processGroupId, materialId, partId, categoryProductionReference, additionalCostProductionReference,
            materialsIncludedInContractRevenue, additionalCost, quotaBasedMaterial);

        ValidateQuantityDetails(issuedDetails, shippedDetails, quotaBasedMaterialQuantities, quotaBasedMaterial);
        ValidateUsageTime(usageTime);

        var item = new AcceptanceReportItem
        {
            AcceptanceReportId = acceptanceReportId,
            ProcessGroupId = processGroupId,
            MaterialId = materialId,
            PartId = partId,
            EquipmentId = categoryProductionReference.EquipmentId,
            UsageTime = usageTime,
            MaterialsIncludedInContractRevenue = materialsIncludedInContractRevenue,
            MaterialsIncludedInContractRevenueQuantity = materialsIncludedInContractRevenueQuantity,
            AdditionalCost = additionalCost,
            OtherMaterialDetail = otherMaterialDetail,
            AdditionalCostQuantity = additionalCostQuantity,
            QuotaBasedMaterial = quotaBasedMaterial,
            QuotaBasedMaterialType = quotaBasedMaterialType,
            Asset = asset,
            AssetMaterialQuantity = assetMaterialQuantity,
            ItemType = itemType,
            ProductionOrderId = categoryProductionReference.ProductionOrderId,
            AdditionalCostProductionOrderId = additionalCostProductionReference.ProductionOrderId,
            AdditionalCostEquipmentId = additionalCostProductionReference.EquipmentId,
        };

        foreach (var detail in issuedDetails)
        {
            item._issuedDetails.Add(AcceptanceReportItemIssuedDetail.Create(item.Id, detail.Type, detail.Quantity));
        }

        foreach (var detail in shippedDetails)
        {
            item._shippedDetails.Add(AcceptanceReportItemShippedDetail.Create(item.Id, detail.Type, detail.Quantity));
        }

        if (quotaBasedMaterial != QuotaBasedMaterial.None && quotaBasedMaterialQuantities != null)
        {
            foreach (var detail in quotaBasedMaterialQuantities)
            {
                item._quotaBasedMaterialQuantities.Add(
                    AcceptanceReportItemQuotaBasedMaterialQuantity.Create(item.Id, detail.Type, detail.Quantity));
            }
        }

        return item;
    }

    public void Update(
        Guid? processGroupId,
        Guid? materialId,
        Guid? partId,
        double usageTime,
        ItemType itemType,
        ProductionReference categoryProductionReference,
        ProductionReference additionalCostProductionReference,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue,
        double materialsIncludedInContractRevenueQuantity,
        AdditionalCost additionalCost,
        OtherMaterialDetail otherMaterialDetail,
        double additionalCostQuantity,
        QuotaBasedMaterial quotaBasedMaterial,
        QuotaBasedMaterialType quotaBasedMaterialType,
        Asset asset,
        double assetMaterialQuantity,
        IList<(IssuedQuantityType Type, double Quantity)> issuedDetails,
        IList<(ShippedQuantityType Type, double Quantity)> shippedDetails,
        IList<(QuotaBasedMaterialType Type, double Quantity)>? quotaBasedMaterialQuantities)
    {
        ValidateIds(processGroupId, materialId, partId, categoryProductionReference, additionalCostProductionReference,
            materialsIncludedInContractRevenue, additionalCost, quotaBasedMaterial);

        ValidateQuantityDetails(issuedDetails, shippedDetails, quotaBasedMaterialQuantities, quotaBasedMaterial);
        ValidateUsageTime(usageTime);

        ProcessGroupId = processGroupId;
        MaterialId = materialId;
        PartId = partId;
        EquipmentId = categoryProductionReference.EquipmentId;
        UsageTime = usageTime;
        MaterialsIncludedInContractRevenue = materialsIncludedInContractRevenue;
        MaterialsIncludedInContractRevenueQuantity = materialsIncludedInContractRevenueQuantity;
        AdditionalCost = additionalCost;
        OtherMaterialDetail = otherMaterialDetail;
        AdditionalCostQuantity = additionalCostQuantity;
        QuotaBasedMaterial = quotaBasedMaterial;
        QuotaBasedMaterialType = quotaBasedMaterialType;
        Asset = asset;
        AssetMaterialQuantity = assetMaterialQuantity;
        ItemType = itemType;
        ProductionOrderId = categoryProductionReference.ProductionOrderId;
        AdditionalCostProductionOrderId = additionalCostProductionReference.ProductionOrderId;
        AdditionalCostEquipmentId = additionalCostProductionReference.EquipmentId;

        // Clear và rebuild toàn bộ details (replace strategy)
        _issuedDetails.Clear();
        foreach (var detail in issuedDetails)
        {
            _issuedDetails.Add(AcceptanceReportItemIssuedDetail.Create(Id, detail.Type, detail.Quantity));
        }

        _shippedDetails.Clear();
        foreach (var detail in shippedDetails)
        {
            _shippedDetails.Add(AcceptanceReportItemShippedDetail.Create(Id, detail.Type, detail.Quantity));
        }

        _quotaBasedMaterialQuantities.Clear();
        if (quotaBasedMaterial != QuotaBasedMaterial.None && quotaBasedMaterialQuantities != null)
        {
            foreach (var detail in quotaBasedMaterialQuantities)
            {
                _quotaBasedMaterialQuantities.Add(
                    AcceptanceReportItemQuotaBasedMaterialQuantity.Create(Id, detail.Type, detail.Quantity));
            }
        }
    }

    public void UpdateUsageTime(double usageTime)
    {
        ValidateUsageTime(usageTime);
        UsageTime = usageTime;
    }
}
