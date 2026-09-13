import { ColumnDef } from '@tanstack/react-table';

export type LongwallElectricityPeriod = {
	id: string;
	startMonth: string;
	endMonth: string;
	equipmentElectricityCost: number;
	quantity: number;
	pdm: number;
	sPdm?: number;
	kyc: number;
	kdt: number;
	workingHour: number;
	workingDate: number;
	longwallAverageMonthlyTunnelProduction: number;
	electricityConsumePerMetres: number;
	electricityCostPerMetres: number;
};

export type LongwallElectricity = {
	id: string;
	equipmentId: string;
	equipmentCode: string;
	equipmentName: string;
	unitOfMeasureName: string;
	equipmentElectricityCost?: number;
	electricityConsumePerMetres?: number;
	electricityCostPerMetres?: number;
	startMonth?: string;
	endMonth?: string;
	type?: number;
	monthlyElectricityCost?: number | null;
	averageMonthlyTunnelProduction?: number | null;
	quantity?: number;
	pdm?: number;
	sPdm?: number;
	kyc?: number;
	kdt?: number;
	workingHour?: number;
	workingDate?: number;
	longwallAverageMonthlyTunnelProduction?: number;
	monthlyElectricityConsumption?: number;
	periods?: LongwallElectricityPeriod[];
};

export const LONGWALL_ELECTRICITY_COLUMNS: ColumnDef<LongwallElectricity>[] = [
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
		header: 'Đơn vị tính',
	},
];

