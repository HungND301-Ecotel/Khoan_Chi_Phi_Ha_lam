import { ActionDialogProps } from '@/components/datatable';
import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { ProductionAdjustment } from '@/features/main/cost/producttion/adjustment/columns';
import { formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useState } from 'react';

type VtlAdjustmentExpandProps = ActionDialogProps<ProductionAdjustment> & {
	monthId?: string;
};

const renderPrice = (val?: number | null) => {
	if (val === null || val === undefined || val === 0) return '-';
	return formatNumber(val);
};

const renderFactor = (coeff?: number | null) => {
	if (coeff === null || coeff === undefined) return '-';
	return formatNumber(coeff);
};

// Song song VtlPlanExpand bên Kế hoạch sản xuất — đơn giá gốc/K1/K2/đơn giá hiệu lực giữ nguyên
// từ Kế hoạch ban đầu, chỉ khác Sản lượng (thực tế) và Doanh thu (điều chỉnh).
export function VtlAdjustmentExpand({ row }: VtlAdjustmentExpandProps) {
	const [opened, setOpened] = useState<string[]>(['vtl-adjustment']);
	const product = (row as any)?.original ?? row;

	const material = product?.material;
	const maintenance = product?.maintenance;
	const power = product?.power;

	return (
		<Accordion
			type='multiple'
			className='mx-2 space-y-2'
			value={opened}
			onValueChange={setOpened}
		>
			<AccordionItem
				value='vtl-adjustment'
				className='rounded-sm border-none bg-[#f5f5f5]'
			>
				<Item
					variant='outline'
					className='flex-1 justify-between py-2 text-sm font-normal'
				>
					<ItemContent>
						<ItemTitle className='text-sm font-medium text-gray-800'>
							Doanh thu điều chỉnh
						</ItemTitle>
					</ItemContent>

					<div className='flex items-center gap-12 text-sm font-semibold text-gray-700'>
						<div>{formatNumber(product?.totalProductionMeters ?? 0)}</div>
						<div>{formatNumber(product?.adjustmentTotalCost ?? 0)}</div>

						<ItemActions className='flex items-center gap-2'>
							<AccordionTrigger className='group p-0 hover:no-underline'>
								<div className='group-data-[state=open]:hidden'>
									<VisibilityIcon className='size-5 text-gray-600' />
								</div>
								<div className='hidden group-data-[state=open]:block'>
									<VisibilityOffIcon className='size-5 text-gray-600' />
								</div>
							</AccordionTrigger>
						</ItemActions>
					</div>
				</Item>

				<AccordionContent className='p-4 pt-2'>
					<div className='overflow-x-auto rounded-md border border-gray-200 bg-white p-4 shadow-xs'>
						<table className='w-full text-left text-sm text-gray-700'>
							<thead className='bg-gray-50 text-xs font-semibold uppercase text-gray-600'>
								<tr>
									<th className='px-4 py-2'>Thành phần chi phí</th>
									<th className='px-4 py-2 text-right'>Đơn giá gốc (đ)</th>
									<th className='px-4 py-2 text-center'>K1</th>
									<th className='px-4 py-2 text-center'>K2</th>
									<th className='px-4 py-2 text-right'>Đơn giá (đ)</th>
								</tr>
							</thead>
							<tbody className='divide-y divide-gray-200'>
								<tr>
									<td className='px-4 py-2 font-medium'>Vật liệu</td>
									<td className='px-4 py-2 text-right'>
										{renderPrice(material?.baseUnitPrice)}
									</td>
									<td className='px-4 py-2 text-center'>-</td>
									<td className='px-4 py-2 text-center'>-</td>
									<td className='px-4 py-2 text-right'>
										{renderPrice(material?.effectiveUnitPrice)}
									</td>
								</tr>
								<tr>
									<td className='px-4 py-2 font-medium'>
										Sửa chữa thường xuyên (SCTX)
									</td>
									<td className='px-4 py-2 text-right'>
										{renderPrice(maintenance?.baseUnitPrice)}
									</td>
									<td className='px-4 py-2 text-center'>
										{renderFactor(maintenance?.k1Coefficient)}
									</td>
									<td className='px-4 py-2 text-center'>
										{renderFactor(maintenance?.k2Coefficient)}
									</td>
									<td className='px-4 py-2 text-right'>
										{renderPrice(maintenance?.effectiveUnitPrice)}
									</td>
								</tr>
								<tr>
									<td className='px-4 py-2 font-medium'>
										Động lực (Điện năng / Nhiên liệu)
									</td>
									<td className='px-4 py-2 text-right'>
										{renderPrice(power?.baseUnitPrice)}
									</td>
									<td className='px-4 py-2 text-center'>
										{renderFactor(power?.k1Coefficient)}
									</td>
									<td className='px-4 py-2 text-center'>
										{renderFactor(power?.k2Coefficient)}
									</td>
									<td className='px-4 py-2 text-right'>
										{renderPrice(power?.effectiveUnitPrice)}
									</td>
								</tr>
							</tbody>
						</table>
					</div>
				</AccordionContent>
			</AccordionItem>
		</Accordion>
	);
}
