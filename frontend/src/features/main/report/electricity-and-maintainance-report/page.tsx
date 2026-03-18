'use client';

import {
	Accordion,
	AccordionContent,
	AccordionItem,
} from '@/components/ui/accordion';
import { Button } from '@/components/ui/button';
import { usePopup } from '@/components/popup';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Spinner } from '@/components/ui/spinner';
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { API } from '@/constants/api-enpoint';
import { AdjustmentElectricityCost } from '@/features/main/cost/producttion/adjustment/adjustment-electricity-cost';
import { AdjustmentMaintainCost } from '@/features/main/cost/producttion/adjustment/adjustment-maintain-cost';
import { ProductionAdjustment } from '@/features/main/cost/producttion/adjustment/columns';
import {
	AdjustmentCostProductDetail,
	AdjustmentOutput,
	AdjustmentProductionOutput,
} from '@/features/main/cost/producttion/adjustment/type';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import DownloadIcon from '@mui/icons-material/Download';
import { Fragment, useCallback, useEffect, useMemo, useState } from 'react';

const UNGROUPED_PROCESS_GROUP = '__ungrouped';

const toMonthIndex = (dateValue: string | undefined) => {
	if (!dateValue) return null;

	const rawDate = dateValue.split('T')[0] ?? dateValue;
	const [yearPart, monthPart] = rawDate.split('-');

	if (yearPart && monthPart) {
		return Number(yearPart) * 12 + (Number(monthPart) - 1);
	}

	const parsedDate = new Date(dateValue);
	if (Number.isNaN(parsedDate.getTime())) {
		return null;
	}

	return parsedDate.getFullYear() * 12 + parsedDate.getMonth();
};

const isMonthWithinRange = (
	startDate: string | undefined,
	endDate: string | undefined,
	targetYear: string,
	targetMonth: string,
) => {
	const startIndex = toMonthIndex(startDate);
	const endIndex = toMonthIndex(endDate ?? startDate);
	if (startIndex === null || endIndex === null) {
		return false;
	}

	const targetIndex = Number(targetYear) * 12 + (Number(targetMonth) - 1);
	return targetIndex >= startIndex && targetIndex <= endIndex;
};

