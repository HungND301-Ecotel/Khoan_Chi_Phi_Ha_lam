namespace Application.Dto.Catalog.LumpSumFinalSettlement;

public class LumpSumFinalSettlementDto
{
    public Guid Id { get; set; }
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; } = string.Empty;
    public double PlannedQuantity { get; set; }
    public double ActualQuantity { get; set; }

    public LumpSumCostDetailDto Materials { get; set; } = new();
    public LumpSumCostDetailDto Maintains { get; set; } = new();
    public LumpSumCostDetailDto Electricities { get; set; } = new();

    public double TotalAmount { get; set; }
}

public class LumpSumCostDetailDto
{
    public double UnitPrice { get; set; }
    public double TotalAmount { get; set; }
}

public class LumpSumFinalSettlementQuarterResponseDto
{
    public List<LumpSumFinalSettlementDto> Items { get; set; } = new();
    public List<LumpSumFinalSettlementMonthResponseDto> MonthBreakdowns { get; set; } = new();
    public List<LumpSumQuarterRevenueByMonthDto> RevenuesByMonth { get; set; } = new();
    public List<LumpSumQuarterRevenueByMonthDto> CostsByMonth { get; set; } = new();
    public List<LumpSumQuarterRevenueByMonthDto> SavingsByMonth { get; set; } = new();
    public List<LumpSumQuarterTransferredCostDto> TransferredCosts { get; set; } = new();
    public List<LumpSumQuarterCustomCostDto> CustomCosts { get; set; } = new();
    public LumpSumQuarterRevenueByMonthDto RevenueQuarter { get; set; } = new();
    public LumpSumQuarterRevenueByMonthDto CostQuarter { get; set; } = new();
    public LumpSumQuarterRevenueByMonthDto SavingQuarter { get; set; } = new();
    public double CoalExcavationActualQuantity { get; set; }
    public double CoalCrosscutActualQuantity { get; set; }
    public double MeterExcavationActualQuantity { get; set; }
    public double MeterCrosscutActualQuantity { get; set; }
    public double TotalSavingQuarter { get; set; }
    public double AcceptedSavingQuarter { get; set; }
    public double SavingsValue { get; set; }
    public double RevenueAdjustmentRate { get; set; }
    public double SavingAddedToIncomeQuarter { get; set; }
}

public class LumpSumFinalSettlementMonthResponseDto
{
    public List<LumpSumFinalSettlementDto> Items { get; set; } = new();
    public LumpSumQuarterRevenueByMonthDto Revenue { get; set; } = new();
    public LumpSumQuarterRevenueByMonthDto Cost { get; set; } = new();
    public LumpSumQuarterRevenueByMonthDto Saving { get; set; } = new();
    public LumpSumQuarterTransferredCostDto TransferredCost { get; set; } = new();
    public List<LumpSumQuarterCustomCostDto> CustomCosts { get; set; } = new();
    public double CoalExcavationActualQuantity { get; set; }
    public double CoalCrosscutActualQuantity { get; set; }
    public double MeterExcavationActualQuantity { get; set; }
    public double MeterCrosscutActualQuantity { get; set; }
    public double TotalSavingMonth { get; set; }
    public double SavingsValue { get; set; }
    public double AcceptedSavingMonth { get; set; }
    public double RevenueAdjustmentRate { get; set; }
    public double SavingAddedToIncomeMonth { get; set; }
    public List<LumpSumSavingCarryForwardByMonthDto> SavingCarryForwardByMonths { get; set; } = new();
    public double SavingCarryForwardToNextMonths { get; set; }
}

public class LumpSumQuarterRevenueByMonthDto
{
    public int Month { get; set; }
    public LumpSumCostDetailDto Materials { get; set; } = new();
    public LumpSumCostDetailDto Maintains { get; set; } = new();
    public LumpSumCostDetailDto Electricities { get; set; } = new();
    public double TotalAmount { get; set; }
}

public class LumpSumQuarterTransferredCostDto
{
    public int Month { get; set; }
    public LumpSumCostDetailDto Materials { get; set; } = new();
    public LumpSumCostDetailDto Maintains { get; set; } = new();
    public LumpSumCostDetailDto Electricities { get; set; } = new();
    public double TotalAmount { get; set; }
}

public class LumpSumQuarterCustomCostDto
{
    public Guid Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public Guid? ProcessGroupId { get; set; }
    public string CustomName { get; set; } = string.Empty;
    public double ActualQuantity { get; set; }
    public double MaterialUnitPrice { get; set; }
    public double MaintainUnitPrice { get; set; }
    public double ElectricityUnitPrice { get; set; }
}

public class LumpSumSavingCarryForwardByMonthDto
{
    public int Month { get; set; }
    public double Value { get; set; }
}
