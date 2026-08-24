import { formatDate } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

export type ScaniaUnitPriceDetail = {
	id?: string;
	haulDistanceId?: string;
	haulDistanceValue?: string;
	fuelUnitPrice: number;
	powerUnitPrice?: number;
	maintenanceUnitPrice: number;
};

export type MotorizedScaniaUnitPrice = {
	id: string;
	assignmentCodeId?: string;
	assignmentCodeName?: string;
	equipmentId?: string;
	equipmentName?: string;
	equipmentQuality: 'A' | 'B' | 'C' | string;
	productionProcessId?: string;
	productionProcess?: string;
	productionProcessName?: string;
	cargoTypeId?: string;
	cargoTypeName?: string;
	receivingLocationId?: string;
	receivingLocationName?: string;
	dumpingLocationId?: string;
	dumpingLocationName?: string;
	distanceRange?: string;
	startMonth: string;
	endMonth: string;
	fuelUnitPrice?: number;
	powerUnitPrice?: number;
	maintenanceUnitPrice?: number;
	details?: ScaniaUnitPriceDetail[];
	allProcesses?: MotorizedScaniaUnitPrice[];
	allIds?: string[];
};

export const MOTORIZED_SCANIA_COLUMNS: ColumnDef<MotorizedScaniaUnitPrice>[] = [
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
			const unit = 'đ/tkm';

			if (details.length === 0) {
				const fuel = row.original.fuelUnitPrice ?? 0;
				const power = row.original.powerUnitPrice ?? 0;
				const sctx = row.original.maintenanceUnitPrice ?? 0;
				const dist = row.original.distanceRange || '';
				return (
					<div className='text-xs'>
						{dist && <div className='font-medium text-gray-600'>Cung độ: {dist}</div>}
						<div>NL: {fuel?.toLocaleString('vi-VN')} {unit}</div>
						<div>ĐL: {power?.toLocaleString('vi-VN')} {unit}</div>
						<div>SCTX: {sctx?.toLocaleString('vi-VN')} {unit}</div>
					</div>
				);
			}

			return (
				<div className='text-xs space-y-1'>
					{details.slice(0, 3).map((d, i) => (
						<div key={i} className='flex items-center gap-2'>
							{d.haulDistanceValue && (
								<span className='font-medium text-gray-600'>{d.haulDistanceValue}km</span>
							)}
							<span>NL: {d.fuelUnitPrice?.toLocaleString('vi-VN')}</span>
							<span>ĐL: {d.powerUnitPrice?.toLocaleString('vi-VN')}</span>
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
