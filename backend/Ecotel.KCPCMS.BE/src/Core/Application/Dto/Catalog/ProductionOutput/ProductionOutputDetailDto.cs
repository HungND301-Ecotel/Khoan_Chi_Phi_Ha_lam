using Domain.Common.Enums;

namespace Application.Dto.Catalog.ProductionOutput;

// ============================================================
// Main Response
// ============================================================

public record ProductionOutputDetailResponseDto
{
    public Guid ProductionOutputId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public Guid? DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public double ProductionMeters { get; set; }
    public double StandardProductionMeters { get; set; }

    /// A. Vật tư tính vào doanh thu khoán
    /// FE phân biệt sub-section qua SectionAType:
    ///   1 = Vật liệu
    ///   2 = Chi phí SCTX (theo kế hoạch vật tư) — TH1
    ///   3 = Chi phí SCTX dài kỳ phân bổ — TH2
    public List<MaterialGroupDto> SectionA { get; set; } = new();

    /// B. Quyết định bổ sung chi phí
    /// FE phân biệt sub-section qua AdditionalCostType:
    ///   Material     = Vật liệu
    ///   Maintain     = Sửa chữa thường xuyên
    ///   OtherMaterial = Vật tư theo chế độ NLĐ / PCCC / mưa bão
    public List<MaterialGroupDto> SectionB { get; set; } = new();

    /// C. Vật tư khoán theo hạn mức
    public List<MaterialGroupDto> SectionC { get; set; } = new();

    /// D. Tài sản
    public List<MaterialGroupDto> SectionD { get; set; } = new();
}

// ============================================================
// Material Group
// ============================================================

public record MaterialGroupDto
{
    /// Mã định danh nhóm (FE dùng làm key):
    ///
    /// SectionA Vật liệu (SectionAType=1):
    ///   AssignmentCode.Code | "VTK"
    ///
    /// SectionA SCTX TH1 (SectionAType=2):
    ///   PartType=OtherPart           → "VTK"
    ///   Có ProductionOrderId         → ProductionOrderId.ToString()
    ///   Không có ProductionOrderId   → Equipment.Code
    ///
    /// SectionA SCTX TH2 (SectionAType=3):
    ///   Có ProductionOrderId         → ProductionOrderId.ToString()
    ///   Không có ProductionOrderId   → Equipment.Code | "VTK"
    ///
    /// SectionB Vật liệu (AdditionalCostType=Material):
    ///   Có ProductionOrderId         → ProductionOrderId.ToString()
    ///   Không có                     → "NO_ORDER"
    ///
    /// SectionB SCTX (AdditionalCostType=Maintain):
    ///   Giống SectionA SCTX TH1
    ///
    /// SectionB OtherMaterial:
    ///   OtherMaterialDetail enum name
    ///
    /// SectionC: "MineSupport" | "SupportAccessories" | "MineTimber"
    /// SectionD: "ASSET"
    public string GroupCode { get; set; } = "";

    /// Tên hiển thị (Equipment.Name / AssignmentCode.Name / ...)
    public string GroupName { get; set; } = "";

    /// Loại vật tư: "Vật liệu" | "SCTX" | "Vì chống lò" | "Phụ kiện chống lò" | "Gỗ lò" | "Tài sản"
    public string MaterialType { get; set; } = "";

    // ── Sub-section discriminators ──────────────────────────
    /// Chỉ set ở SectionA. Giá trị: 1=VatLieu, 2=SctxTh1, 3=SctxTh2
    public int? SectionAType { get; set; }

    /// Chỉ set ở SectionB.
    public AdditionalCost? AdditionalCostType { get; set; }

    // ── Metadata ─────────────────────────────────────────────
    /// Set khi group được tạo theo ProductionOrder thay vì Equipment/Assignment.
    public Guid? ProductionOrderId { get; set; }

    /// Chỉ set ở SectionB OtherMaterial.
    public OtherMaterialDetail? OtherMaterialDetail { get; set; }

    // ── Content ───────────────────────────────────────────────
    public List<MaterialDetailDto> Materials { get; set; } = new();

    /// SubGroups — chỉ dùng cho SectionC MineSupport / SupportAccessories
    /// SubGroupCode: "New" | "Reusable"
    public List<SubGroupDto> SubGroups { get; set; } = new();
}

