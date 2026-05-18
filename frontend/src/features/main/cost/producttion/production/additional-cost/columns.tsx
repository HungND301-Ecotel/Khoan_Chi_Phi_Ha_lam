import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';

// Material (Vật tư) Types
export type MaterialItem = {
	id: string;
	contractCode: string;
	materialCode: string;
	materialName: string;
	unitOfMeasure: string;
	quantity: number;
};

export type MaterialGroup = {
	contractCode: string;
	items: MaterialItem[];
};

// SCTX (Spare Parts) Types
export type SCTXItem = {
	id: string;
	equipmentCode: string;
	partCode: string;
	partName: string;
	unitOfMeasure: string;
	quantity: number;
};

export type SCTXGroup = {
	equipmentCode: string;
	items: SCTXItem[];
};

// Other Material (Vật tư khác) Type
export type OtherMaterial = {
	id: string;
	materialCode: string;
	materialName: string;
	unitOfMeasure: string;
	quantity: number;
};

// API Response Types
export type AdditionalCostItem = {
	code: string;
	name: string;
	unitOfMeasureName: string;
	additionalCostQuantity: number;
};

export type AdditionalCostResponse = {
	id: string;
	additionalCosts: {
		material: AdditionalCostItem[];
		maintain: AdditionalCostItem[];
		otherMaterial: AdditionalCostItem[];
	};
};

// Material Cost - for display without grouping
export type MaterialCost = {
	id: string;
	materialCode: string;
	materialName: string;
	unitOfMeasure: string;
	quantity: number;
};

// SCTX Cost - for display without grouping
export type SCTXCost = {
	id: string;
	partCode: string;
	partName: string;
	unitOfMeasure: string;
	quantity: number;
};

// Additional Cost Detail
export type AdditionalCostDetail = {
	id: string;
	materials: MaterialGroup[];
	sctx: SCTXGroup[];
	otherMaterials: OtherMaterial[];
};

// Helper function to flatten material data
export function flattenMaterialData(
	materials: MaterialGroup[],
): MaterialCost[] {
	const flat: MaterialCost[] = [];

	materials.forEach((group) => {
		// Add item rows
		group.items.forEach((item) => {
			flat.push({
				id: item.id,
				materialCode: item.materialCode,
				materialName: item.materialName,
				unitOfMeasure: item.unitOfMeasure,
				quantity: item.quantity,
			});
		});
	});

	return flat;
}

// Helper function to flatten SCTX data
export function flattenSCTXData(sctxGroups: SCTXGroup[]): SCTXCost[] {
	const flat: SCTXCost[] = [];

	sctxGroups.forEach((group) => {
		// Add item rows
		group.items.forEach((item) => {
			flat.push({
				id: item.id,
				partCode: item.partCode,
				partName: item.partName,
				unitOfMeasure: item.unitOfMeasure,
				quantity: item.quantity,
			});
		});
	});

	return flat;
}

// Material Columns (Vật tư)
export const MATERIAL_COLUMNS: ColumnDef<MaterialCost>[] = [
	{
		accessorKey: 'materialCode',
		header: () => <span className='whitespace-normal'>{'Mã vật tư'}</span>,
		cell: ({ row }) => row.original.materialCode ?? '',
		size: 120,
	},
	{
		accessorKey: 'materialName',
		header: () => <span className='whitespace-normal'>{'Tên vật tư'}</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.materialName ?? ''}
			</span>
		),
		size: 200,
	},
	{
		accessorKey: 'unitOfMeasure',
		header: () => <span className='whitespace-normal'>{'ĐVT'}</span>,
		cell: ({ row }) => row.original.unitOfMeasure ?? '',
		size: 100,
	},
	{
		accessorKey: 'quantity',
		header: () => <span className='whitespace-normal'>{'Số lượng'}</span>,
		cell: ({ row }) =>
			row.original.quantity !== undefined
				? formatNumber(row.original.quantity)
				: '',
		size: 120,
	},
];

