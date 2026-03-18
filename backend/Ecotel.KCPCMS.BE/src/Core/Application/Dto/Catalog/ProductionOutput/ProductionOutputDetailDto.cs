namespace Application.Dto.Catalog.ProductionOutput;

#region Main Response

public record ProductionOutputDetailResponseDto
{
    public Guid ProductionOutputId { get; init; }
    public DateOnly StartMonth { get; init; }
    public DateOnly EndMonth { get; init; }
    public double ProductionMeters { get; init; }
    public double StandardProductionMeters { get; init; }
    public List<ProductionOutputDetailItemDto> Items { get; init; } = new();
}

public record ProductionOutputDetailItemDto
{
    public int CategoryType { get; init; } // 1: Materials in Contract Revenue, 2: Additional Cost, 3: Quota-Based Material, 4: Asset
    public string CategoryName { get; init; } // "Vật tư tính vào doanh thu khoán", "Bổ sung chi phí", etc.
    public List<MaterialGroupDto> MaterialGroups { get; init; } = new();
}

#endregion

#region Material Group

public record MaterialGroupDto
{
    public string GroupCode { get; init; } // AssignmentCode.Code or Equipment.Code or "VTK"
    public string GroupName { get; init; } // AssignmentCode.Name or Equipment.Name or "Vật tư khác"
    public string MaterialType { get; init; } // "Vật liệu", "SCTX", etc.
    public List<MaterialDetailDto> Materials { get; init; } = new(); // Direct materials (for categories without subgroups)
    public List<SubGroupDto> SubGroups { get; init; } = new(); // Subgroups (for quota-based materials with New/Reusable)
}

public record SubGroupDto
{
    public string SubGroupCode { get; init; } // "New" or "Reusable"
    public string SubGroupName { get; init; } // "Lĩnh mới" or "Lĩnh tái sử dụng"
    public List<MaterialDetailDto> Materials { get; init; } = new();
}

#endregion

#region Material Detail

public record MaterialDetailDto
{
    public Guid MaterialId { get; init; }
    public string MaterialCode { get; init; }
    public string MaterialName { get; init; }
    public string UnitOfMeasureName { get; init; }
    public decimal PlannedUnitPrice { get; init; }
    public decimal ActualUnitPrice { get; init; } // Default = 0

    // For Materials in Contract Revenue
    public IssuedInPeriodDto? IssuedInPeriod { get; init; }
    public ExportedInPeriodDto? ExportedInPeriod { get; init; }

    // For SCTX (Materials in Contract Revenue)
    public BeginningInventoryDto? BeginningInventory { get; init; }
    public EndingInventoryDto? EndingInventory { get; init; }

    // For Quota-Based Material
    public string? State { get; init; } // "Lĩnh mới", "Lĩnh tái sử dụng", etc.
}

#endregion

#region Period Data

public record IssuedInPeriodDto
{
    public ReceivedSuppliesDto Received { get; init; }
    public TotalDto Total { get; init; }
}

public record ExportedInPeriodDto
{
    public ExportedToProductionDto ExportedToProduction { get; init; }
    public LongTermExpenseDto? LongTermExpense { get; init; } // Only for SCTX
    public TotalDto Total { get; init; }
}

public record ReceivedSuppliesDto
{
    public double Quantity { get; init; }
    public decimal PlannedAmount { get; init; } // Số lượng lĩnh * đơn giá kế hoạch
    public decimal ActualAmount { get; init; } // Số lượng lĩnh * đơn giá thực tế
}

public record ExportedToProductionDto
{
    public double Quantity { get; init; }
    public decimal Amount { get; init; } // Số lượng xuất * đơn giá kế hoạch
}

public record LongTermExpenseDto
{
    public decimal Amount { get; init; } // Chi phí vật tư dài kỳ hạch toán (cumulative)
}

public record TotalDto
{
    public double Quantity { get; init; }
    public decimal Amount { get; init; } // Thành tiền KH or cumulative amount
}

public record BeginningInventoryDto
{
    public decimal PendingValue { get; init; } // Chi phí chờ hạch toán đầu kỳ (cumulative from previous periods)
    public TotalDto Total { get; init; }
}

public record EndingInventoryDto
{
    public ExportedToProductionDto ExportedToProduction { get; init; }
    public TotalDto Total { get; init; }
}

#endregion
