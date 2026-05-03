'use client';

import {
	Bar,
	BarChart,
	ResponsiveContainer,
	XAxis,
	YAxis,
	Tooltip,
	Legend,
	CartesianGrid,
} from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { TrendingUp, TrendingDown, Pickaxe, Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { Department } from '@/features/main/catalog/department/columns';
import { api } from '@/lib/api';
import { API } from '@/constants/api-enpoint';
import { ProcessGroupType } from '@/constants/process-group';
import { DashboardCostSummary } from './types';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { formatNumber, formatYAxisValue } from '@/lib/utils';

const ALL_PROCESS_GROUP_VALUE = '__all_process_group__';
const ALL_DEPARTMENT_VALUE = '__all_department__';

type DashboardChartItem = {
	name: string;
	chiPhiKeHoach: number;
	doanhThuDieuChinh: number;
	chiPhiThucHien: number;
	sanLuongDaoLo: number;
	sanLuongLoCho: number;
};

const generateMonthlyData = () => {
	return Array.from(
		{ length: 12 },
		(_, i): DashboardChartItem => ({
			name: `Tháng ${i + 1}`,
			chiPhiKeHoach: 0,
			doanhThuDieuChinh: 0,
			chiPhiThucHien: 0,
			sanLuongDaoLo: 0,
			sanLuongLoCho: 0,
		}),
	);
};

export default function DashboardPage() {
	const [groups, setGroups] = useState<ProcessGroup[]>([]);
	const [departments, setDepartments] = useState<Department[]>([]);
	const [selectedGroup, setSelectedGroup] = useState<string>('');
	const [selectedDepartment, setSelectedDepartment] = useState<string>('');
	const [selectedYear, setSelectedYear] = useState<string>(
		new Date().getFullYear().toString(),
	);
	const [data, setData] = useState(generateMonthlyData());
	const [summary, setSummary] = useState<DashboardCostSummary | null>(null);
	const [isLoading, setIsLoading] = useState(false);

	// Fetch process groups
	useEffect(() => {
		api
			.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST)
			.then((res) => setGroups(res.result.data))
			.catch((error) => console.error('Error fetching process groups:', error));
	}, []);

	// Fetch departments
	useEffect(() => {
		api
			.pagging<Department>(API.CATALOG.DEPARTMENT.LIST, {
				ignorePagination: true,
			})
			.then((res) => setDepartments(res.result.data ?? []))
			.catch((error) => console.error('Error fetching departments:', error));
	}, []);

	// Fetch dashboard data when filters change
	useEffect(() => {
		const fetchOverview = async () => {
			setIsLoading(true);
			try {
				const query: Record<string, string> = { year: selectedYear };
				if (selectedGroup) {
					query.processGroupId = selectedGroup;
				}
				if (selectedDepartment) {
					query.departmentId = selectedDepartment;
				}

				// call API with query params
				const res = await api.get<DashboardCostSummary>(
					API.DASHBOARD.COST_SUMMARY,
					query,
				);

				const body = res.result;

				// prepare 12-month array with defaults
				const months = Array.from({ length: 12 }, (_, i) => ({
					name: `Tháng ${i + 1}`,
					chiPhiKeHoach: 0,
					doanhThuDieuChinh: 0,
					chiPhiThucHien: 0,
					sanLuongDaoLo: 0,
					sanLuongLoCho: 0,
				}));

				body.monthlyData?.forEach((m) => {
					const idx = Math.max(0, Math.min(11, m.month - 1));
					const plannedCost = Number((m as any).plannedCost ?? 0);
					const adjustmentCost = Number(
						(m as any).adjustmentCost ?? (m as any).adjustmentcost ?? 0,
					);
					const actualCost = Number((m as any).actualCost ?? 0);
					months[idx] = {
						name: `Tháng ${m.month}`,
						chiPhiKeHoach: plannedCost,
						doanhThuDieuChinh: adjustmentCost,
						chiPhiThucHien: actualCost,
						sanLuongDaoLo: Number((m as any).tunnelQuantity ?? 0),
						sanLuongLoCho: Number((m as any).longwallQuantity ?? 0),
					};
				});

				setSummary(body);
				setData(months);
			} catch (error) {
				console.error('Error fetching dashboard overview:', error);
				// fallback to empty months
				setSummary(null);
				setData(generateMonthlyData());
			} finally {
				setIsLoading(false);
			}
		};

		// allow fetching "all groups" when selectedGroup is empty
		if (selectedYear) {
			fetchOverview();
		}
	}, [selectedGroup, selectedDepartment, selectedYear]);

	// Generate year options (current year and 100 years back)
	const currentYear = new Date().getFullYear();
	const yearOptions = Array.from({ length: 101 }, (_, i) => currentYear - i);

	// Calculate totals
	const totalPlanned = data.reduce((sum, item) => sum + item.chiPhiKeHoach, 0);
	const totalAdjustment = data.reduce(
		(sum, item) => sum + item.doanhThuDieuChinh,
		0,
	);
	const totalActual = data.reduce((sum, item) => sum + item.chiPhiThucHien, 0);
	const selectedGroupData = groups.find((g) => g.id === selectedGroup);
	const selectedGroupType = selectedGroupData?.type;

	const volumeCards =
		selectedGroupType === ProcessGroupType.LC
			? [
					{
						title: 'Tổng sản lượng (tấn)',
						value: summary?.totalLongwallQuantity ?? 0,
						description: `Tổng sản lượng lò chợ trong năm ${selectedYear}`,
					},
				]
			: selectedGroupType === ProcessGroupType.DL ||
				  selectedGroupType === ProcessGroupType.XL
				? [
						{
							title: 'Tổng sản lượng (mét)',
							value: summary?.totalTunnelQuantity ?? 0,
							description: `Tổng sản lượng đào lò trong năm ${selectedYear}`,
						},
					]
				: [
						{
							title: 'Tổng sản lượng (tấn)',
							value: summary?.totalLongwallQuantity ?? 0,
							description: `Tổng sản lượng lò chợ trong năm ${selectedYear}`,
						},
						{
							title: 'Tổng sản lượng (mét)',
							value: summary?.totalTunnelQuantity ?? 0,
							description: `Tổng sản lượng đào lò trong năm ${selectedYear}`,
						},
					];
	const statsGridClass =
		volumeCards.length === 2
			? 'md:grid-cols-2 xl:grid-cols-5'
			: 'md:grid-cols-2 xl:grid-cols-4';
	const processGroupSelectValue = selectedGroup || ALL_PROCESS_GROUP_VALUE;
	const departmentSelectValue = selectedDepartment || ALL_DEPARTMENT_VALUE;

	return (
		<div className='flex flex-col gap-6 p-4 font-sans md:p-8'>
			<div className='flex flex-col gap-4'>
				<h2 className='text-3xl font-bold tracking-tight'>Báo cáo tổng quan</h2>

				<div className='flex justify-end'>
					<div className='flex w-full max-w-fit flex-col gap-3 rounded-lg border border-blue-200/50 bg-linear-to-br from-blue-50 to-emerald-50 p-4 sm:flex-row sm:items-end'>
						<div className='flex flex-col gap-2'>
							<Label
								htmlFor='process-group'
								className='text-sm font-semibold text-gray-800'
							>
								Nhóm công đoạn sản xuất
							</Label>
							<Select
								value={processGroupSelectValue}
								onValueChange={(value) =>
									setSelectedGroup(
										value === ALL_PROCESS_GROUP_VALUE ? '' : value,
									)
								}
							>
								<SelectTrigger
									id='process-group'
									className='h-10 w-full border-2 border-gray-300 bg-white font-medium shadow-sm transition-colors hover:border-blue-400 sm:w-60'
								>
									<SelectValue placeholder='Tất cả nhóm công đoạn sản xuất' />
								</SelectTrigger>
								<SelectContent>
									<SelectItem value={ALL_PROCESS_GROUP_VALUE}>
										Tất cả nhóm công đoạn sản xuất
									</SelectItem>
									{groups.map((group) => (
										<SelectItem key={group.id} value={group.id}>
											{group.name}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
						<div className='flex flex-col gap-2'>
							<Label
								htmlFor='department'
								className='text-sm font-semibold text-gray-800'
							>
								Đơn vị
							</Label>
							<Select
								value={departmentSelectValue}
								onValueChange={(value) =>
									setSelectedDepartment(
										value === ALL_DEPARTMENT_VALUE ? '' : value,
									)
								}
							>
								<SelectTrigger
									id='department'
									className='h-10 w-full border-2 border-gray-300 bg-white font-medium shadow-sm transition-colors hover:border-blue-400 sm:w-56'
								>
									<SelectValue placeholder='Tất cả đơn vị' />
								</SelectTrigger>
								<SelectContent>
									<SelectItem value={ALL_DEPARTMENT_VALUE}>
										Tất cả đơn vị
									</SelectItem>
									{departments.map((department) => (
										<SelectItem key={department.id} value={department.id}>
											{department.code} - {department.name}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
						<div className='flex flex-col gap-2'>
							<Label
								htmlFor='year'
								className='text-sm font-semibold text-gray-800'
							>
								Năm
							</Label>
							<Select value={selectedYear} onValueChange={setSelectedYear}>
								<SelectTrigger
									id='year'
									className='h-10 w-full border-2 border-gray-300 bg-white font-medium shadow-sm transition-colors hover:border-blue-400 sm:w-[140px]'
								>
									<SelectValue placeholder='Chọn năm' />
								</SelectTrigger>
								<SelectContent className='h-60'>
									{yearOptions.map((year) => (
										<SelectItem key={year} value={year.toString()}>
											{year}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
					</div>
				</div>
			</div>

			{/* Stats Cards */}
			<div className={`grid gap-4 ${statsGridClass}`}>
				{volumeCards.map((card) => (
					<Card key={card.title}>
						<CardHeader className='flex flex-row items-center justify-between space-y-0 pb-2'>
							<CardTitle className='text-sm font-medium'>
								{card.title}
							</CardTitle>
							<Pickaxe className='h-4 w-4 text-gray-500' />
						</CardHeader>
						<CardContent>
							<div className='text-2xl font-bold text-gray-900'>
								{formatNumber(card.value)}
							</div>
							<p className='text-muted-foreground mt-1 text-xs'>
								{card.description}
							</p>
						</CardContent>
					</Card>
				))}
				<Card>
					<CardHeader className='flex flex-row items-center justify-between space-y-0 pb-2'>
						<CardTitle className='text-sm font-medium'>
							Tổng doanh thu kế hoạch
						</CardTitle>
						<TrendingUp className='h-4 w-4 text-blue-500' />
					</CardHeader>
					<CardContent>
						<div className='text-2xl font-bold text-blue-600'>
							{formatNumber(totalPlanned)} VNĐ
						</div>
						<p className='text-muted-foreground mt-1 text-xs'>
							Tổng doanh thu kế hoạch trong năm {selectedYear}
						</p>
					</CardContent>
				</Card>
				<Card>
					<CardHeader className='flex flex-row items-center justify-between space-y-0 pb-2'>
						<CardTitle className='text-sm font-medium'>
							Tổng doanh thu điều chỉnh
						</CardTitle>
						<TrendingUp className='h-4 w-4 text-cyan-500' />
					</CardHeader>
					<CardContent>
						<div className='text-2xl font-bold text-cyan-600'>
							{formatNumber(totalAdjustment)} VNĐ
						</div>
						<p className='text-muted-foreground mt-1 text-xs'>
							Tổng doanh thu điều chỉnh trong năm {selectedYear}
						</p>
					</CardContent>
				</Card>
				<Card>
					<CardHeader className='flex flex-row items-center justify-between space-y-0 pb-2'>
						<CardTitle className='text-sm font-medium'>Tổng chi phí</CardTitle>
						<TrendingDown className='h-4 w-4 text-emerald-500' />
					</CardHeader>
					<CardContent>
						<div className='text-2xl font-bold text-emerald-600'>
							{formatNumber(totalActual)} VNĐ
						</div>
						<p className='text-muted-foreground mt-1 text-xs'>
							Tổng chi phí trong năm {selectedYear}
						</p>
					</CardContent>
				</Card>
			</div>

			{/* Chart Section */}
			<Card>
				<CardHeader className='space-y-4'>
					<div className='flex flex-col gap-4 md:flex-row md:items-start md:justify-between'>
						<div>
							<CardTitle className='text-xl'>
								Biểu đồ doanh thu & chi phí
							</CardTitle>
							<p className='text-muted-foreground mt-1 text-sm'>
								Theo dõi doanh thu và chi phí theo từng tháng trong năm
							</p>
						</div>
					</div>
				</CardHeader>
				<CardContent className='pt-4'>
					{isLoading ? (
						<div className='flex h-[450px] items-center justify-center'>
							<div className='flex flex-col items-center gap-3'>
								<Loader2 className='h-10 w-10 animate-spin text-blue-500' />
								<p className='text-sm text-gray-500'>Đang tải dữ liệu...</p>
							</div>
						</div>
					) : (
						<ResponsiveContainer width='100%' height={450}>
							<BarChart
								data={data}
								margin={{ top: 10, right: 30, left: 0, bottom: 0 }}
								style={{ fontFamily: 'inherit' }}
							>
								<CartesianGrid
									strokeDasharray='3 3'
									stroke='#e5e7eb'
									opacity={0.5}
								/>
								<XAxis
									dataKey='name'
									stroke='#9ca3af'
									fontSize={12}
									tickLine={false}
									axisLine={false}
									tickMargin={10}
								/>
								<YAxis
									stroke='#9ca3af'
									fontSize={12}
									tickLine={false}
									axisLine={false}
									tickMargin={10}
									tickFormatter={formatYAxisValue}
								/>
								<Tooltip
									cursor={false}
									content={({ active, payload }) => {
										if (active && payload && payload.length > 0) {
											const monthData = payload[0].payload;
											const plannedRevenue = Number(
												monthData.chiPhiKeHoach ?? 0,
											);
											const adjustmentRevenue = Number(
												monthData.doanhThuDieuChinh ?? 0,
											);
											const actualCost = Number(monthData.chiPhiThucHien ?? 0);

											return (
												<div className='overflow-hidden rounded-lg border border-gray-200 bg-white shadow-lg'>
													<div className='border-b border-gray-200 bg-linear-to-r from-blue-50 to-emerald-50 px-4 py-2.5'>
														<p className='font-semibold text-gray-900'>
															{monthData.name}
														</p>
													</div>
													<div className='space-y-3 p-4'>
														{(selectedGroupType === undefined ||
															selectedGroupType === ProcessGroupType.LC) && (
															<div className='flex items-center justify-between gap-10'>
																<span className='flex items-center gap-2 text-sm text-gray-600'>
																	<Pickaxe className='h-3.5 w-3.5' />
																	Sản lượng lò chợ:
																</span>
																<span className='font-semibold text-gray-900 tabular-nums'>
																	{formatNumber(monthData.sanLuongLoCho)} tấn
																</span>
															</div>
														)}
														{(selectedGroupType === undefined ||
															selectedGroupType === ProcessGroupType.DL ||
															selectedGroupType === ProcessGroupType.XL) && (
															<div className='flex items-center justify-between gap-10'>
																<span className='flex items-center gap-2 text-sm text-gray-600'>
																	<Pickaxe className='h-3.5 w-3.5' />
																	Sản lượng đào lò:
																</span>
																<span className='font-semibold text-gray-900 tabular-nums'>
																	{formatNumber(monthData.sanLuongDaoLo)} mét
																</span>
															</div>
														)}
														<div className='flex items-center justify-between gap-10'>
															<span className='flex items-center gap-2 text-sm text-gray-600'>
																<span className='inline-block h-3 w-3 rounded-sm bg-blue-500' />
																Doanh thu:
															</span>
															<span className='font-bold text-blue-600 tabular-nums'>
																{formatNumber(plannedRevenue)} VNĐ
															</span>
														</div>
														<div className='flex items-center justify-between gap-10'>
															<span className='flex items-center gap-2 text-sm text-gray-600'>
																<span className='inline-block h-3 w-3 rounded-sm bg-cyan-500' />
																Doanh thu điều chỉnh:
															</span>
															<span className='font-bold text-cyan-600 tabular-nums'>
																{formatNumber(adjustmentRevenue)}{' '}
																VNĐ
															</span>
														</div>
														<div className='flex items-center justify-between gap-10'>
															<span className='flex items-center gap-2 text-sm text-gray-600'>
																<span className='inline-block h-3 w-3 rounded-sm bg-emerald-500' />
																Chi phí:
															</span>
															<span className='font-bold text-emerald-600 tabular-nums'>
																{formatNumber(actualCost)} VNĐ
															</span>
														</div>
													</div>
												</div>
											);
										}
										return null;
									}}
								/>
								<Legend
									wrapperStyle={{ paddingTop: '24px' }}
									iconType='rect'
									iconSize={14}
									formatter={(value) => {
										const labels: Record<string, string> = {
											chiPhiKeHoach: 'Doanh thu kế hoạch',
											doanhThuDieuChinh: 'Doanh thu điều chỉnh',
											chiPhiThucHien: 'Chi phí',
										};
										return (
											<span className='text-sm font-medium text-gray-700'>
												{labels[value] || value}
											</span>
										);
									}}
								/>
								<Bar
									dataKey='chiPhiKeHoach'
									fill='#3b82f6'
									radius={[6, 6, 0, 0]}
									maxBarSize={50}
								/>
								<Bar
									dataKey='doanhThuDieuChinh'
									fill='#06b6d4'
									radius={[6, 6, 0, 0]}
									maxBarSize={50}
								/>
								<Bar
									dataKey='chiPhiThucHien'
									fill='#10b981'
									radius={[6, 6, 0, 0]}
									maxBarSize={50}
								/>
							</BarChart>
						</ResponsiveContainer>
					)}
				</CardContent>
			</Card>
		</div>
	);
}
