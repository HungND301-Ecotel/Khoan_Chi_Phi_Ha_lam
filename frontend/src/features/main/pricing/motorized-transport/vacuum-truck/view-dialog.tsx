import { useDialog } from '@/data/dialog/dialog.hook';
import { MotorizedVacuumTruckUnitPrice } from './columns';

type ViewDialogProps = {
	row?: MotorizedVacuumTruckUnitPrice | null;
	data?: any;
};

export function MotorizedVacuumTruckViewDialog({ row }: ViewDialogProps) {
	const { setOpen } = useDialog();

	if (!row) return null;

	const allProcs: MotorizedVacuumTruckUnitPrice[] = row.allProcesses || [row];

	// Group by process name
	const groupedByProcess: Record<string, MotorizedVacuumTruckUnitPrice[]> = {};
	allProcs.forEach((p) => {
		const procName = p.productionProcessName || 'Công đoạn chưa phân loại';
		if (!groupedByProcess[procName]) {
			groupedByProcess[procName] = [];
		}
		groupedByProcess[procName].push(p);
	});

	const formatCurrency = (val: number | null | undefined) => {
		if (val === null || val === undefined || isNaN(val)) return '-';
		return new Intl.NumberFormat('vi-VN').format(val);
	};

	return (
		<div className='max-h-[80vh] space-y-6 overflow-y-auto p-2'>
			{/* Top Summary Header */}
			<div className='rounded-lg border border-gray-200 bg-gray-50/50 p-4 shadow-xs'>
				<div className='grid grid-cols-2 gap-4 text-sm'>
					<div>
						<span className='font-medium text-gray-500'>Thời gian bắt đầu:</span>{' '}
						<span className='font-semibold text-gray-900'>
							{row.startMonth?.substring(0, 7)}
						</span>
					</div>
					<div>
						<span className='font-medium text-gray-500'>Thời gian kết thúc:</span>{' '}
						<span className='font-semibold text-gray-900'>
							{row.endMonth?.substring(0, 7)}
						</span>
					</div>
					<div className='col-span-2'>
						<span className='font-medium text-gray-500'>Nhóm vật tư, tài sản:</span>{' '}
						<span className='font-semibold text-primary'>{row.assignmentCodeName}</span>
					</div>
				</div>
			</div>

			{/* Sub tables grouped by Production Process */}
			{Object.entries(groupedByProcess).map(([procName, procList]) => {
				const isHourly = procName.toLowerCase().includes('phục vụ') || procName.toLowerCase().includes('di chuyển');
				const unitLabel = isHourly ? '(đ/h)' : '(đ/tkm)';

				// Sort items by quality (A -> B -> C)
				const sortedProcList = [...procList].sort((a, b) => {
					const qA = (a.equipmentQuality || '').toUpperCase();
					const qB = (b.equipmentQuality || '').toUpperCase();
					return qA.localeCompare(qB);
				});

				return (
					<div key={procName} className='space-y-3'>
						<div className='flex items-center gap-2 border-b border-gray-200 pb-2'>
							<span className='h-2.5 w-2.5 rounded-full bg-primary'></span>
							<h4 className='text-sm font-bold uppercase text-gray-800'>{procName}</h4>
						</div>

						<div className='overflow-hidden rounded-lg border border-gray-200 bg-white shadow-xs'>
							<table className='w-full text-left text-sm'>
								<thead className='border-b border-gray-200 bg-gray-50 text-xs font-semibold uppercase text-gray-600'>
									<tr>
										<th className='min-w-[120px] px-4 py-3'>Chất lượng</th>
										{!isHourly && <th className='min-w-[120px] px-4 py-3'>Cung độ</th>}
										<th className='px-4 py-3'>Đơn giá Nhiên liệu {unitLabel}</th>
										<th className='px-4 py-3'>Đơn giá SCTX {unitLabel}</th>
									</tr>
								</thead>
								<tbody className='divide-y divide-gray-100'>
									{sortedProcList.flatMap((item, pIdx) => {
										const details = item.details || [];
										if (details.length === 0) {
											return (
												<tr key={`${pIdx}-empty`} className='hover:bg-gray-50/50'>
													<td className='px-4 py-2.5 font-medium text-gray-700'>
														Thiết bị loại {item.equipmentQuality}
													</td>
													{!isHourly && <td className='px-4 py-2.5 text-gray-500'>-</td>}
													<td className='px-4 py-2.5 text-gray-500'>-</td>
													<td className='px-4 py-2.5 text-gray-500'>-</td>
												</tr>
											);
										}

										return details.map((d, dIdx) => (
											<tr key={`${pIdx}-${dIdx}`} className='hover:bg-gray-50/50'>
												<td className='px-4 py-2.5 font-medium text-gray-700'>
													Thiết bị loại {item.equipmentQuality}
												</td>
												{!isHourly && (
													<td className='px-4 py-2.5 text-gray-700'>
														{d.haulDistanceValue || '-'}
													</td>
												)}
												<td className='px-4 py-2.5 text-gray-900'>
													{formatCurrency(d.fuelUnitPrice)}
												</td>
												<td className='px-4 py-2.5 text-gray-900'>
													{formatCurrency(d.maintenanceUnitPrice)}
												</td>
											</tr>
										));
									})}
								</tbody>
							</table>
						</div>
					</div>
				);
			})}

			<div className='flex justify-end pt-2'>
				<button
					type='button'
					onClick={() => setOpen(false)}
					className='rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50'
				>
					Đóng
				</button>
			</div>
		</div>
	);
}
