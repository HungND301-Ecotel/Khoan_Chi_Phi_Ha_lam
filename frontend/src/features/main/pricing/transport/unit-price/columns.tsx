import { ColumnDef } from '@tanstack/react-table';

export type TransportMode =
	| 'conveyor'
	| 'shaft'
	| 'cable_winch'
	| 'monorail'
	| 'other';

export type TransportUnitPrice = {
	id: string;
	code?: string;
	displayCode?: string;
	transportMode?: TransportMode;
	productionProcessId: string;
	productionProcessCode?: string;
	productionProcessName?: string;
	transportRouteId?: string;
	transportRouteCode?: string;
	transportRouteName?: string;
	departmentId?: string;
	departmentName?: string;
	contractCodeId?: string;
	contractCodeName?: string;
	equipmentId?: string;
	equipmentName?: string;
	materialId?: string;
	materialName?: string;
	equipmentQuality?: string;
	quantity?: number;
	unitOfMeasureId?: string;
	unitOfMeasureName?: string;
	materialFuelUnitPrice?: number;
	powerUnitPrice?: number;
	maintenanceUnitPrice?: number;
	isLowVolumeCase?: boolean;
	startMonth: string;
	endMonth: string;
};

export const formatMoney = (value?: number) => {
	if (value === undefined || value === null) return '-';
	return new Intl.NumberFormat('vi-VN').format(value);
};

export const MAIN_PRICING_TRANSPORT_UNIT_PRICE_COLUMNS: ColumnDef<TransportUnitPrice>[] =
	[
		{
			accessorKey: 'startMonth',
			header: 'Thời gian',
			cell: ({ row }) => (
				<div className='flex flex-col text-xs font-medium'>
					<span>{row.original.startMonth}</span>
					<span className='text-gray-500'>{row.original.endMonth}</span>
				</div>
			),
		},
		{
			accessorKey: 'productionProcessName',
			header: 'Công đoạn sản xuất',
		},
		{
			accessorKey: 'transportRouteName',
			header: 'Tuyến vận tải',
			cell: ({ row }) => row.original.transportRouteName || '-',
		},
		{
			accessorKey: 'contractCodeName',
			header: 'Nhóm vật tư',
			cell: ({ row }) =>
				row.original.contractCodeName ||
				row.original.equipmentName ||
				row.original.materialName ||
				'-',
		},
	];

export const EXPAND_TRANSPORT_UNIT_PRICE_COLUMNS: ColumnDef<TransportUnitPrice>[] =
	[
		{
			accessorKey: 'departmentName',
			header: 'Đơn vị',
			cell: ({ row }) => {
				const dept = row.original.departmentName || '-';
				if (row.original.isLowVolumeCase) {
					return `${dept} ( < 10.000t/tháng )`;
				}
				return dept;
			},
		},
		{
			accessorKey: 'equipmentQuality',
			header: 'Chất lượng thiết bị',
			cell: ({ row }) =>
				row.original.equipmentQuality
					? `Thiết bị loại ${row.original.equipmentQuality}`
					: '-',
		},
		{
			accessorKey: 'materialFuelUnitPrice',
			header: 'Đơn giá VL, NL',
			cell: ({ row }) => formatMoney(row.original.materialFuelUnitPrice),
		},
		{
			accessorKey: 'powerUnitPrice',
			header: 'Đơn giá Động lực',
			cell: ({ row }) => formatMoney(row.original.powerUnitPrice),
		},
		{
			accessorKey: 'maintenanceUnitPrice',
			header: 'Đơn giá SCTX',
			cell: ({ row }) => formatMoney(row.original.maintenanceUnitPrice),
		},
		{
			accessorKey: 'quantity',
			header: 'Định mức',
			cell: ({ row }) => row.original.quantity ?? '-',
		},
	];
