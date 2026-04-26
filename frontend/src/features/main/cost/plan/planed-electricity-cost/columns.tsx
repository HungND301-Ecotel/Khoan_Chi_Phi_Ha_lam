import { ProcessGroupType } from '@/constants/process-group';
import { PlanedElectricityCostDetailCost } from '@/features/main/cost/plan/planed-electricity-cost/types';
import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export const getPlanedElectricityCostColumns = (
	processGroupType?: ProcessGroupType,
): ColumnDef<PlanedElectricityCostDetailCost>[] => {
	const kFactorLength =
		processGroupType === ProcessGroupType.DL ||
		processGroupType === ProcessGroupType.XL
			? 3
			: processGroupType === ProcessGroupType.LC
				? 1
				: 3;

	return [
		{
			accessorKey: 'equipmentCode',
			header: 'Mã thiết bị',
		},
		{
			accessorKey: 'equipmentName',
			header: 'Tên thiết bị',
		},
		{
			accessorKey: 'electricityUnitPrice',
			header: 'Đơn giá (đ)',
			cell: ({ row }) =>
				formatNumber(row.original.electricityUnitPrice ?? 0, {
					maximumFractionDigits: 0,
				}),
		},
		{
			accessorKey: 'quantity',
			header: 'Số lượng',
			cell: ({ row }) => formatNumber(row.original.quantity ?? 0),
		},
		...Array.from({ length: kFactorLength }).map<
			ColumnDef<PlanedElectricityCostDetailCost>
		>((_, idx) => {
			const name = `K${idx + 1}`;
			return {
				id: name,
				header: name,
				cell: ({ row }) => {
					const sortedAdjustmentFactors = [
						...(row.original?.adjustmentFactorDescriptions ?? []),
					].sort((a, b) =>
						(a?.adjustmentFactorCode ?? '').localeCompare(
							b?.adjustmentFactorCode ?? '',
						),
					);

					return formatNumber(
						sortedAdjustmentFactors[idx]?.effectiveValue ?? 0,
					);
				},
			};
		}),
		{
			accessorKey: 'totalPrice',
			header: 'Đơn giá điện năng (đ/m)',
			cell: ({ row }) => formatNumber(Math.round(row.original.totalPrice)),
		},
	];
};
