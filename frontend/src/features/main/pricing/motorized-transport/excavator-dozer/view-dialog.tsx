import { Fragment } from 'react';
import { MotorizedExcavatorDozerUnitPrice } from './columns';

type MotorizedExcavatorDozerViewDialogProps = {
	row?: MotorizedExcavatorDozerUnitPrice;
};

export function MotorizedExcavatorDozerViewDialog({
	row,
}: MotorizedExcavatorDozerViewDialogProps) {
	const allItems: MotorizedExcavatorDozerUnitPrice[] =
		(row as any)?.allProcesses || (row ? [row] : []);

	// Group items by Process Name
	const processGroups: Record<string, MotorizedExcavatorDozerUnitPrice[]> = {};
	allItems.forEach((item) => {
		const procName =
			item.productionProcessName || item.productionProcess || 'Công đoạn khác';
		if (!processGroups[procName]) {
			processGroups[procName] = [];
		}
		processGroups[procName].push(item);
	});

	return (
		<div className='mx-8 my-2 overflow-hidden rounded-md border border-gray-200 bg-white shadow-xs'>
			<table className='w-full text-left text-base'>
				<thead className='border-b border-gray-200 bg-gray-50 text-sm font-bold text-gray-800'>
					<tr>
						<th className='min-w-[260px] px-6 py-3.5'>Công đoạn sản xuất</th>
						<th className='px-6 py-3.5 text-right'>Đơn giá nhiên liệu (đ)</th>
						<th className='px-6 py-3.5 text-right'>Đơn giá SCTX (đ)</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-gray-100'>
					{Object.entries(processGroups).map(([procName, items], pIdx) => {
						// Sort items A -> B -> C
						const sortedItems = [...items].sort((a, b) => {
							const qA = (a.equipmentQuality || '').toUpperCase();
							const qB = (b.equipmentQuality || '').toUpperCase();
							return qA.localeCompare(qB);
						});

						return (
							<Fragment key={pIdx}>
								{/* Process Title Row */}
								<tr className='bg-gray-50/80 font-bold text-gray-800'>
									<td
										colSpan={3}
										className='text-primary px-6 py-3 text-base font-bold'
									>
										{procName}
									</td>
								</tr>
								{/* Equipment Quality Rows */}
								{sortedItems.map((item, qIdx) => {
									const rawQ = item.equipmentQuality || 'A';
									const qualityText =
										rawQ.startsWith('Loại') || rawQ.startsWith('Thiết bị')
											? rawQ
											: `Loại ${rawQ}`;
									const fuel =
										item.details?.[0]?.fuelUnitPrice ?? item.fuelUnitPrice ?? 0;
									const maint =
										item.details?.[0]?.maintenanceUnitPrice ??
										item.maintenanceUnitPrice ??
										0;

									return (
										<tr key={qIdx} className='hover:bg-gray-50/30'>
											<td className='px-6 py-2.5 pl-12 text-base font-medium text-gray-800'>
												{qualityText}
											</td>
											<td className='px-6 py-2.5 text-right text-base font-semibold text-gray-900'>
												{fuel > 0 ? fuel.toLocaleString('vi-VN') : '-'}
											</td>
											<td className='px-6 py-2.5 text-right text-base font-semibold text-gray-900'>
												{maint > 0 ? maint.toLocaleString('vi-VN') : '-'}
											</td>
										</tr>
									);
								})}
							</Fragment>
						);
					})}
				</tbody>
			</table>
		</div>
	);
}
