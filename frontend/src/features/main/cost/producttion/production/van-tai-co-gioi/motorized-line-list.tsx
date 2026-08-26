import { DataTable } from '@/components/datatable';
import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import { Item, ItemActions, ItemTitle } from '@/components/ui/item';
import { formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { ColumnDef } from '@tanstack/react-table';
import { useEffect, useMemo, useState } from 'react';
import type { MotorizedLineItem } from './types';

// Cấp 3: Dòng chi tiết bên trong bảng theo từng tổ hợp
export type MotorizedTableDetailRow = {
	id: string;
	equipmentQuality?: string;
	haulDistanceValue?: string;
	cargoTypeName?: string;
	receivingLocationName?: string;
	dumpingLocationName?: string;
	productionMeters: number;
	unitName?: string;
};

export const MOTORIZED_INNER_COLUMNS: ColumnDef<MotorizedTableDetailRow>[] = [
	{
		accessorKey: 'equipmentQuality',
		header: () => <span className='font-semibold'>Chất lượng</span>,
		cell: ({ row }) => {
			const q = row.original.equipmentQuality;
			if (!q) return <span className='text-gray-400'>-</span>;
			return <span className='text-xs font-medium text-black'>Loại {q}</span>;
		},
	},
	{
		accessorKey: 'haulDistanceValue',
		header: () => <span className='font-semibold'>Cung độ</span>,
		cell: ({ row }) => {
			const dist = row.original.haulDistanceValue;
			if (!dist) return <span className='text-gray-400'>-</span>;
			return (
				<span className='font-medium text-gray-800'>
					{dist.startsWith('L') ? dist : `L ≤ ${dist}`}
					{dist.includes('km') ? '' : ' km'}
				</span>
			);
		},
	},
	{
		id: 'parameters',
		header: () => <span className='font-semibold'>Thông số</span>,
		cell: ({ row }) => {
			const { cargoTypeName, receivingLocationName, dumpingLocationName } =
				row.original;
			const parts: string[] = [];
			if (cargoTypeName) {
				parts.push(cargoTypeName);
			}
			if (receivingLocationName || dumpingLocationName) {
				parts.push(
					`${receivingLocationName || '...'} → ${dumpingLocationName || '...'}`,
				);
			}

			if (parts.length === 0) {
				return <span className='text-gray-400'>-</span>;
			}

			return (
				<span className='text-xs font-normal text-gray-900'>
					{parts.join(' - ')}
				</span>
			);
		},
	},
	{
		accessorKey: 'productionMeters',
		header: () => (
			<div className='w-full pr-4 text-right font-semibold'>
				Sản lượng thực tế
			</div>
		),
		cell: ({ row }) => (
			<div className='w-full pr-4 text-right font-semibold text-gray-900'>
				{formatNumber(row.original.productionMeters ?? 0)}
			</div>
		),
	},
];

type ProcessGroupItem = {
	id: string;
	processId?: string;
	processCode?: string;
	processName: string;
	totalProductionMeters: number;
	unitName: string;
	rows: MotorizedTableDetailRow[];
};

type EquipmentGroupItem = {
	id: string;
	code?: string;
	name?: string;
	totalProductionMeters: number;
	processes: ProcessGroupItem[];
};

export function Motorized3TierTreeList({
	motorizedLines = [],
}: {
	motorizedLines: MotorizedLineItem[];
}) {
	const equipmentGroups = useMemo<EquipmentGroupItem[]>(() => {
		const eqMap = new Map<
			string,
			{
				id: string;
				code?: string;
				name?: string;
				totalProductionMeters: number;
				procMap: Map<string, ProcessGroupItem>;
			}
		>();

		(motorizedLines || []).forEach((line, index) => {
			const eqId =
				line.equipmentId || line.equipmentCode || `equipment-${index}`;
			const eqCode = line.equipmentCode;
			const eqName = line.equipmentName || 'Nhóm vật tư, tài sản';

			if (!eqMap.has(eqId)) {
				eqMap.set(eqId, {
					id: eqId,
					code: eqCode,
					name: eqName,
					totalProductionMeters: 0,
					procMap: new Map(),
				});
			}

			const eqGroup = eqMap.get(eqId)!;
			eqGroup.totalProductionMeters += line.productionMeters ?? 0;

			const procId =
				line.productionProcessId ||
				line.productionProcessCode ||
				line.productionProcessName ||
				`proc-${index}`;
			const procCode = line.productionProcessCode;
			const procName = line.productionProcessName || 'Công đoạn sản xuất';

			if (!eqGroup.procMap.has(procId)) {
				eqGroup.procMap.set(procId, {
					id: `${eqId}-${procId}`,
					processId: line.productionProcessId,
					processCode: procCode,
					processName: procName,
					totalProductionMeters: 0,
					unitName: line.unitName || 'tấn',
					rows: [],
				});
			}

			const procItem = eqGroup.procMap.get(procId)!;
			procItem.totalProductionMeters += line.productionMeters ?? 0;
			procItem.rows.push({
				id: line.id || `${eqId}-${procId}-${procItem.rows.length}`,
				equipmentQuality: line.equipmentQuality,
				haulDistanceValue: line.haulDistanceValue,
				cargoTypeName: line.cargoTypeName,
				receivingLocationName: line.receivingLocationName,
				dumpingLocationName: line.dumpingLocationName,
				productionMeters: line.productionMeters ?? 0,
				unitName: line.unitName || procItem.unitName,
			});
		});

		return Array.from(eqMap.values())
			.sort((a, b) =>
				(a.code || a.name || '').localeCompare(b.code || b.name || ''),
			)
			.map((eq) => ({
				id: eq.id,
				code: eq.code,
				name: eq.name,
				totalProductionMeters: eq.totalProductionMeters,
				processes: Array.from(eq.procMap.values())
					.sort((a, b) =>
						(a.processCode || a.processName || '').localeCompare(
							b.processCode || b.processName || '',
						),
					)
					.map((proc) => ({
						...proc,
						rows: [...proc.rows].sort((a, b) => {
							const qCompare = (a.equipmentQuality || '').localeCompare(
								b.equipmentQuality || '',
							);
							if (qCompare !== 0) return qCompare;
							const dCompare = (a.haulDistanceValue || '').localeCompare(
								b.haulDistanceValue || '',
							);
							if (dCompare !== 0) return dCompare;
							return (a.cargoTypeName || '').localeCompare(
								b.cargoTypeName || '',
							);
						}),
					})),
			}));
	}, [motorizedLines]);

	const [openedEquipments, setOpenedEquipments] = useState<string[]>([]);
	const [openedProcs, setOpenedProcs] = useState<string[]>([]);

	useEffect(() => {
		setOpenedEquipments(equipmentGroups.map((e) => e.id));
		const allProcIds = equipmentGroups.flatMap((e) =>
			e.processes.map((p) => p.id),
		);
		setOpenedProcs(allProcIds);
	}, [equipmentGroups]);

	if (equipmentGroups.length === 0) {
		return (
			<div className='text-muted-foreground flex min-h-24 items-center justify-center text-sm'>
				Chưa có dữ liệu sản phẩm vận tải cơ giới.
			</div>
		);
	}

	return (
		<Accordion
			type='multiple'
			className='flex w-full min-w-0 flex-col gap-2 pl-4'
			value={openedEquipments}
			onValueChange={setOpenedEquipments}
		>
			{equipmentGroups.map((eq) => (
				<AccordionItem
					key={eq.id}
					value={eq.id}
					className='min-w-0 overflow-hidden border-none'
				>
					{/* CẤP 1: NHÓM VẬT TƯ / THIẾT BỊ VTCG */}
					<Item
						variant={'outline'}
						className='relative w-full flex-1 rounded-sm bg-gray-200 py-1.5'
					>
						<div className='flex w-full items-center gap-4'>
							<div className='flex flex-1 items-center'>
								<ItemTitle className='text-sm font-bold text-black'>
									{eq.code
										? `${eq.code} - ${eq.name || ''}`
										: eq.name || 'Nhóm vật tư, tài sản'}
								</ItemTitle>
							</div>
							<div className='w-28 text-right text-sm font-bold text-black'>
								{formatNumber(eq.totalProductionMeters)}
							</div>
							<ItemActions>
								<AccordionTrigger className='group p-0'>
									<div className='group-data-[state=open]:hidden'>
										<VisibilityIcon className='size-4' />
									</div>
									<div className='hidden group-data-[state=open]:block'>
										<VisibilityOffIcon className='size-4' />
									</div>
								</AccordionTrigger>
							</ItemActions>
						</div>
					</Item>

					{/* CẤP 2: CÔNG ĐOẠN SẢN XUẤT */}
					<AccordionContent className='p-0 pt-2 pl-4'>
						<div className='w-full min-w-0 overflow-x-auto'>
							<Accordion
								type='multiple'
								className='flex w-full min-w-0 flex-col gap-2 pl-4'
								value={openedProcs}
								onValueChange={setOpenedProcs}
							>
								{eq.processes.map((proc) => (
									<AccordionItem
										key={proc.id}
										value={proc.id}
										className='min-w-0 overflow-hidden border-none'
									>
										<Item
											variant={'outline'}
											className='relative w-full flex-1 rounded-sm bg-gray-100 py-1'
										>
											<div className='flex w-full items-center gap-4'>
												<div className='flex flex-1 items-center gap-2'>
													<span className='h-2 w-2 rounded-full bg-blue-500' />
													<ItemTitle className='text-xs font-bold text-black'>
														{proc.processCode
															? `${proc.processCode} - ${proc.processName}`
															: proc.processName}
													</ItemTitle>
												</div>
												<div className='w-24 text-right text-xs font-bold text-black'>
													{formatNumber(proc.totalProductionMeters)}
												</div>
												<ItemActions>
													<AccordionTrigger className='group p-0'>
														<div className='group-data-[state=open]:hidden'>
															<VisibilityIcon className='size-3.5' />
														</div>
														<div className='hidden group-data-[state=open]:block'>
															<VisibilityOffIcon className='size-3.5' />
														</div>
													</AccordionTrigger>
												</ItemActions>
											</div>
										</Item>

										{/* CẤP 3: BẢNG CHI TIẾT CÁC TỔ HỢP TRONG CÔNG ĐOẠN */}
										<AccordionContent className='p-0 pt-1.5 pl-3'>
											<div className='w-full min-w-0 overflow-x-auto'>
												<DataTable
													columns={MOTORIZED_INNER_COLUMNS}
													items={proc.rows}
													hasActions={false}
													hasPagination={false}
													hasSort={false}
													hasIndex={false}
													compact={true}
												/>
											</div>
										</AccordionContent>
									</AccordionItem>
								))}
							</Accordion>
						</div>
					</AccordionContent>
				</AccordionItem>
			))}
		</Accordion>
	);
}