export function ElectricityAndMaintainanceReportPage() {
	const now = new Date();
	const currentYear = now.getFullYear();
	const [month, setMonth] = useState(
		String(now.getMonth() + 1).padStart(2, '0'),
	);
	const [year, setYear] = useState(String(currentYear));
	const [selectedProcessGroup, setSelectedProcessGroup] = useState('all');
	const [items, setItems] = useState<ProductionAdjustment[]>([]);
	const [loading, setLoading] = useState(false);
	const [isExporting, setIsExporting] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const popup = usePopup();

	const monthOptions = useMemo(
		() =>
			Array.from({ length: 12 }, (_, index) => {
				const value = String(index + 1).padStart(2, '0');
				return {
					value,
					label: `Tháng ${value}`,
				};
			}),
		[],
	);

	const yearOptions = useMemo(() => {
		return Array.from({ length: 101 }, (_, index) => {
			const optionYear = String(currentYear - index);
			return {
				value: optionYear,
				label: optionYear,
			};
		});
	}, [currentYear]);

	const fetchItems = useCallback(async () => {
		setLoading(true);
		setError(null);

		try {
			const response = await api.pagging<ProductionAdjustment>(
				API.COST.PRODUCT.LIST,
				{
					ignorePagination: true,
					scenarioType: 2,
				},
			);

			setItems(response.result.data ?? []);
		} catch (err) {
			setItems([]);
			setError(
				err instanceof Error
					? err.message
					: 'Không thể tải dữ liệu chi phí SCTX và điện năng điều chỉnh',
			);
		} finally {
			setLoading(false);
		}
	}, []);

	useEffect(() => {
		fetchItems();
	}, [fetchItems]);

	const periodFilteredItems = useMemo(() => {
		return items.filter((item) =>
			isMonthWithinRange(item.startMonth, item.endMonth, year, month),
		);
	}, [items, month, year]);

	const processGroupOptions = useMemo(() => {
		const groups = new Map<string, string>();

		periodFilteredItems.forEach((item) => {
			const value = item.processGroupId || UNGROUPED_PROCESS_GROUP;
			const label = item.processGroupCode || 'Không có nhóm công đoạn';

			if (!groups.has(value)) {
				groups.set(value, label);
			}
		});

		const dynamicGroups = Array.from(groups.entries())
			.map(([value, label]) => ({ value, label }))
			.sort((a, b) => a.label.localeCompare(b.label, 'vi'));

		return [{ value: 'all', label: 'Tất cả nhóm công đoạn' }, ...dynamicGroups];
	}, [periodFilteredItems]);

	useEffect(() => {
		if (
			!processGroupOptions.some(
				(option) => option.value === selectedProcessGroup,
			)
		) {
			setSelectedProcessGroup('all');
		}
	}, [processGroupOptions, selectedProcessGroup]);

	const filteredItems = useMemo(() => {
		if (selectedProcessGroup === 'all') {
			return periodFilteredItems;
		}

		if (selectedProcessGroup === UNGROUPED_PROCESS_GROUP) {
			return periodFilteredItems.filter((item) => !item.processGroupId);
		}

		return periodFilteredItems.filter(
			(item) => item.processGroupId === selectedProcessGroup,
		);
	}, [periodFilteredItems, selectedProcessGroup]);

	const handleExport = async () => {
		try {
			setIsExporting(true);
			const filename = await api.export(API.COST.PRODUCT.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (err) {
			popup.error(err);
		} finally {
			setIsExporting(false);
		}
	};

	return (
		<div className='relative flex min-h-0 min-w-0 flex-1 flex-col gap-3'>
			<div className='flex flex-wrap items-end justify-between gap-3'>
				<div className='flex flex-wrap items-end gap-2'>
					<div className='space-y-1'>
						<p className='text-sm font-medium'>Tháng</p>
						<Select value={month} onValueChange={setMonth}>
							<SelectTrigger className='w-[150px] bg-white'>
								<SelectValue placeholder='Chọn tháng' />
							</SelectTrigger>
							<SelectContent>
								{monthOptions.map((option) => (
									<SelectItem key={option.value} value={option.value}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>

					<div className='space-y-1'>
						<p className='text-sm font-medium'>Năm</p>
						<Select value={year} onValueChange={setYear}>
							<SelectTrigger className='w-[120px] bg-white'>
								<SelectValue placeholder='Chọn năm' />
							</SelectTrigger>
							<SelectContent className='max-h-64'>
								{yearOptions.map((option) => (
									<SelectItem key={option.value} value={option.value}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>

					<div className='space-y-1'>
						<p className='text-sm font-medium'>Nhóm công đoạn</p>
						<Select
							value={selectedProcessGroup}
							onValueChange={setSelectedProcessGroup}
						>
							<SelectTrigger className='w-[260px] bg-white'>
								<SelectValue placeholder='Chọn nhóm công đoạn' />
							</SelectTrigger>
							<SelectContent className='max-h-64'>
								{processGroupOptions.map((option) => (
									<SelectItem key={option.value} value={option.value}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>
				</div>

				<Button
					variant='outline'
					size='sm'
					className='h-10 gap-1.5'
					disabled={loading || isExporting}
					onClick={handleExport}
				>
					{isExporting ? (
						<Spinner />
					) : (
						<>
							<DownloadIcon style={{ fontSize: 18 }} />
							<span>Xuất file</span>
						</>
					)}
				</Button>
			</div>

			{error ? (
				<div className='border-border flex min-h-48 items-center justify-center rounded-t-md border bg-white shadow'>
					<div className='text-muted-foreground text-center'>
						<p className='text-lg font-medium'>Lỗi tải dữ liệu</p>
						<p className='text-sm'>{error}</p>
					</div>
				</div>
			) : loading ? (
				<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
					<Spinner />
				</div>
			) : (
				<ElectricityAndMaintainanceReportTable
					items={filteredItems}
					month={month}
					year={year}
				/>
			)}
		</div>
	);
}

type ElectricityAndMaintainanceReportTableProps = {
	items: ProductionAdjustment[];
	month: string;
	year: string;
};

function ElectricityAndMaintainanceReportTable({
	items,
	month,
	year,
}: ElectricityAndMaintainanceReportTableProps) {
	if (items.length === 0) {
		return (
			<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
				<div className='text-muted-foreground text-center'>
					<p className='text-lg font-medium'>Không có dữ liệu</p>
					<p className='text-sm'>Không tìm thấy dữ liệu phù hợp bộ lọc</p>
				</div>
			</div>
		);
	}

	return (
		<div className='overflow-hidden rounded-t-md border bg-white shadow'>
			<Table>
				<TableHeader className='bg-[#fafafa] text-base'>
					<TableRow className='h-14'>
						<TableHead className='p-0 hover:bg-[#f0f0f0]'>
							<div className='inline-flex w-full px-4 py-2 font-bold'>
								Mã sản phẩm
							</div>
						</TableHead>
						<TableHead className='p-0 hover:bg-[#f0f0f0]'>
							<div className='inline-flex w-full px-4 py-2 font-bold'>
								Tên sản phẩm
							</div>
						</TableHead>
						<TableHead className='p-0 hover:bg-[#f0f0f0]'>
							<div className='inline-flex w-full px-4 py-2 font-bold'>
								Mã nhóm CĐSX
							</div>
						</TableHead>
						<TableHead className='p-0 text-center hover:bg-[#f0f0f0]'>
							<div className='inline-flex w-full items-center justify-center px-4 py-2 font-bold'>
								Xem
							</div>
						</TableHead>
					</TableRow>
				</TableHeader>

				<TableBody>
					{items.map((item) => (
						<ReportRow key={item.id} item={item} month={month} year={year} />
					))}
				</TableBody>
			</Table>
		</div>
	);
}

function ReportRow({
	item,
	month,
	year,
}: {
	item: ProductionAdjustment;
	month: string;
	year: string;
}) {
	const [expanded, setExpanded] = useState(false);

	return (
		<Fragment>
			<TableRow className='hover:bg-muted/30 h-18'>
				<TableCell className='px-4 py-2'>{item.productCode}</TableCell>
				<TableCell className='px-4 py-2'>{item.productName}</TableCell>
				<TableCell className='px-4 py-2'>{item.processGroupCode}</TableCell>
				<TableCell className='px-4 py-2 text-center'>
					<button
						type='button'
						className='text-muted-foreground hover:text-foreground cursor-pointer'
						onClick={() => setExpanded((value) => !value)}
					>
						{expanded ? <VisibilityOffIcon /> : <VisibilityIcon />}
					</button>
				</TableCell>
			</TableRow>

			{expanded && (
				<TableRow className='bg-[#fcfcfc] hover:bg-[#fcfcfc]'>
					<TableCell colSpan={4} className='px-0 py-3'>
						<AdjustmentReportExpand row={item} month={month} year={year} />
					</TableCell>
				</TableRow>
			)}
		</Fragment>
	);
}

function AdjustmentReportExpand({
	row,
	month,
	year,
}: {
	row: ProductionAdjustment;
	month: string;
	year: string;
}) {
	const [adjustment, setAdjustment] = useState<AdjustmentCostProductDetail>();
	const [opened, setOpened] = useState<string[]>([]);
	const [loading, setLoading] = useState<boolean>(true);

	useEffect(() => {
		api
			.get<AdjustmentCostProductDetail>(
				API.COST.PRODUCT.DETAIL_ADJUSTMENT(row.id),
			)
			.then((res) => {
				setAdjustment(res.result);
			})
			.finally(() => setLoading(false));
	}, [row.id]);

	const hasOutputs = adjustment?.outputs && adjustment.outputs.length > 0;
	const displayItems = hasOutputs
		? adjustment?.outputs
		: adjustment?.productionOutputs;
	const matchedItems =
		displayItems?.filter((item) =>
			isMonthWithinRange(item.startMonth, item.endMonth, year, month),
		) ?? [];

	if (loading) {
		return (
			<div className='mx-2 flex h-10 items-center gap-8 rounded-sm bg-[#e5e7eb] px-4 py-2 hover:no-underline'>
				<Skeleton className='bg-muted-foreground h-4 w-full rounded-full' />
				<Spinner />
			</div>
		);
	}

	if (!matchedItems.length) {
		return (
			<div className='text-muted-foreground px-4 py-3 text-sm'>
				Không có dữ liệu chi tiết.
			</div>
		);
	}

	return (
		<Accordion type='multiple' className='mx-2 space-y-4'>
			{matchedItems.map((item) => {
				const matchedProductionOutput = hasOutputs
					? adjustment?.productionOutputs.find(
							(po) =>
								po.startMonth === item.startMonth &&
								po.endMonth === item.endMonth,
						)
					: (item as AdjustmentProductionOutput);

				return (
					<AccordionItem
						key={`${item.id}-${formatDate(item.startMonth)}`}
						value={item.id}
						className='border-none'
					>
						<AccordionContent forceMount className='p-0'>
							<Accordion
								type='multiple'
								className='flex flex-col gap-2 px-2'
								value={opened}
								onValueChange={setOpened}
							>
								<AdjustmentMaintainCost
									id={item.id}
									adjustment={adjustment}
									output={hasOutputs ? (item as AdjustmentOutput) : undefined}
									productionOutput={matchedProductionOutput}
									callback={async () => {}}
									isOpen={opened.includes('adjustment-maintain-cost')}
									multiplyByProductionMeters={false}
								/>

								<AdjustmentElectricityCost
									id={item.id}
									adjustment={adjustment}
									output={hasOutputs ? (item as AdjustmentOutput) : undefined}
									productionOutput={matchedProductionOutput}
									callback={async () => {}}
									isOpen={opened.includes('adjustment-electricity-cost')}
									multiplyByProductionMeters={false}
								/>
							</Accordion>
						</AccordionContent>
					</AccordionItem>
				);
			})}
		</Accordion>
	);
}
