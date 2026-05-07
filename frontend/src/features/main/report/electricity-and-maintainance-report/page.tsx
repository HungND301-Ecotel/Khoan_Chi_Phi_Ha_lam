'use client';

import { Button } from '@/components/ui/button';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { Spinner } from '@/components/ui/spinner';
import {
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { ProductionAdjustment } from '@/features/main/cost/producttion/adjustment/columns';
import {
	AdjustmentMaintainCostDetail,
	AdjustmentMaintainCostItem,
} from '@/features/main/cost/producttion/adjustment/adjustment-maintain-cost/columns';
import { AdjustmentElectricityCostDetailCost } from '@/features/main/cost/producttion/adjustment/adjustment-electricity-cost/types';
import {
	AdjustmentCostProductDetail,
	AdjustmentOutput,
	AdjustmentProductionOutput,
} from '@/features/main/cost/producttion/adjustment/type';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import {
	useCallback,
	useEffect,
	useMemo,
	useState,
	type ReactNode,
} from 'react';

const UNGROUPED_PROCESS_GROUP = '__ungrouped';

type EquipmentReportRow = {
	key: string;
	equipmentName: string;
	unitOfMeasureName: string;
	quantity: number;
	kValues: number[];
	maintainUnitPrice?: number;
	maintainTotalPrice?: number;
	electricityUnitPrice?: number;
	electricityTotalPrice?: number;
};

type ProductReportBlock = {
	key: string;
	processGroupLabel: string;
	productName: string;
	productUnitLabel: string;
	productionMeters: number;
	rows: EquipmentReportRow[];
};

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

const extractMaintainFactors = (item?: AdjustmentMaintainCostItem) => {
	if (!item) return undefined;
	const sortedDescriptions = [
		...(item.adjustmentFactorDescriptions ?? []),
	].sort((a, b) =>
		a.adjustmentFactorCode.localeCompare(b.adjustmentFactorCode),
	);

	return [
		sortedDescriptions[0]?.effectiveValue ?? 1,
		sortedDescriptions[1]?.effectiveValue ?? 1,
		sortedDescriptions[2]?.effectiveValue ?? 1,
		sortedDescriptions[3]?.effectiveValue ?? 1,
		sortedDescriptions[4]?.effectiveValue ?? 1,
		item.k6AdjustmentFactorValue ?? 1,
		sortedDescriptions[5]?.effectiveValue ?? 1,
	];
};

const extractElectricityFactors = (
	item?: AdjustmentElectricityCostDetailCost,
) => {
	if (!item) return undefined;
	const sortedDescriptions = [
		...(item.adjustmentFactorDescriptions ?? []),
	].sort((a, b) =>
		a.adjustmentFactorCode.localeCompare(b.adjustmentFactorCode),
	);

	// Electricity currently has fewer K factors than maintain.
	// Missing factors are normalized to 1 for report display.
	return [
		sortedDescriptions[0]?.effectiveValue ?? 1,
		sortedDescriptions[1]?.effectiveValue ?? 1,
		sortedDescriptions[2]?.effectiveValue ?? 1,
		1,
		1,
		1,
		1,
	];
};

const formatPeriodLabel = (month: string, year: string) => {
	return `Tháng ${Number(month)} năm ${year}`;
};

const formatDateString = (date: Date) => {
	return `${date.getDate().toString().padStart(2, '0')} tháng ${(
		date.getMonth() + 1
	)
		.toString()
		.padStart(2, '0')} năm ${date.getFullYear()}`;
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
	const [reportBlocks, setReportBlocks] = useState<ProductReportBlock[]>([]);
	const [loading, setLoading] = useState(false);
	const [loadingDetails, setLoadingDetails] = useState(false);
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
					: 'Không thể tải dữ liệu doanh thu SCTX và điện năng điều chỉnh',
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
			const label =
				[item.processGroupCode, item.processGroupName]
					.filter(Boolean)
					.join(' - ') || 'Không có nhóm công đoạn';

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

	useEffect(() => {
		let cancelled = false;

		const buildReportBlocks = async () => {
			setLoadingDetails(true);
			try {
				const blocks = await Promise.all(
					filteredItems.map(async (item) => {
						const detail = await api.get<AdjustmentCostProductDetail>(
							API.COST.PRODUCT.DETAIL_ADJUSTMENT(item.id),
						);
						const detailData = detail.result;
						const hasOutputs =
							detailData.outputs && detailData.outputs.length > 0;
						const displayItems = hasOutputs
							? detailData.outputs
							: detailData.productionOutputs;
						const matchedItems =
							displayItems?.filter((output) =>
								isMonthWithinRange(
									output.startMonth,
									output.endMonth,
									year,
									month,
								),
							) ?? [];

						if (!matchedItems.length) {
							return [] as ProductReportBlock[];
						}

						const outputBlocks = await Promise.all(
							matchedItems.map(async (matchedOutput) => {
								const outputId = matchedOutput.id;
								const [maintainRes, electricityRes] = await Promise.all([
									api
										.get<AdjustmentMaintainCostDetail>(
											API.COST.ADJUSTMENT_MAINTAIN.DETAIL(outputId),
										)
										.catch(() => ({
											result: {
												id: '',
												productUnitPriceId: '',
												outputId,
												akRate: 0,
												akRatePercent: 0,
												costs: [],
											} satisfies AdjustmentMaintainCostDetail,
										})),
									api
										.get<{
											costs: AdjustmentElectricityCostDetailCost[];
										}>(API.COST.ADJUSTMENT_ELECTRICITY.DETAIL(outputId))
										.catch(() => ({ result: { costs: [] } })),
								]);

								const maintainCosts = maintainRes.result?.costs ?? [];
								const electricityCosts = electricityRes.result?.costs ?? [];

								const maintainByEquipment = new Map(
									maintainCosts.map((cost) => [cost.equipmentId, cost]),
								);
								const electricityByEquipment = new Map(
									electricityCosts.map((cost) => [cost.equipmentId, cost]),
								);
								const equipmentIds = Array.from(
									new Set([
										...maintainByEquipment.keys(),
										...electricityByEquipment.keys(),
									]),
								);

								const rows: EquipmentReportRow[] = equipmentIds.map(
									(equipmentId) => {
										const maintainCost = maintainByEquipment.get(equipmentId);
										const electricityCost =
											electricityByEquipment.get(equipmentId);
										const maintainFactors =
											extractMaintainFactors(maintainCost);
										const electricityFactors =
											extractElectricityFactors(electricityCost);

										return {
											key: `${outputId}-${equipmentId}`,
											equipmentName:
												maintainCost?.equipmentName ||
												electricityCost?.equipmentName ||
												'',
											unitOfMeasureName: 'Cái',
											quantity:
												maintainCost?.quantity ??
												electricityCost?.quantity ??
												0,
											kValues: maintainFactors ??
												electricityFactors ?? [1, 1, 1, 1, 1, 1, 1],
											maintainUnitPrice: maintainCost?.maintainUnitPrice,
											maintainTotalPrice: maintainCost?.totalPrice,
											electricityUnitPrice:
												electricityCost?.electricityUnitPrice,
											electricityTotalPrice: electricityCost?.totalPrice,
										};
									},
								);

								const matchedProductionOutput = hasOutputs
									? detailData.productionOutputs.find(
											(po) =>
												po.startMonth === matchedOutput.startMonth &&
												po.endMonth === matchedOutput.endMonth,
										)
									: (matchedOutput as AdjustmentProductionOutput);

								const productionMeters =
									(matchedOutput as AdjustmentOutput).productionMeters ??
									matchedProductionOutput?.productionMeters ??
									item.totalProductionMeters ??
									0;

								return {
									key: `${item.id}-${outputId}`,
									processGroupLabel:
										[item.processGroupCode, item.processGroupName]
											.filter(Boolean)
											.join(' - ') || 'Chưa phân nhóm',
									productName: item.productName,
									productUnitLabel:
										item.fixedKeyType === 1
											? 'Mét'
											: item.fixedKeyType === 2
												? 'Tấn'
												: '',
									productionMeters,
									rows,
								};
							}),
						);

						return outputBlocks.filter((block) => block.rows.length > 0);
					}),
				);

				if (!cancelled) {
					setReportBlocks(blocks.flat());
				}
			} finally {
				if (!cancelled) {
					setLoadingDetails(false);
				}
			}
		};

		buildReportBlocks();

		return () => {
			cancelled = true;
		};
	}, [filteredItems, month, year]);

	const handleExport = async () => {
		if (!filteredItems.length) {
			popup.error('Không có dữ liệu để xuất file');
			return;
		}

		try {
			setIsExporting(true);
			const processGroupId =
				selectedProcessGroup !== 'all' &&
				selectedProcessGroup !== UNGROUPED_PROCESS_GROUP
					? selectedProcessGroup
					: undefined;

			const fileName = `bang-tinh-don-gia-sctx-va-dien-nang-thang-${month}-nam-${year}.xlsx`;

			await api.export(
				`${API.COST.PRODUCT.EXPORT_ADJUSTMENT_ELECTRICITY_MAINTAIN_REPORT}?${new URLSearchParams(
					Object.entries({
						month,
						year,
						scenarioType: '2',
						...(processGroupId ? { processGroupId } : {}),
					}) as [string, string][],
				).toString()}`,
				{
					fileName,
					forceFileName: true,
				},
			);

			popup.success(`Đã xuất file ${fileName}`);
		} catch (err) {
			popup.error(err);
		} finally {
			setIsExporting(false);
		}
	};

	const groupedBlocks = useMemo(() => {
		const groups = new Map<string, ProductReportBlock[]>();
		reportBlocks.forEach((block) => {
			const key = block.processGroupLabel || 'Chưa phân nhóm';
			if (!groups.has(key)) {
				groups.set(key, []);
			}
			groups.get(key)?.push(block);
		});
		return Array.from(groups.entries());
	}, [reportBlocks]);

	return (
		<div className='relative flex min-h-0 min-w-0 flex-1 flex-col gap-3'>
			<div className='flex flex-wrap items-end justify-between gap-3'>
				<div className='flex flex-wrap items-end gap-2'>
					<div className='space-y-1'>
						<p className='text-sm font-medium'>Tháng</p>
						<Select value={month} onValueChange={setMonth}>
							<SelectTrigger className='w-37.5 bg-white'>
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
							<SelectTrigger className='w-30 bg-white'>
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
							<SelectTrigger className='w-65 bg-white'>
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
					disabled={loading || loadingDetails || isExporting}
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
			) : loading || loadingDetails ? (
				<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
					<Spinner />
				</div>
			) : (
				<div className='rounded-md border bg-[#e6e6e6] p-3 md:p-4'>
					<div className='mx-auto w-full'>
						<div className='mx-auto min-h-[210mm] bg-white p-3 shadow-[0_8px_30px_rgba(0,0,0,0.14)] md:p-5'>
							<ElectricityMaintainHeader month={month} year={year} />
							<div className='mt-6 border border-black'>
								{reportBlocks.length === 0 ? (
									<div className='py-12 text-center text-sm'>
										Không có dữ liệu
									</div>
								) : (
									<table className='w-full table-fixed border-collapse font-["Times_New_Roman",Times,serif] text-[12px] [&_td]:wrap-break-word [&_td]:whitespace-normal! [&_th]:wrap-break-word [&_th]:whitespace-normal!'>
										<TableHeader>
											<TableRow className='bg-white'>
												<ReportHead rowSpan={2}>STT</ReportHead>
												<ReportHead rowSpan={2}>Tên sản phẩm</ReportHead>
												<ReportHead rowSpan={2}>ĐVT</ReportHead>
												<ReportHead rowSpan={2}>Sản lượng</ReportHead>
												<ReportHead rowSpan={2}>Tên chủng loại</ReportHead>
												<ReportHead rowSpan={2}>ĐVT</ReportHead>
												<ReportHead rowSpan={2}>Số lượng</ReportHead>
												<ReportHead rowSpan={2}>K1</ReportHead>
												<ReportHead rowSpan={2}>K2</ReportHead>
												<ReportHead rowSpan={2}>K3</ReportHead>
												<ReportHead rowSpan={2}>K4</ReportHead>
												<ReportHead rowSpan={2}>K5</ReportHead>
												<ReportHead rowSpan={2}>K6</ReportHead>
												<ReportHead rowSpan={2}>K7</ReportHead>
												<ReportHead colSpan={2}>SCTX</ReportHead>
												<ReportHead colSpan={2}>ĐIỆN NĂNG</ReportHead>
											</TableRow>
											<TableRow className='bg-white'>
												<ReportHead>Đơn giá</ReportHead>
												<ReportHead>Thành tiền</ReportHead>
												<ReportHead>Đơn giá</ReportHead>
												<ReportHead>Thành tiền</ReportHead>
											</TableRow>
										</TableHeader>
										<TableBody>
											{groupedBlocks.map(([groupLabel, blocks], groupIndex) => (
												<ReportGroupRows
													key={groupLabel}
													groupLabel={groupLabel}
													groupIndex={groupIndex}
													blocks={blocks}
												/>
											))}
										</TableBody>
									</table>
								)}
							</div>

							<ElectricityMaintainFooter />
						</div>
					</div>
				</div>
			)}
		</div>
	);
}

