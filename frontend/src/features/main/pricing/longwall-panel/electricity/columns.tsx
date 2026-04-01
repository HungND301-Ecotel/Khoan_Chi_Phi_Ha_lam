import { formatDate, formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type LongwallElectricity = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	unitOfMeasureName: string;
	equipmentElectricityCost: number;
	electricityConsumePerMetres: number;
	electricityCostPerMetres: number;
	startMonth: string;
	endMonth: string;
	type: number;
	monthlyElectricityCost: number | null;
	averageMonthlyTunnelProduction: number | null;
	quantity: number;
	pdm: number;
	sPdm?: number;
	kyc: number;
	kdt: number;
	workingHour: number;
	workingDate: number;
	longwallAverageMonthlyTunnelProduction: number;
	monthlyElectricityConsumption?: number;
};

export const LONGWALL_ELECTRICITY_COLUMNS: ColumnDef<LongwallElectricity>[] = [
	{
		accessorKey: 'equipmentCode',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Mã thiết bị'}</span>
		),
	},
	{
		accessorKey: 'equipmentName',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Tên thiết bị'}</span>
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
		accessorKey: 'quantity',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Số lượng'}</span>
		),
		cell: ({ row }) => formatNumber(row.original.quantity),
	},
	{
		accessorKey: 'pdm',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Pđm (kW)'}</span>
		),
		cell: ({ row }) => formatNumber(row.original.pdm),
	},
	{
		accessorKey: 'sPdm',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'SPđm (kW)'}</span>
		),
		cell: ({ row }) => formatNumber(row.original.sPdm ?? 0),
	},
	{
		accessorKey: 'kyc',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Kyc'}</span>
		),
		cell: ({ row }) => formatNumber(row.original.kyc ?? 0),
	},
	{
		accessorKey: 'kdt',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Kdt'}</span>
		),
		cell: ({ row }) => formatNumber(row.original.kdt ?? 0),
	},
	{
		accessorKey: 'ptt',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Ptt (kW)'}</span>
		),
		cell: ({ row }) =>
			formatNumber(
				(row.original.sPdm ?? 0) *
					(row.original.kyc ?? 0) *
					(row.original.kdt ?? 0),
			),
	},
	{
		accessorKey: 'workingHour',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Thời gian (h)'}</span>
		),
		cell: ({ row }) => formatNumber(row.original.workingHour ?? 0),
	},
	{
		accessorKey: 'workingDate',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Ngày hoạt động'}
			</span>
		),
		cell: ({ row }) => formatNumber(row.original.workingDate ?? 0),
	},
	{
		accessorKey: 'longwallAverageMonthlyTunnelProduction',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Sản lượng than bình quân tháng (1000 tấn)'}
			</span>
		),
		cell: ({ row }) =>
			formatNumber(row.original.longwallAverageMonthlyTunnelProduction),
	},
	{
		accessorKey: 'electricityConsumePerMetres',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Điện năng cho 01 thiết bị/1 tấn than (kWh/tấn)'}
			</span>
		),
		cell: ({ row }) =>
			formatNumber(row.original.electricityConsumePerMetres ?? 0),
	},
	{
		accessorKey: 'electricityCostPerMetres',
		header: () => (
			<span className='leading-tight whitespace-normal'>
				{'Chi phí Điện năng cho 1 thiết bị/1 tấn than (đ/tấn)'}
			</span>
		),
		cell: ({ row }) =>
			formatNumber(Math.round(row.original.electricityCostPerMetres ?? 0)),
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