public record SubGroupDto
{
    public string SubGroupCode { get; set; } = "";
    public List<MaterialDetailDto> Materials { get; set; } = new();
}

// ============================================================
// Material Detail
// ============================================================

public record MaterialDetailDto
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string UnitOfMeasureName { get; set; } = "";
    public decimal PlannedUnitPrice { get; set; }
    public decimal ActualUnitPrice { get; set; }

    /// Tồn đầu kỳ — chỉ có ở SCTX TH2
    public BeginningInventoryDto? BeginningInventory { get; set; }

    /// Lĩnh trong kỳ — có ở mọi item
    public IssuedInPeriodDto? IssuedInPeriod { get; set; }

    /// Xuất trong kỳ — có ở mọi item
    public ExportedInPeriodDto? ExportedInPeriod { get; set; }

    /// Tồn cuối kỳ — SCTX TH2 và SectionC
    public EndingInventoryDto? EndingInventory { get; set; }
}

// ============================================================
// Period Data
// ============================================================

public record BeginningInventoryDto
{
    /// Tồn tại khai trường đầu kỳ — carry-forward từ EndingInventory.RemainingAtSite tháng trước
    /// (item không có ProductionOrderId)
    public InventoryQuantityDto? RemainingAtSite { get; set; }

    /// Quyết định, giao khoán công trình đầu kỳ — carry-forward từ EndingInventory.RemainingByOrder tháng trước
    /// (item có ProductionOrderId)
    public InventoryQuantityDto? RemainingByOrder { get; set; }

    /// Chi phí chờ hạch toán đầu kỳ — chỉ có ở SCTX TH2
    public decimal? PendingValue { get; set; }

    public TotalDto Total { get; set; } = new();
}

public record IssuedInPeriodDto
{
    public ReceivedSuppliesDto Received { get; set; } = new();
    public QuantityAmountDto BorrowedNoVoucher { get; set; } = new();
    public QuantityAmountDto ReturnPreviousMonthVoucher { get; set; } = new();
    public QuantityAmountDto OtherReceipt { get; set; } = new();
    public TotalDto Total { get; set; } = new();
}

public record ExportedInPeriodDto
{
    public ExportedToProductionDto ExportedToProduction { get; set; } = new();
    public QuantityAmountDto OtherExport { get; set; } = new();
    public QuantityAmountDto ContractSettlement { get; set; } = new();
    /// Chi phí vật tư dài kỳ hạch toán — chỉ có ở SCTX
    public LongTermExpenseDto? LongTermExpense { get; set; }
    public TotalDto Total { get; set; } = new();
}

public record EndingInventoryDto
{
    /// Tồn tại khai trường — item KHÔNG có ProductionOrderId
    public InventoryQuantityDto? RemainingAtSite { get; set; }

    /// Quyết định, giao khoán công trình — item CÓ ProductionOrderId
    public InventoryQuantityDto? RemainingByOrder { get; set; }

    /// Chi phí chờ hạch toán cuối kỳ — chỉ có ở SCTX TH2
    public decimal? PendingValue { get; set; }

    public TotalDto Total { get; set; } = new();
}

/// Số lượng + thành tiền tồn kho tại một vị trí cụ thể
public record InventoryQuantityDto
{
    public double Quantity { get; set; }
    public decimal Amount { get; set; }
}

public record QuantityAmountDto
{
    public double Quantity { get; set; }
    public decimal Amount { get; set; }
}

public record ReceivedSuppliesDto
{
    public double Quantity { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
}

public record ExportedToProductionDto
{
    public double Quantity { get; set; }
    public decimal Amount { get; set; }
}

public record LongTermExpenseDto
{
    public decimal Amount { get; set; }
}

public record TotalDto
{
    public double Quantity { get; set; }
    public decimal Amount { get; set; }
}

// ============================================================
// Internal constants (không expose ra contract)
// ============================================================
internal static class MatTypeLabel
{
    public const string VatLieu = "Vật liệu";
    public const string Sctx = "SCTX";
}

internal static class SecAType
{
    public const int VatLieu = 1;
    public const int SctxTh1 = 2;
    public const int SctxTh2 = 3;
}
