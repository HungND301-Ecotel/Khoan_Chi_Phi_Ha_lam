import { ColumnDef } from '@tanstack/react-table';

export type ElectricityPeriod = {
	id: string;
	startMonth: string;
	endMonth: string;
	equipmentElectricityCost: number;
	monthlyElectricityCost: number;
	averageMonthlyTunnelProduction: number;
	electricityConsumePerMetres: number;
	electricityCostPerMetres: number;
};

export type Electricity = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	unitOfMeasureName: string;
	equipmentElectricityCost?: number;
	monthlyElectricityCost?: number;
	averageMonthlyTunnelProduction?: number;
	electricityConsumePerMetres?: number;
	electricityCostPerMetres?: number;
	startMonth?: string;
	endMonth?: string;
	periods?: ElectricityPeriod[];
};

export const MAIN_PRICING_ELECTRICITY_COLUMNS: ColumnDef<Electricity>[] = [
	{
		accessorKey: 'equipmentCode',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Nhóm vật tư, tài sản'}</span>
		),
	},
	{
		accessorKey: 'equipmentName',
		header: () => (
			<span className='leading-tight whitespace-normal'>{'Tên nhóm vật tư, tài sản'}</span>
		),
	},
	{
		accessorKey: 'unitOfMeasureName',
		header: 'ĐVT',
	},
];

