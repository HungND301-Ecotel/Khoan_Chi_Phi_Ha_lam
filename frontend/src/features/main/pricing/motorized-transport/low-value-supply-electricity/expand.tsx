import { Checkbox } from '@/components/ui/checkbox';
import { cn, formatDate } from '@/lib/utils';
import { Fragment } from 'react';
import {
	MotorizedLowValuePeriod,
	MotorizedLowValueSupplyElectricityUnitPrice,
} from './columns';

interface MotorizedLowValuePeriodExpandProps {
	row: MotorizedLowValueSupplyElectricityUnitPrice;
	selectedPeriodIds: string[];
	onTogglePeriodSelect: (periodId: string) => void;
}

export function MotorizedLowValuePeriodExpand({
	row,
	selectedPeriodIds,
	onTogglePeriodSelect,
}: MotorizedLowValuePeriodExpandProps) {
	const periods: MotorizedLowValuePeriod[] = row?.periods ?? [];

	return (
		<div className='mx-6 my-2 overflow-hidden rounded-md border border-neutral-200 bg-white text-black'>
			<table className='w-full table-fixed text-left text-sm text-black'>
				<thead className='border-b border-neutral-200 bg-neutral-100 text-sm font-semibold text-black'>
					<tr>
						<th scope='col' className='w-10 px-3 py-2.5 text-center' />
						<th
							scope='col'
							className='w-12 px-3 py-2.5 text-center font-semibold text-black'
						>
							STT
						</th>
						<th scope='col' className='px-4 py-2.5 font-semibold text-black'>
							Thời gian áp dụng
						</th>
						<th
							scope='col'
							className='px-4 py-2.5 text-right font-semibold text-black'
						>
							Vật tư mau hỏng rẻ tiền (đ/tháng)
						</th>
						<th
							scope='col'
							className='px-4 py-2.5 text-right font-semibold text-black'
						>
							Điện năng (đ/tháng)
						</th>
					</tr>
				</thead>
				<tbody className='divide-y divide-neutral-200'>
					{periods.length === 0 ? (
						<tr>
							<td colSpan={5} className='py-6 text-center text-sm text-black'>
								Chưa có khoảng thời gian nào được thiết lập.
							</td>
						</tr>
					) : (
						periods.map((period, index) => {
							const isChecked = selectedPeriodIds.includes(period.id);

							return (
								<Fragment key={period.id}>
									<tr
										className={cn(
											'border-b border-neutral-200 transition-colors hover:bg-neutral-50/80',
											isChecked && 'bg-neutral-50',
										)}
									>
										<td className='w-10 px-3 py-2 text-center'>
											<Checkbox
												checked={isChecked}
												onCheckedChange={() => onTogglePeriodSelect(period.id)}
												aria-label={`Chọn khoảng thời gian ${formatDate(period.startMonth)} - ${formatDate(period.endMonth)}`}
												className='[&_.lucide-check]:text-white'
											/>
										</td>
										<td className='w-12 px-3 py-2 text-center text-sm text-black'>
											{index + 1}
										</td>
										<td className='px-4 py-2 text-sm text-black'>
											{formatDate(period.startMonth)} -{' '}
											{formatDate(period.endMonth)}
										</td>
										<td className='px-4 py-2 text-right text-sm text-black'>
											{(
												period.lowValuePerishableSupplyUnitPrice ??
												period.lowValueSupplyUnitPrice ??
												0
											).toLocaleString('vi-VN')}
										</td>
										<td className='px-4 py-2 text-right text-sm text-black'>
											{(period.electricityUnitPrice ?? 0).toLocaleString(
												'vi-VN',
											)}
										</td>
									</tr>
								</Fragment>
							);
						})
					)}
				</tbody>
			</table>
		</div>
	);
}
