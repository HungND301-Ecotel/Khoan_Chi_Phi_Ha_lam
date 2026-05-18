import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type Electricity = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	unitOfMeasureName: string;
	equipmentElectricityCost: number;
	monthlyElectricityCost: number;
	averageMonthlyTunnelProduction: number;
	electricityConsumePerMetres: number;
	electricityCostPerMetres: number;
	startMonth: string;
	endMonth: string;
};

export const MAIN_PRICING_ELECTRICITY_COLUMNS: ColumnDef<Electricity>[] = [
	{
		accessorKey: 'equipmentCode',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Mã giao khoán'}</span>
		),
	},
	{
		accessorKey: 'equipmentName',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Tên giao khoán'}</span>
		),
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'ĐVT',
	},
	{
		accessorKey: 'equipmentElectricityCost',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Đơn giá điện năng (đ/kwh)'}
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.equipmentElectricityCost),
	},
	{
		accessorKey: 'monthlyElectricityCost',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Điện năng tiêu thụ 1 thiết bị/tháng (Kwh/tháng)'}
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.monthlyElectricityCost),
	},
	{
		accessorKey: 'averageMonthlyTunnelProduction',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Sản lượng xén lò bình quân tháng (m)'}
			</span>
		),
		cell: ({ row }) =>
			formatNumber(row.original.averageMonthlyTunnelProduction),
	},
	{
		accessorKey: 'electricityConsumePerMetres',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Điện năng tiêu thụ 1 thiết bị/1 mét lò xén (kwh/m)'}
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.electricityConsumePerMetres),
	},
	{
		accessorKey: 'electricityCostPerMetres',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Chi phí điện năng 1 thiết bị/1 mét lò xén (đ/m)'}
			</span>
		),
		cell: ({ row }) =>
			formatNumber(row.original.electricityCostPerMetres),
	},
	{
		accessorKey: 'time',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Thời gian'}</span>
		),
		cell: ({ row }) => (
			<span>
				<span>{formatDate(row.original.startMonth)}</span>
				<br />
				<span>{formatDate(row.original.endMonth)}</span>
			</span>
		),
	},
];