function ElectricityMaintainHeader({
	month,
	year,
}: {
	month: string;
	year: string;
}) {
	return (
		<div className='font-["Times_New_Roman",Times,serif]'>
			<div className='flex items-start justify-between gap-10'>
				<div className='space-y-1 text-left font-bold'>
					<p className='text-base leading-tight md:text-lg'>
						CÔNG TY CP THAN HÀ LẦM - VINACOMIN
					</p>
					<p className='border-b border-black pb-1 text-center text-sm leading-tight md:text-base'>
						CÔNG TRƯỜNG KHAI THÁC 1
					</p>
				</div>
				<div className='space-y-1 pt-1 text-right text-sm font-bold md:text-base'>
					<p>ĐVT: Đồng</p>
					<p>Bảng số: 02</p>
				</div>
			</div>

			<div className='mt-4 text-center'>
				<p className='text-lg font-bold uppercase md:text-2xl'>
					Bảng tính đơn giá SCTX và điện năng
				</p>
				<p className='mt-2 text-base font-bold md:text-xl'>
					{formatPeriodLabel(month, year)}
				</p>
			</div>
		</div>
	);
}

function ElectricityMaintainFooter() {
	return (
		<div className='mt-10 font-["Times_New_Roman",Times,serif] text-[14px] md:text-[16px]'>
			<div className='mb-3 flex justify-end'>
				<p className='pr-8 text-right font-semibold italic'>
					Hà Lầm, ngày {formatDateString(new Date())}
				</p>
			</div>

			<div className='grid grid-cols-2 gap-12 font-semibold'>
				<div className='grid grid-cols-2 text-center'>
					<p className='col-span-2'>ĐẠI DIỆN BÊN NHẬN KHOÁN</p>
					<p className='mt-2'>NGƯỜI LẬP BIỂU</p>
					<p className='mt-2'>QUẢN ĐỐC</p>
				</div>

				<div className='grid grid-cols-1 text-center'>
					<p>ĐẠI DIỆN BÊN GIAO KHOÁN</p>
					<p className='mt-2'>PHÒNG KẾ HOẠCH</p>
				</div>
			</div>
		</div>
	);
}

