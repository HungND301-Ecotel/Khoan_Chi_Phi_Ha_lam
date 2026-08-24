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

// Cấp 3: Bảng chi tiết sản lượng theo Cung độ / Vị trí / Loại hàng
type MotorizedDetailRow = {
	id: string;
	detailLabel: string;
	productionMeters: number;
	unitName: string;
};

const MOTORIZED_DETAIL_COLUMNS: ColumnDef<MotorizedDetailRow>[] = [
	{
		accessorKey: 'detailLabel',
		header: () => <span>Chi tiết cung độ / tuyến / loại hàng</span>,
		cell: ({ row }) => row.original.detailLabel,
	},
	{
		accessorKey: 'productionMeters',
		header: () => <span>Sản lượng thực tế</span>,
		cell: ({ row }) => (
			<div className='flex items-center gap-1 font-semibold text-gray-900'>
				<span>{formatNumber(row.original.productionMeters ?? 0)}</span>
				<span className='text-xs text-gray-500 font-normal'>{row.original.unitName}</span>
			</div>
		),
	},
];

type QualityProcessItem = {
	id: string;
	quality: string;
	processName: string;
	totalProductionMeters: number;
	unitName: string;
	lines: MotorizedLineItem[];
};

export function Motorized3TierTreeList({
	motorizedLines = [],
}: {
	motorizedLines: MotorizedLineItem[];
}) {
	const equipmentGroups = useMemo(() => {
		const eqMap = new Map<
			string,
			{
				id: string;
				code?: string;
				name?: string;
				totalProductionMeters: number;
				qpMap: Map<string, QualityProcessItem>;
			}
		>();

		(motorizedLines || []).forEach((line, index) => {
			const eqId =
				line.equipmentId || line.equipmentCode || `equipment-${index}`;
			const eqCode = line.equipmentCode;
			const eqName = line.equipmentName || 'Thiết bị cơ giới';

			if (!eqMap.has(eqId)) {
				eqMap.set(eqId, {
					id: eqId,
					code: eqCode,
					name: eqName,
					totalProductionMeters: 0,
					qpMap: new Map(),
				});
			}

			const eqGroup = eqMap.get(eqId)!;
			eqGroup.totalProductionMeters += line.productionMeters ?? 0;

			const q = line.equipmentQuality || 'A';
			const procName = line.productionProcessName || 'Vận hành sản xuất';
			const qpKey = `${q}__${line.productionProcessId || procName}`;

			if (!eqGroup.qpMap.has(qpKey)) {
				eqGroup.qpMap.set(qpKey, {
					id: `${eqId}-${qpKey}`,
					quality: q,
					processName: procName,
					totalProductionMeters: 0,
					unitName: line.unitName || 'tấn',
					lines: [],
				});
			}

			const qpItem = eqGroup.qpMap.get(qpKey)!;
			qpItem.totalProductionMeters += line.productionMeters ?? 0;
			qpItem.lines.push(line);
		});

		return Array.from(eqMap.values()).map((eq) => ({
			id: eq.id,
			code: eq.code,
			name: eq.name,
			totalProductionMeters: eq.totalProductionMeters,
			qualityProcesses: Array.from(eq.qpMap.values()),
		}));
	}, [motorizedLines]);

	const [openedEquipments, setOpenedEquipments] = useState<string[]>([]);
	const [openedQPs, setOpenedQPs] = useState<string[]>([]);

	useEffect(() => {
		setOpenedEquipments(equipmentGroups.map((e) => e.id));
		const allQpIds = equipmentGroups.flatMap((e) =>
			e.qualityProcesses.map((qp) => qp.id),
		);
		setOpenedQPs(allQpIds);
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
										: eq.name || 'Thiết bị chưa xác định'}
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

					{/* CẤP 2: CHẤT LƯỢNG & CÔNG ĐOẠN */}
					<AccordionContent className='p-0 pt-2 pl-4'>
						<div className='w-full min-w-0 overflow-x-auto'>
							<Accordion
								type='multiple'
								className='flex w-full min-w-0 flex-col gap-2 pl-4'
								value={openedQPs}
								onValueChange={setOpenedQPs}
							>
								{eq.qualityProcesses.map((qp) => {
									const detailRows: MotorizedDetailRow[] = qp.lines.map((line, idx) => {
										const parts: string[] = [];
										if (line.cargoTypeName) parts.push(`Hàng: ${line.cargoTypeName}`);
										if (line.receivingLocationName || line.dumpingLocationName) {
											parts.push(
												`${line.receivingLocationName || '...'} → ${line.dumpingLocationName || '...'}`
											);
										}
										if (line.haulDistanceValue) {
											parts.push(`Cung độ: ${line.haulDistanceValue} km`);
										}
										const detailLabel = parts.length > 0 ? parts.join(' | ') : 'Sản lượng công đoạn';

										return {
											id: `${qp.id}-${idx}`,
											detailLabel,
											productionMeters: line.productionMeters ?? 0,
											unitName: qp.unitName,
										};
									});

									return (
										<AccordionItem
											key={qp.id}
											value={qp.id}
											className='min-w-0 overflow-hidden border-none'
										>
											<Item
												variant={'outline'}
												className='relative w-full flex-1 rounded-sm bg-gray-100 py-1'
											>
												<div className='flex w-full items-center gap-4'>
													<div className='flex flex-1 items-center gap-2'>
														<span className='rounded-xs bg-blue-100 px-1.5 py-0.5 text-xs font-bold text-blue-800'>
															Loại {qp.quality}
														</span>
														<ItemTitle className='text-xs font-bold text-black'>
															{qp.processName}
														</ItemTitle>
													</div>
													<div className='w-24 text-right text-xs font-bold text-black'>
														{formatNumber(qp.totalProductionMeters)}
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

											{/* CẤP 3: CHI TIẾT SẢN LƯỢNG */}
											<AccordionContent className='p-0 pt-1.5 pl-3'>
												<div className='w-full min-w-0 overflow-x-auto'>
													<DataTable
														columns={MOTORIZED_DETAIL_COLUMNS}
														items={detailRows}
														hasActions={false}
														hasPagination={false}
														hasSort={false}
														hasIndex={false}
														compact={true}
													/>
												</div>
											</AccordionContent>
										</AccordionItem>
									);
								})}
							</Accordion>
						</div>
					</AccordionContent>
				</AccordionItem>
			))}
		</Accordion>
	);
}
