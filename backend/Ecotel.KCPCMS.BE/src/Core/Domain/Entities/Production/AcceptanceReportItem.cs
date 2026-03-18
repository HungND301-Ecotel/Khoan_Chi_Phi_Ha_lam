using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Production;

public class AcceptanceReportItem : AuditableEntity<Guid>
{
    public Guid AcceptanceReportId { get; protected set; }
    public Guid? ProcessGroupId { get; protected set; }
    public Guid? MaintainUnitPriceEquipmentId { get; protected set; }
    public Guid? MaterialId { get; protected set; }

    public double IssuedQuantity { get; protected set; }
    public double ShippedQuantity { get; protected set; }

    //Vật tư tính vào doanh thu khoán
    public MaterialsIncludedInContractRevenue MaterialsIncludedInContractRevenue { get; protected set; }
    public double MaterialsIncludedInContractRevenueQuantity { get; protected set; }

    //Bổ sung chi phí
    public AdditionalCost AdditionalCost { get; protected set; }
    public double AdditionalCostQuantity { get; protected set; }

    //Vật tư theo hạn mức
    public QuotaBasedMaterial QuotaBasedMaterial { get; protected set; }
    public QuotaBasedMaterialType QuotaBasedMaterialType { get; protected set; }
    public double QuotaBasedMaterialQuantity { get; protected set; }

    //Tài sản
    public Asset Asset { get; protected set; }
    public double AssetMaterialQuantity { get; protected set; }

    // Navigation properties
    public virtual AcceptanceReport AcceptanceReport { get; protected set; }
    public virtual ProcessGroup? ProcessGroup { get; protected set; }
    public virtual MaintainUnitPriceEquipment? MaintainUnitPriceEquipment { get; protected set; }
    public virtual Material? Material { get; protected set; }


    private IList<AcceptanceReportItemLog> _acceptanceReportItemLogs = new List<AcceptanceReportItemLog>();
    public virtual IReadOnlyCollection<AcceptanceReportItemLog> AcceptanceReportItemLogs => _acceptanceReportItemLogs.AsReadOnly();

    private static void ValidateIds(
        Guid? processGroupId,
        Guid? materialId,
        Guid? maintainUnitPriceEquipmentId,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue,
        AdditionalCost additionalCost,
        QuotaBasedMaterial quotaBasedMaterial)
    {
        bool requiresMaintain =
            materialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain ||
            additionalCost == AdditionalCost.Maintain;

        bool requiresMaterial =
            materialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Material ||
            additionalCost == AdditionalCost.Material ||
            additionalCost == AdditionalCost.OtherMaterial ||
            quotaBasedMaterial != QuotaBasedMaterial.None;

        if (requiresMaintain && maintainUnitPriceEquipmentId == null)
        {
            throw new ArgumentException(CustomResponseMessage.MaintainIdRequired);
        }

        if (requiresMaterial && materialId == null)
        {
            throw new ArgumentException(CustomResponseMessage.MaterialIdRequired);
        }

        if (materialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None && processGroupId == null)
        {
            throw new ArgumentException(CustomResponseMessage.ProcessGroupNotFound);
        }
    }

    public static AcceptanceReportItem Create(
        Guid acceptanceReportId,
        Guid? processGroupId,
        Guid? materialId,
        Guid? maintainUnitPriceEquipmentId,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue,
        double materialsIncludedInContractRevenueQuantity,
        AdditionalCost additionalCost,
        double additionalCostQuantity,
        QuotaBasedMaterial quotaBasedMaterial,
        QuotaBasedMaterialType quotaBasedMaterialType,
        double quotaBasedMaterialQuantity,
        Asset asset,
        double assetMaterialQuantity,
        double issuedQuantity,
        double shippedQuantity)
    {
        ValidateIds(processGroupId, materialId, maintainUnitPriceEquipmentId, materialsIncludedInContractRevenue, additionalCost, quotaBasedMaterial);

        return new AcceptanceReportItem
        {
            AcceptanceReportId = acceptanceReportId,
            ProcessGroupId = processGroupId,
            MaterialId = materialId,
            MaintainUnitPriceEquipmentId = maintainUnitPriceEquipmentId,
            MaterialsIncludedInContractRevenue = materialsIncludedInContractRevenue,
            MaterialsIncludedInContractRevenueQuantity = materialsIncludedInContractRevenueQuantity,
            AdditionalCost = additionalCost,
            AdditionalCostQuantity = additionalCostQuantity,
            QuotaBasedMaterial = quotaBasedMaterial,
            QuotaBasedMaterialType = quotaBasedMaterialType,
            QuotaBasedMaterialQuantity = quotaBasedMaterialQuantity,
            Asset = asset,
            AssetMaterialQuantity = assetMaterialQuantity,
            IssuedQuantity = issuedQuantity,
            ShippedQuantity = shippedQuantity
        };
    }

    public void Update(
        Guid? processGroupId,
        Guid? materialId,
        Guid? maintainUnitPriceEquipmentId,
        MaterialsIncludedInContractRevenue materialsIncludedInContractRevenue,
        double materialsIncludedInContractRevenueQuantity,
        AdditionalCost additionalCost,
        double additionalCostQuantity,
        QuotaBasedMaterial quotaBasedMaterial,
        QuotaBasedMaterialType quotaBasedMaterialType,
        double quotaBasedMaterialQuantity,
        Asset asset,
        double assetMaterialQuantity,
        double issuedQuantity,
        double shippedQuantity)
    {
        ValidateIds(processGroupId, materialId, maintainUnitPriceEquipmentId, materialsIncludedInContractRevenue, additionalCost, quotaBasedMaterial);

        ProcessGroupId = processGroupId;
        MaterialId = materialId;
        MaintainUnitPriceEquipmentId = maintainUnitPriceEquipmentId;
        MaterialsIncludedInContractRevenue = materialsIncludedInContractRevenue;
        MaterialsIncludedInContractRevenueQuantity = materialsIncludedInContractRevenueQuantity;
        AdditionalCost = additionalCost;
        AdditionalCostQuantity = additionalCostQuantity;
        QuotaBasedMaterial = quotaBasedMaterial;
        QuotaBasedMaterialType = quotaBasedMaterialType;
        QuotaBasedMaterialQuantity = quotaBasedMaterialQuantity;
        Asset = asset;
        AssetMaterialQuantity = assetMaterialQuantity;
        IssuedQuantity = issuedQuantity;
        ShippedQuantity = shippedQuantity;
    }
}