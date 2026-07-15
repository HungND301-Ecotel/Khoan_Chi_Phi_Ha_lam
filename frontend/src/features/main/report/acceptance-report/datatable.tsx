'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { Production } from '@/features/main/cost/producttion/production/columns';
import { ProductionOutputDto } from '@/features/main/cost/producttion/production/acceptance-report/api-types';
import {
	applyProductionOrderNames,
	type ProductionOrderDisplayInfo,
	transformApiResponseToHierarchical,
} from '@/features/main/cost/producttion/production/acceptance-report/api-transformer';
import { AcceptanceReportDataTable as AcceptanceReportGrid } from '@/features/main/cost/producttion/production/acceptance-report/datatable';
import { HierarchicalRow } from '@/features/main/cost/producttion/production/acceptance-report/types';
import { flattenHierarchicalData } from '@/features/main/cost/producttion/production/acceptance-report/utils';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import SearchIcon from '@mui/icons-material/Search';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';

const isSameMonthAndYear = (
	dateValue: string | undefined,
	targetYear: string,
	targetMonth: string,
) => {
	if (!dateValue) return false;

	const rawDate = dateValue.split('T')[0] ?? dateValue;
	const [yearPart, monthPart] = rawDate.split('-');

	if (yearPart && monthPart) {
		return yearPart === targetYear && monthPart === targetMonth;
	}

	const parsedDate = new Date(dateValue);
	if (Number.isNaN(parsedDate.getTime())) {
		return false;
	}

	return (
		String(parsedDate.getFullYear()) === targetYear &&
		String(parsedDate.getMonth() + 1).padStart(2, '0') === targetMonth
	);
};

interface AcceptanceReportDataTableProps {
	enableSearch?: boolean;
	largeText?: boolean;
}

type ProductionOrderLookupDto = {
	id: string;
	code: string;
	name: string;
};

const formatPeriodLabel = (month: string, year: string) => {
	return `THÁNG ${Number(month)} NĂM ${year}`;
};

const ExcelAcceptanceReportHeader = ({
	month,
	year,
}: {
	month: string;
	year: string;
}) => {
	return (
		<div className='font-["Times_New_Roman",Times,serif]'>
			<div className='inline-block text-left font-bold'>
				<p className='text-base leading-tight md:text-lg'>
					CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN
				</p>
				<p className='w-full text-center text-sm leading-tight md:text-base'>
					CÔNG TRƯỜNG KHAI THÁC 1
				</p>
			</div>

			<div className='mt-4 text-center'>
				<p className='text-base font-bold uppercase md:text-lg'>
					Bảng nghiệm thu vật tư sử dụng và kết chuyển chi phí
				</p>
				<p className='mt-1 text-sm font-bold md:text-base'>
					{formatPeriodLabel(month, year)}
				</p>
			</div>
		</div>
	);
};