function ReportHead({
	children,
	rowSpan,
	colSpan,
}: {
	children: ReactNode;
	rowSpan?: number;
	colSpan?: number;
}) {
	return (
		<TableHead
			rowSpan={rowSpan}
			colSpan={colSpan}
			className='h-auto border border-black px-1 py-1.5 text-center text-[11px] leading-tight font-bold wrap-break-word whitespace-normal text-black'
		>
			{children}
		</TableHead>
	);
}

function ReportGroupRows({
	groupLabel,
	groupIndex,
	blocks,
}: {
	groupLabel: string;
	groupIndex: number;
	blocks: ProductReportBlock[];
}) {
	return (
		<>
			<TableRow className='bg-white'>
				<TableCell
					colSpan={18}
					className='border border-black px-2 py-1 font-bold uppercase'
				>
					{toRoman(groupIndex + 1)}. {groupLabel}
				</TableCell>
			</TableRow>

			{blocks.map((block, blockIndex) =>
				block.rows.map((row, rowIndex) => {
					const isFirstRow = rowIndex === 0;
					const rowSpan = block.rows.length;
					return (
						<TableRow key={row.key} className='bg-white'>
							{isFirstRow && (
								<TableCell
									rowSpan={rowSpan}
									className='border border-black px-1 py-1 text-center align-top'
								>
									{blockIndex + 1}
								</TableCell>
							)}
							{isFirstRow && (
								<TableCell
									rowSpan={rowSpan}
									className='border border-black px-1 py-1 align-top'
								>
									{block.productName}
								</TableCell>
							)}
							{isFirstRow && (
								<TableCell
									rowSpan={rowSpan}
									className='border border-black px-1 py-1 text-center align-top'
								>
									{block.productUnitLabel}
								</TableCell>
							)}
							{isFirstRow && (
								<TableCell
									rowSpan={rowSpan}
									className='border border-black px-1 py-1 text-center align-top'
								>
									{formatNumber(block.productionMeters)}
								</TableCell>
							)}
							<TableCell className='border border-black px-1 py-1'>
								{row.equipmentName}
							</TableCell>
							<TableCell className='border border-black px-1 py-1 text-center'>
								{row.unitOfMeasureName}
							</TableCell>
							<TableCell className='border border-black px-1 py-1 text-center'>
								{formatNumber(row.quantity)}
							</TableCell>

							{row.kValues.map((value, kIndex) => (
								<TableCell
									key={`${row.key}-k-${kIndex + 1}`}
									className='border border-black px-1 py-1 text-center'
								>
									{formatNumber(value)}
								</TableCell>
							))}

							<TableCell className='border border-black px-1 py-1 text-right'>
								{row.maintainUnitPrice == null
									? ''
									: formatNumber(row.maintainUnitPrice)}
							</TableCell>
							<TableCell className='border border-black px-1 py-1 text-right'>
								{row.maintainTotalPrice == null
									? ''
									: formatNumber(row.maintainTotalPrice)}
							</TableCell>
							<TableCell className='border border-black px-1 py-1 text-right'>
								{row.electricityUnitPrice == null
									? ''
									: formatNumber(row.electricityUnitPrice)}
							</TableCell>
							<TableCell className='border border-black px-1 py-1 text-right'>
								{row.electricityTotalPrice == null
									? ''
									: formatNumber(row.electricityTotalPrice)}
							</TableCell>
						</TableRow>
					);
				}),
			)}
		</>
	);
}

function toRoman(value: number) {
	const romans = [
		['M', 1000],
		['CM', 900],
		['D', 500],
		['CD', 400],
		['C', 100],
		['XC', 90],
		['L', 50],
		['XL', 40],
		['X', 10],
		['IX', 9],
		['V', 5],
		['IV', 4],
		['I', 1],
	] as const;
	let number = value;
	let result = '';
	for (const [roman, arabic] of romans) {
		while (number >= arabic) {
			result += roman;
			number -= arabic;
		}
	}
	return result;
}
