export type DashboardMonthlyItem = {
	month: number; // 1..12
	tunnelQuantity: number;
	longwallQuantity: number;
	plannedCost: number;
	adjustmentCost: number;
	actualCost: number;
};

export type DashboardCostSummary = {
	totalTunnelQuantity: number;
	totalLongwallQuantity: number;
	totalOtherQuantity: number;
	totalPlannedCost: number;
	totalAdjustmentCost: number;
	totalActualCost: number;
	monthlyData: DashboardMonthlyItem[];
};