const ExcelAcceptanceReportFooter = ({
	month,
	year,
}: {
	month: string;
	year: string;
}) => {
	return (
		<div className='mt-6 font-["Times_New_Roman",Times,serif] text-[12px] font-semibold'>
			{/* Ngày tháng năm */}
			<div className='mb-4 text-right font-normal italic'>
				{/* Hà lầm, ngày 18 tháng 7 năm 2024 */}
				{`Hà lầm, tháng ${Number(month)} năm ${year}`}
			</div>

			{/* Kết luận */}
			<div className='max-w-[520px] border border-[#2f9f62] p-2 text-center leading-tight'>
				Kết luận: Toàn bộ số vật tư trên đã được sử dụng đúng mục đích, đảm bảo
				kỹ thuật an toàn. Hội đồng nghiệm thu thống nhất nghiệm thu làm cơ sở
				thanh toán.
			</div>

			{/* Tiêu đề đại diện hai bên */}
			<div className='mt-4 flex justify-between'>
				<p className='w-[48%] text-center'>ĐẠI DIỆN BÊN NHẬN KHOÁN</p>
				<p className='w-[48%] text-center'>ĐẠI DIỆN BÊN GIAO KHOÁN</p>
			</div>

			{/* Hàng chức danh */}
			<div className='mt-2 flex justify-between'>
				{/* Bên nhận khoán: NGƯỜI LẬP + QUẢN ĐỐC */}
				<div className='flex w-[48%] justify-around'>
					<p>NGƯỜI LẬP</p>
					<p>QUẢN ĐỐC</p>
					<p>PHÒNG KH</p>
					<p>PHÒNG KTTC</p>
					<p>PHÒNG CV</p>
					<p>PHÒNG VẬT TƯ</p>
					<p>PHÒNG KCM</p>
				</div>

				{/* Bên giao khoán: PHÒNG KCM + KT.GIÁM ĐỐC */}
				<div className='flex w-[48%] justify-around'>
					<div className='text-center'>
						<p>KT. GIÁM ĐỐC</p>
						<p>PHÓ GIÁM ĐỐC</p>
					</div>
				</div>
			</div>
			{/* Chữ ký */}
			<div className='mt-14 flex justify-between'>
				<div className='w-[48%]'>{/* chỗ ký bên nhận khoán */}</div>
				<div className='flex w-[48%] justify-end pr-8'></div>
			</div>
		</div>
	);
};

