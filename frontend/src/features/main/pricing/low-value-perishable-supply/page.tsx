import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Checkbox } from '@/components/ui/checkbox';
import { API } from '@/constants/api-enpoint';
import { LowValuePerishableSupplyType } from '@/constants/low-value-perishable-supply';
import { useMeta } from '@/data/meta/meta-hook';
import {
	LOW_VALUE_PERISHABLE_SUPPLY_COLUMNS,
	LowValuePerishablePeriod,
	LowValuePerishableSupplyUnitPrice,
} from '@/features/main/pricing/low-value-perishable-supply/columns';
import { LowValuePerishableSupplyForm } from '@/features/main/pricing/low-value-perishable-supply/form';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import { usePermission } from '@/hooks/use-permission';
import { useState } from 'react';

type LowValuePerishableSupplyPageProps = {
	type: LowValuePerishableSupplyType;
};

export function LowValuePerishableSupplyPage({
	type,
}: LowValuePerishableSupplyPageProps) {
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const { hasPermission } = usePermission();

	const [selectedPeriodIds, setSelectedPeriodIds] = useState<string[]>([]);

	const permPrefix =
		type === LowValuePerishableSupplyType.TunnelExcavation
			? 'pricing.tunnellowvalueperishablesupplyunitprice'
			: type === LowValuePerishableSupplyType.Longwall
			? 'pricing.longwalllowvalueperishablesupplyunitprice'
			: 'pricing.transportlowvalueperishablesupplyunitprice';

	const apiConfig =
		type === LowValuePerishableSupplyType.TunnelExcavation
			? API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TUNNELING
			: type === LowValuePerishableSupplyType.Longwall
			? API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.LONGWALL
			: API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TRANSPORT;

	const isRowSelected = (
		row: LowValuePerishableSupplyUnitPrice,
	): boolean | 'indeterminate' => {
		const periods = row.periods ?? [];
		if (periods.length === 0) {
			return selectedPeriodIds.includes(row.id);
		}
		const selectedCount = periods.filter((p) =>
			selectedPeriodIds.includes(p.id),
		).length;
		if (selectedCount === 0) return false;
		if (selectedCount === periods.length) return true;
		return 'indeterminate';
	};

	const handleRowSelectChange = (
		row: LowValuePerishableSupplyUnitPrice,
		checked: boolean,
	) => {
		const periodIds =
			row.periods && row.periods.length > 0
				? row.periods.map((p) => p.id)
				: [row.id];

		setSelectedPeriodIds((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...periodIds]));
			} else {
				return prev.filter((id) => !periodIds.includes(id));
			}
		});
	};

	const handleSelectAllRowsChange = (
		checked: boolean,
		rows: LowValuePerishableSupplyUnitPrice[],
	) => {
		const allPeriodIds = rows.flatMap((r) =>
			r.periods && r.periods.length > 0
				? r.periods.map((p) => p.id)
				: [r.id],
		);
		setSelectedPeriodIds((prev) => {
			if (checked) {
				return Array.from(new Set([...prev, ...allPeriodIds]));
			} else {
				return prev.filter((id) => !allPeriodIds.includes(id));
			}
		});
	};

	const handleTogglePeriodSelect = (periodId: string) => {
		setSelectedPeriodIds((prev) => {
			if (prev.includes(periodId)) {
				return prev.filter((id) => id !== periodId);
			} else {
				return [...prev, periodId];
			}
		});
	};

	const handleDelete = async ({
		data,
	}: ActionDialogProps<LowValuePerishableSupplyUnitPrice>) => {
		if (selectedPeriodIds.length === 0) return;
		try {
			await api.delete(
				API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.DELETES,
				selectedPeriodIds,
			);

			popup.success(
				`Đã xoá thành công ${selectedPeriodIds.length} khoảng thời gian ${breadcrumb}.`,
			);
			setSelectedPeriodIds([]);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(apiConfig.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<LowValuePerishableSupplyUnitPrice>['data'],
	) => {
		try {
			const result = await api.import(apiConfig.IMPORT, file);
			if (typeof result === 'string') {
				popup.success(`Đã tải về danh sách lỗi: ${result}`);
			} else {
				popup.success('Nhập dữ liệu thành công');
				await data?.refresh();
			}
		} catch (error) {
			popup.error(error);
		}
	};

	const transformData = (
		rows: LowValuePerishableSupplyUnitPrice[],
	): LowValuePerishableSupplyUnitPrice[] => {
		const groupMap = new Map<string, LowValuePerishableSupplyUnitPrice>();

		rows.forEach((row) => {
			const key = `${row.departmentId}__${row.processGroupId}`;
			const existing = groupMap.get(key);

			const period: LowValuePerishablePeriod = {
				id: row.id,
				startMonth: row.startMonth ?? '',
				endMonth: row.endMonth ?? '',
				totalPrice: row.totalPrice ?? 0,
			};

			if (!existing) {
				groupMap.set(key, {
					...row,
					id: key,
					periods: [period],
				});
			} else {
				existing.periods = existing.periods ?? [];
				if (!existing.periods.some((p) => p.id === period.id)) {
					existing.periods.push(period);
				}
			}
		});

		return Array.from(groupMap.values()).map((item) => ({
			...item,
			periods: (item.periods ?? []).sort((a, b) =>
				b.startMonth.localeCompare(a.startMonth),
			),
		}));
	};

	return (
		<DataTable
			url={apiConfig.LIST}
			transformData={transformData}
			columns={LOW_VALUE_PERISHABLE_SUPPLY_COLUMNS}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'departmentCode', label: 'Mã đơn vị' },
				{ key: 'departmentName', label: 'Tên đơn vị' },
				{ key: 'processGroupCode', label: 'Mã nhóm công đoạn' },
				{ key: 'processGroupName', label: 'Tên nhóm công đoạn' },
			]}
			onCreate={
				hasPermission(`${permPrefix}.create`)
					? (props) => <LowValuePerishableSupplyForm {...props} type={type} />
					: undefined
			}
			onDuplicate={
				hasPermission(`${permPrefix}.create`)
					? (props) => (
							<LowValuePerishableSupplyForm
								{...props}
								type={type}
								isDuplicate
							/>
						)
					: undefined
			}
			onUpdate={
				hasPermission(`${permPrefix}.update`)
					? (props) => <LowValuePerishableSupplyForm {...props} type={type} />
					: undefined
			}
			onDelete={hasPermission(`${permPrefix}.delete`) ? handleDelete : undefined}
			deleteCountOverride={selectedPeriodIds.length}
			deleteDisabledOverride={selectedPeriodIds.length === 0}
			isRowSelected={isRowSelected}
			onRowSelectChange={handleRowSelectChange}
			onSelectAllRowsChange={handleSelectAllRowsChange}
			onExport={hasPermission(`${permPrefix}.export`) ? handleExport : undefined}
			onImport={hasPermission(`${permPrefix}.import`) ? handleImport : undefined}
			onExpand={(props) => (
				<LowValuePerishablePeriodExpand
					{...props}
					selectedPeriodIds={selectedPeriodIds}
					onTogglePeriodSelect={handleTogglePeriodSelect}
				/>
			)}
		/>
	);
}

function LowValuePerishablePeriodExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: {
	row?: LowValuePerishableSupplyUnitPrice;
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}) {
	const periods = row?.periods ?? [];

	return (
		<div className='mx-6 my-2 overflow-hidden rounded-md border border-neutral-200 bg-white text-black'>
			<table className='w-full text-left text-sm text-black'>
					<thead className='bg-neutral-100 border-b border-neutral-200 text-sm font-semibold text-black'>
						<tr>
							<th scope='col' className='w-10 px-3 py-2.5 text-center' />
							<th
								scope='col'
								className='w-12 px-3 py-2.5 text-center text-black font-semibold'
							>
								STT
							</th>
							<th scope='col' className='px-4 py-2.5 text-black font-semibold'>
								Thời gian áp dụng
							</th>
							<th
								scope='col'
								className='px-4 py-2.5 text-right text-black font-semibold'
							>
								Đơn giá (đ/tháng)
							</th>
						</tr>
					</thead>
					<tbody className='divide-y divide-neutral-200'>
						{periods.length === 0 ? (
							<tr>
								<td
									colSpan={4}
									className='py-6 text-center text-sm text-black'
								>
									Chưa có khoảng thời gian nào được thiết lập.
								</td>
							</tr>
						) : (
							periods.map((period, index) => (
								<tr
									key={period.id}
									className='border-b border-neutral-200 transition-colors hover:bg-neutral-50/80'
								>
									<td className='w-10 px-3 py-2 text-center'>
										<Checkbox
											checked={selectedPeriodIds.includes(period.id)}
											onCheckedChange={() => onTogglePeriodSelect(period.id)}
											aria-label={`Chọn khoảng thời gian ${formatDate(period.startMonth)} - ${formatDate(period.endMonth)}`}
										/>
									</td>
									<td className='w-12 px-3 py-2 text-center text-sm text-black'>
										{index + 1}
									</td>
									<td className='px-4 py-2 text-sm text-black'>
										{formatDate(period.startMonth)} -{' '}
										{formatDate(period.endMonth)}
									</td>
									<td className='px-4 py-2 text-right text-sm font-semibold text-black'>
										{formatNumber(period.totalPrice)}
									</td>
								</tr>
							))
						)}
					</tbody>
				</table>
		</div>
	);
}
