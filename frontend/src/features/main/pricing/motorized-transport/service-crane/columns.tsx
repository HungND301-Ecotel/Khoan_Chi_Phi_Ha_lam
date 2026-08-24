import { formatDate } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type ServiceCraneUnitPriceDetail = {
	id?: string;
	haulDistanceId?: string;
	haulDistanceValue?: string;
	fuelUnitPrice: number;
	maintenanceUnitPrice: number;
};

export type MotorizedServiceCraneUnitPrice = {
	id: string;
	assignmentCodeId?: string;
	assignmentCodeName?: string;
	equipmentId?: string;
	equipmentName?: string;
	equipmentQuality: 'A' | 'B' | 'C' | string;
	productionProcessId?: string;
	productionProcess?: string;
	productionProcessName?: string;
	startMonth: string;
	endMonth: string;
	distanceRange?: string;
	fuelUnitPrice?: number;
	maintenanceUnitPrice?: number;
	details?: ServiceCraneUnitPriceDetail[];
	allProcesses?: MotorizedServiceCraneUnitPrice[];
	allIds?: string[];
};

export const MOTORIZED_SERVICE_CRANE_COLUMNS: ColumnDef<MotorizedServiceCraneUnitPrice>[] = [
	{
		accessorKey: 'startMonth',
		header: 'Thời gian',
		cell: ({ row }) => (
			<div className='flex flex-col text-xs font-medium'>
				<span>{formatDate(row.original.startMonth)}</span>
				<span className='text-gray-500'>
					{formatDate(row.original.endMonth)}
				</span>
			</div>
		),
	},
	{
		accessorKey: 'assignmentCodeName',
		header: 'Nhóm vật tư, tài sản',
		cell: ({ row }) =>
			row.original.assignmentCodeName || row.original.equipmentName || '-',
	},
	{
		accessorKey: 'equipmentQuality',
		header: 'Chất lượng thiết bị',
		cell: ({ row }) => `Thiết bị loại ${row.original.equipmentQuality}`,
	},
	{
		accessorKey: 'productionProcessName',
		header: 'Công đoạn sản xuất',
		cell: ({ row }) =>
			row.original.productionProcessName || row.original.productionProcess || '-',
	},
	{
		id: 'detailsSummary',
		header: 'Thông tin đơn giá',
		cell: ({ row }) => {
			const details = row.original.details || [];
			const process = row.original.productionProcessName || row.original.productionProcess || '';
			const isWatering = process.toLowerCase().includes('tưới đường mỏ');

			let unit = 'đ/km';
			if (process.toLowerCase().includes('phục vụ') || process.toLowerCase().includes('di chuyển'))
				unit = 'đ/h';
			else if (isWatering) unit = 'đ/tkm';

			// If no details array, show from direct fields
			if (details.length === 0) {
				const fuel = row.original.fuelUnitPrice ?? 0;
				const sctx = row.original.maintenanceUnitPrice ?? 0;
				const dist = row.original.distanceRange || '';
				return (
					<div className='text-xs'>
						{dist && <div className='font-medium text-gray-600'>Cung độ: {dist}</div>}
						<div>Nhiên liệu: {fuel?.toLocaleString('vi-VN')} {unit}</div>
						<div>SCTX: {sctx?.toLocaleString('vi-VN')} {unit}</div>
					</div>
				);
			}

			// Show details with distance if watering
			return (
				<div className='text-xs space-y-1'>
					{details.slice(0, 3).map((d, i) => (
						<div key={i} className='flex items-center gap-2'>
							{isWatering && d.haulDistanceValue && (
								<span className='font-medium text-gray-600'>{d.haulDistanceValue}km</span>
							)}
							<span>NL: {d.fuelUnitPrice?.toLocaleString('vi-VN')}</span>
							<span>|</span>
							<span>SCTX: {d.maintenanceUnitPrice?.toLocaleString('vi-VN')}</span>
							<span>{unit}</span>
						</div>
					))}
					{details.length > 3 && (
						<div className='text-gray-500'>... và {details.length - 3} mục khác</div>
					)}
				</div>
			);
		},
	},
];
