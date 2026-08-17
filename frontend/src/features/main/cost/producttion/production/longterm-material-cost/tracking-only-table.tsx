import { formatNumber } from '@/lib/utils';
import { LongTermTrackingOnlyItem } from './types';

type TrackingOnlyTableProps = {
	items: LongTermTrackingOnlyItem[];
};

// Vật tư vừa tick "Xuất khác" vừa có "Bổ sung chi phí" (cả 2 điều kiện đi cùng nhau) — chỉ mang
// tính theo dõi, không cộng vào bất kỳ giá trị hạch toán/phân bổ nào của Bảng hạch toán chi phí
// vật tư dài kỳ. Tách hẳn thành bảng riêng (6 cột đơn giản) thay vì nhét vào bảng chính, tránh
// phải bỏ trống hàng loạt cột hạch toán (Nguyên giá, Thời gian sử dụng, Tỷ lệ phân bổ...) vốn
// không áp dụng cho các dòng này.
export function TrackingOnlyTable({ items }: TrackingOnlyTableProps) {
	if (!items || items.length === 0) {
		return null;
	}

	return (
		<div className='mt-2 flex flex-col gap-1.5'>
			<div className='text-black-500 text-xs font-semibold uppercase'>
				Vật tư theo dõi
			</div>
			<div className='overflow-hidden rounded-md border border-gray-200'>
				<table className='w-full text-left text-sm'>
					<thead className='border-black-200 text-black-600 border-b bg-gray-50 text-xs font-semibold uppercase'>
						<tr>
							<th className='px-3 py-2'>Mã vật tư</th>
							<th className='px-3 py-2'>Tên vật tư</th>
							<th className='px-3 py-2'>ĐVT</th>
							<th className='px-3 py-2'>Số lượng</th>
							<th className='px-3 py-2'>Loại</th>
							<th className='px-3 py-2'>Ghi chú</th>
						</tr>
					</thead>
					<tbody className='divide-y divide-gray-100'>
						{items.map((item, index) => (
							<tr
								key={`${item.acceptanceReportItemId}-${item.trackingType}-${index}`}
							>
								<td className='px-3 py-2'>{item.materialCode || '-'}</td>
								<td className='px-3 py-2'>{item.materialName || '-'}</td>
								<td className='px-3 py-2'>{item.unitOfMeasureName || '-'}</td>
								<td className='px-3 py-2'>
									{formatNumber(item.quantity ?? 0)}
								</td>
								<td className='px-3 py-2'>{item.trackingType}</td>
								<td className='px-3 py-2'>{item.note || '-'}</td>
							</tr>
						))}
					</tbody>
				</table>
			</div>
		</div>
	);
}