export function AcceptanceReportDataTable({
	enableSearch = true,
	largeText = true,
}: AcceptanceReportDataTableProps) {
	const now = new Date();
	const currentYear = now.getFullYear();
	const { hasPermission } = usePermission();
	const [month, setMonth] = useState(
		String(now.getMonth() + 1).padStart(2, '0'),
	);
	const [year, setYear] = useState(String(currentYear));
	const [rows, setRows] = useState<HierarchicalRow[]>([]);
	const [isLoading, setIsLoading] = useState(false);
	const [isExporting, setIsExporting] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [searchQuery, setSearchQuery] = useState('');

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

	const filteredRows = useMemo(() => {
		if (!enableSearch || !searchQuery.trim()) {
			return rows;
		}

		const query = searchQuery.toLowerCase();

		return rows.filter((row) => {
			const keywords = [row.label, row.itemCode, row.itemName, row.unit]
				.filter(Boolean)
				.join(' ')
				.toLowerCase();

			return keywords.includes(query);
		});
	}, [enableSearch, rows, searchQuery]);

	const fetchAcceptanceReport = useCallback(async () => {
		setIsLoading(true);
		setError(null);
		try {
			const outputResponse = await api.pagging<Production>(
				API.PRODUCTION.PRODUCTION_OUTPUT.LIST,
				{
					ignorePagination: true,
				},
			);

			const allOutputs = outputResponse.result.data ?? [];
			const outputsByPeriod = allOutputs
				.filter((output) => isSameMonthAndYear(output.startMonth, year, month))
				.filter((output) => !!output.acceptanceReportId)
				.sort(
					(a, b) =>
						new Date(b.startMonth).getTime() - new Date(a.startMonth).getTime(),
				);

			if (outputsByPeriod.length === 0) {
				setRows([]);
				return;
			}

			const [detailResponses, productionOrderResponse] = await Promise.all([
				Promise.all(
					outputsByPeriod.map((output) =>
						api.get<ProductionOutputDto>(
							API.PRODUCTION.PRODUCTION_OUTPUT.DETAIL(output.id),
						),
					),
				),
				api.pagging<ProductionOrderLookupDto>(
					API.CATALOG.PARAMETER.PRODUCTION_ORDER.LIST,
					{ ignorePagination: true },
				),
			]);

			const productionOrderNameById: Record<
				string,
				ProductionOrderDisplayInfo
			> = Object.fromEntries(
				(productionOrderResponse.result.data ?? []).map((item) => [
					item.id,
					{
						code: item.code || item.id,
						name: item.name || item.code || item.id,
					},
				]),
			);

			const mergedRows = detailResponses.flatMap((response, index) => {
				const detail = response.result;
				if (!detail) return [] as HierarchicalRow[];

				const hierarchical = transformApiResponseToHierarchical(detail);
				const flattened = flattenHierarchicalData(
					applyProductionOrderNames(hierarchical, productionOrderNameById),
				);
				const outputId = outputsByPeriod[index]?.id || `output-${index}`;

				return flattened.map((row) => ({
					...row,
					id: `${outputId}-${row.id}`,
				}));
			});

			setRows(mergedRows);
		} catch (err) {
			setRows([]);
			setError(
				err instanceof Error
					? err.message
					: 'Không thể tải dữ liệu bảng nghiệm thu và kết chuyển chi phí',
			);
		} finally {
			setIsLoading(false);
		}
	}, [month, year]);

	useEffect(() => {
		fetchAcceptanceReport();
	}, [fetchAcceptanceReport]);

	const handleExport = async () => {
		if (rows.length === 0) return;
		setIsExporting(true);
		try {
			const exportFileName = `bang-nghiem-thu-vat-tu-su-dung-va-ket-chuyen-chi-phi-thang-${month}-nam-${year}.xlsx`;
			await api.export(
				API.PRODUCTION.ACCEPTANCE_REPORT.EXPORT_PERIOD(month, year),
				{
					fileName: exportFileName,
					forceFileName: true,
				},
			);
		} catch (err) {
			console.error('Failed to export acceptance report:', err);
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

					{enableSearch && (
						<div className='space-y-1'>
							<p className='text-sm font-medium'>Tìm kiếm</p>
							<div className='relative'>
								<SearchIcon
									style={{ fontSize: 18 }}
									className='text-muted-foreground absolute top-1/2 left-2.5 -translate-y-1/2'
								/>
								<Input
									value={searchQuery}
									onChange={(event) => {
										setSearchQuery(event.target.value);
									}}
									placeholder='Tìm theo mã, tên vật tư...'
									className='h-10 w-[340px] bg-white pl-8 text-base'
								/>
							</div>
						</div>
					)}
				</div>

				{hasPermission(PERMISSIONS.REPORT.ACCEPTANCE_REPORT.EXPORT) && (
					<Button
						variant='outline'
						size='sm'
						disabled={rows.length === 0 || isLoading || isExporting}
						onClick={handleExport}
						className='h-10 gap-1.5'
					>
						{isExporting ? (
							<Spinner />
						) : (
							<>
								<DownloadIcon style={{ fontSize: 18 }} />
								<span>Tải xuống</span>
							</>
						)}
					</Button>
				)}
			</div>

			{error ? (
				<div className='border-border flex min-h-48 items-center justify-center rounded-t-md border bg-white shadow'>
					<div className='text-muted-foreground text-center'>
						<p className='text-lg font-medium'>Lỗi tải dữ liệu</p>
						<p className='text-sm'>{error}</p>
					</div>
				</div>
			) : isLoading ? (
				<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
					<Spinner />
				</div>
			) : (
				<>
					<div className='rounded-md border bg-[#e6e6e6] p-3 md:p-4'>
						<div className='mx-auto w-full overflow-auto'>
							<div className='mx-auto h-[210mm] overflow-auto bg-white p-3 shadow-[0_8px_30px_rgba(0,0,0,0.14)] md:p-5'>
								<ExcelAcceptanceReportHeader month={month} year={year} />

								<div className='mt-6'>
									<AcceptanceReportGrid
										data={filteredRows}
										className={cn(
											'overflow-auto rounded-none border border-black shadow-none [&_table]:font-["Times_New_Roman",Times,serif] [&_table]:text-[10px] [&_td]:px-1 [&_td]:py-1 [&_th]:px-1 [&_th]:py-1 [&>button]:hidden',
											largeText && '[&_table]:text-[11px]',
										)}
									/>
								</div>

								<ExcelAcceptanceReportFooter month={month} year={year} />
							</div>
						</div>
					</div>
				</>
			)}
		</div>
	);
}