// SCTX Columns (Vật tư công cụ)
export const SCTX_COLUMNS: ColumnDef<SCTXCost>[] = [
	{
		accessorKey: 'partCode',
		header: () => <span className='whitespace-normal'>{'Mã vật tư'}</span>,
		cell: ({ row }) => row.original.partCode ?? '',
		size: 120,
	},
	{
		accessorKey: 'partName',
		header: () => <span className='whitespace-normal'>{'Tên vật tư'}</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>{row.original.partName ?? ''}</span>
		),
		size: 200,
	},
	{
		accessorKey: 'unitOfMeasure',
		header: () => <span className='whitespace-normal'>{'ĐVT'}</span>,
		cell: ({ row }) => row.original.unitOfMeasure ?? '',
		size: 100,
	},
	{
		accessorKey: 'quantity',
		header: () => <span className='whitespace-normal'>{'Số lượng'}</span>,
		cell: ({ row }) =>
			row.original.quantity !== undefined
				? formatNumber(row.original.quantity)
				: '',
		size: 120,
	},
];

// Other Materials Columns (Vật tư khác)
export const OTHER_MATERIALS_COLUMNS: ColumnDef<OtherMaterial>[] = [
	{
		accessorKey: 'materialCode',
		header: () => <span className='whitespace-normal'>{'Mã vật tư'}</span>,
		cell: ({ row }) => row.original.materialCode ?? '',
		size: 120,
	},
	{
		accessorKey: 'materialName',
		header: () => <span className='whitespace-normal'>{'Tên vật tư'}</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>
				{row.original.materialName ?? ''}
			</span>
		),
		size: 200,
	},
	{
		accessorKey: 'unitOfMeasure',
		header: () => <span className='whitespace-normal'>{'ĐVT'}</span>,
		cell: ({ row }) => row.original.unitOfMeasure ?? '',
		size: 100,
	},
	{
		accessorKey: 'quantity',
		header: () => <span className='whitespace-normal'>{'Số lượng'}</span>,
		cell: ({ row }) =>
			row.original.quantity !== undefined
				? formatNumber(row.original.quantity)
				: '',
		size: 120,
	},
];

// Mock data for development
export const MOCK_ADDITIONAL_COST_DATA: AdditionalCostDetail = {
	id: 'test-id',
	materials: [
		{
			contractCode: 'GK-001',
			items: [
				{
					id: 'mat-001',
					contractCode: 'GK-001',
					materialCode: 'VT-001',
					materialName: 'Thép xây dựng D10',
					unitOfMeasure: 'Tấn',
					quantity: 10,
				},
				{
					id: 'mat-002',
					contractCode: 'GK-001',
					materialCode: 'VT-002',
					materialName: 'Xi măng Portland',
					unitOfMeasure: 'Tấn',
					quantity: 10,
				},
			],
		},
		{
			contractCode: 'GK-002',
			items: [
				{
					id: 'mat-003',
					contractCode: 'GK-002',
					materialCode: 'VT-003',
					materialName: 'Cát xây dựng',
					unitOfMeasure: 'm³',
					quantity: 10,
				},
				{
					id: 'mat-004',
					contractCode: 'GK-002',
					materialCode: 'VT-004',
					materialName: 'Gạch ốp lát',
					unitOfMeasure: 'cái',
					quantity: 10,
				},
			],
		},
	],
	sctx: [
		{
			equipmentCode: 'TB-001',
			items: [
				{
					id: 'sctx-001',
					equipmentCode: 'TB-001',
					partCode: 'PT-001',
					partName: 'Dao cắt',
					unitOfMeasure: 'cái',
					quantity: 10,
				},
				{
					id: 'sctx-002',
					equipmentCode: 'TB-001',
					partCode: 'PT-002',
					partName: 'Mặt bích',
					unitOfMeasure: 'cái',
					quantity: 5,
				},
			],
		},
		{
			equipmentCode: 'TB-002',
			items: [
				{
					id: 'sctx-003',
					equipmentCode: 'TB-002',
					partCode: 'PT-003',
					partName: 'Lò xo nén',
					unitOfMeasure: 'cái',
					quantity: 10,
				},
				{
					id: 'sctx-004',
					equipmentCode: 'TB-002',
					partCode: 'PT-004',
					partName: 'Vòng đệm',
					unitOfMeasure: 'cái',
					quantity: 10,
				},
			],
		},
	],
	otherMaterials: [
		{
			id: 'other-mat-001',
			materialCode: 'VTK-001',
			materialName: 'Vật tư khác',
			unitOfMeasure: 'Cái',
			quantity: 10,
		},
		{
			id: 'other-mat-002',
			materialCode: 'VTK-002',
			materialName: 'Vật tư khác 2',
			unitOfMeasure: 'Cái',
			quantity: 10,
		},
	],
};
