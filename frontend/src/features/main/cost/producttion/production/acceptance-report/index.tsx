import {
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
import { Spinner } from '@/components/ui/spinner';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import { formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useMemo } from 'react';
import { AcceptanceReportDataTable } from './datatable';
import { calculateTypeTotals, flattenHierarchicalData } from './utils';
import { useAcceptanceReportDetail } from './use-acceptance-report-detail';

export function AcceptanceReport({
	id,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	// Fetch data from API
	const {
		data: hierarchicalData,
		loading,
		error,
	} = useAcceptanceReportDetail(id, !!id, reloadKey);

	const { materialCost, sctxCost } = useMemo(() => {
		const normalize = (value?: string | null) =>
			(value || '')
				.trim()
				.toLowerCase()
				.replace(/đ/g, 'd')
				.normalize('NFD')
				.replace(/[\u0300-\u036f]/g, '');

		const contractedRevenueCategory = hierarchicalData?.categories.find(
			(category) =>
				normalize(category.categoryName) ===
					'vat tu tinh vao doanh thu khoan' ||
				normalize(category.categoryName) ===
					'vat tu da tinh vao doanh thu khoan',
		);

		if (!contractedRevenueCategory) {
			return { materialCost: 0, sctxCost: 0 };
		}

		const materialType = contractedRevenueCategory.types.find(
			(type) => normalize(type.typeName) === 'vat lieu',
		);
		const sparePartType = contractedRevenueCategory.types.find(
			(type) => normalize(type.typeName) === 'phu tung',
		);

		return {
			materialCost: materialType
				? calculateTypeTotals(materialType).issueForProductionAmount
				: 0,
			sctxCost: sparePartType
				? calculateTypeTotals(sparePartType).issueTotalAmount
				: 0,
		};
	}, [hierarchicalData]);

	const flattenedData = useMemo(
		() =>
			hierarchicalData ? flattenHierarchicalData(hierarchicalData) : [],
		[hierarchicalData],
	);

	return (
		<AccordionItem value={'acceptance-report'} className='border-none'>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Bảng nghiệm thu vật tư và kết chuyển chi phí</ItemTitle>
				</ItemContent>
				<ItemContent className='w-64'>
					<ItemTitle className='text-right text-sm'>
						{loading ? <Spinner /> : formatNumber(materialCost)}
					</ItemTitle>
				</ItemContent>
				<ItemContent className='w-64'>
					<ItemTitle className='text-right text-sm'>
						{loading ? <Spinner /> : formatNumber(sctxCost)}
					</ItemTitle>
				</ItemContent>
				<ItemActions>
					<AccordionTrigger
						disabled={false}
						className='group p-0 disabled:opacity-50'
					>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
					</AccordionTrigger>
				</ItemActions>
			</Item>

			{isOpen && (
				<AccordionContent className='p-0 px-2 pt-2'>
					{loading ? (
						<div className='flex justify-center py-8'>
							<Spinner />
						</div>
					) : error ? (
						<div className='flex justify-center py-8 text-red-500'>
							<p>Lỗi tải dữ liệu: {error.message}</p>
						</div>
					) : (
						<AcceptanceReportDataTable data={flattenedData} className='mt-2' />
					)}{' '}
				</AccordionContent>
			)}
		</AccordionItem>
	);
}
