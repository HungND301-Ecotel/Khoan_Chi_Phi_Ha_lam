import { FormMonthYear } from '@/components/form-month-year/form-month-year';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormProvider } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { API } from '@/constants/api-enpoint';
import { LUMP_SUM_FINAL_SETTLEMENT_COLUMNS } from '@/features/main/cost/lump-sum-final-settlement/columns';
import { LumpSumDataTable } from '@/features/main/cost/lump-sum-final-settlement/components/datatable';
import { groupByProcessGroup } from '@/features/main/cost/lump-sum-final-settlement/grouping';
import {
	LumpSumFinalSettlement,
	LumpSumFinalSettlementListRequest,
	ProcessGroup,
	YearFilterForm,
} from '@/features/main/cost/lump-sum-final-settlement/types';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import DownloadIcon from '@mui/icons-material/Download';
import EmailIcon from '@mui/icons-material/Email';
import PrintIcon from '@mui/icons-material/Print';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

const shadow = cn(
	'hover:shadow-[0px_2px_4px_-1px_rgba(0,0,0,0.2),0px_4px_5px_0px_rgba(0,0,0,0.14),0px_1px_10px_0px_rgba(0,0,0,0.12)] shadow-[0px_3px_1px_-2px_rgba(0,0,0,0.2),0px_2px_2px_0px_rgba(0,0,0,0.14),0px_1px_5px_0px_rgba(0,0,0,0.12)]',
);

export function MainCostLumpSumFinalSettlementMonthPage() {
	const [filteredData, setFilteredData] = useState<LumpSumFinalSettlement[]>(
		[],
	);
	const [isLoading, setIsLoading] = useState(false);
	const [processGroups, setProcessGroups] = useState<
		{ value: string; label: string }[]
	>([{ value: '', label: 'Tất cả nhóm công đoạn' }]);

	const now = new Date();
	const defaultMonth = String(now.getMonth() + 1);
	const defaultYear = String(now.getFullYear());

	const form = useForm<YearFilterForm>({
		defaultValues: {
			month: defaultMonth,
			year: defaultYear,
			processGroup: '',
		},
	});

	useEffect(() => {
		const fetchProcessGroups = async () => {
			try {
				const response = await api.pagging<ProcessGroup>(
					API.CATALOG.PROCESS.GROUP.LIST,
					{ ignorePagination: true },
				);
				const options = [
					{ value: '', label: 'Tất cả nhóm công đoạn' },
					...(response.result.data ?? []).map((item: ProcessGroup) => ({
						value: item.id,
						label: `${item.code} - ${item.name}`,
					})),
				];
				setProcessGroups(options);
			} catch (error) {
				console.error('Error fetching process groups:', error);
			}
		};

		fetchProcessGroups();
	}, []);

	const fetchLumpSum = useCallback(
		async (payload: {
			month: string;
			year: string;
			processGroupId: string;
		}) => {
			setIsLoading(true);
			try {
				const res = await api.post<
					LumpSumFinalSettlement[],
					LumpSumFinalSettlementListRequest
				>(API.COST.LUMP_SUM_FINAL_SETTLEMENT.LIST, payload);

				setFilteredData(groupByProcessGroup(res.result));
			} catch (error) {
				console.error('Error fetching lump sum list:', error);
			} finally {
				setIsLoading(false);
			}
		},
		[],
	);

	const handleFilter = useCallback(
		(data: YearFilterForm) => {
			if (!data.month || !data.year) return;

			fetchLumpSum({
				month: data.month,
				year: data.year,
				processGroupId: data.processGroup ?? '',
			});
		},
		[fetchLumpSum],
	);

	useEffect(() => {
		const subscription = form.watch((value) => {
			if (value.month && value.year) {
				handleFilter({
					month: value.month,
					year: value.year,
					processGroup: value.processGroup ?? '',
				});
			}
		});
		return () => subscription.unsubscribe();
	}, [form, handleFilter]);

	useEffect(() => {
		const value = form.getValues();
		if (value.month && value.year) {
			handleFilter({
				month: value.month,
				year: value.year,
				processGroup: value.processGroup ?? '',
			});
		}
	}, [form, handleFilter]);

	return (
		<Card>
			<CardHeader>
				<FormProvider context={form} onSubmit={handleFilter}>
					<div className='flex items-end justify-between gap-4'>
						<div className='grid w-full max-w-3xl flex-1 grid-cols-1 gap-4 md:grid-cols-3'>
							<FormMonthYear
								control={form.control}
								month='month'
								year='year'
								label='Tháng và Năm'
								placeholder='Chọn tháng và năm'
							/>
							<FormComboBox
								control={form.control}
								name='processGroup'
								label='Nhóm công đoạn sản xuất'
								placeholder='Tất cả nhóm công đoạn'
								options={processGroups}
							/>
						</div>
						<div className='flex shrink-0 gap-4'>
							<Button variant={'ghost'} className={shadow}>
								<DownloadIcon fontSize='small' />
								<span className='hidden xl:block'>Xuất file</span>
							</Button>
							<Button variant={'ghost'} className={shadow}>
								<PrintIcon fontSize='small' />
								<span className='hidden xl:block'>In</span>
							</Button>
							<Button variant={'ghost'} className={shadow}>
								<EmailIcon fontSize='small' />
								<span className='hidden xl:block'>Gửi</span>
							</Button>
						</div>
					</div>
				</FormProvider>
			</CardHeader>
			<CardContent>
				<LumpSumDataTable
					columns={LUMP_SUM_FINAL_SETTLEMENT_COLUMNS}
					data={filteredData}
					isLoading={isLoading}
				/>
			</CardContent>
		</Card>
	);
}
