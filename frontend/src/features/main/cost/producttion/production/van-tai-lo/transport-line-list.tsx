import { DataTable } from '@/components/datatable';
import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import { Item, ItemActions, ItemTitle } from '@/components/ui/item';
import {
	detectTransportMode,
	isMonorailMode,
	isOtherMode,
} from '@/features/main/cost/plan/van-tai-lo/utils';
import { formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { ColumnDef } from '@tanstack/react-table';
import { useEffect, useMemo, useState } from 'react';

export type ProductionOutputTransportLineRow = {
	productionProcessId: string;
	productionProcessCode?: string;
	productionProcessName?: string;
	equipmentId?: string;
	equipmentCode?: string;
	equipmentName?: string;
	equipmentQuality?: string;
	transportRouteId?: string;
	transportRouteCode?: string;
	transportRouteName?: string;
	routeDepartmentId?: string;
	routeDepartmentCode?: string;
	routeDepartmentName?: string;
	productionMeters: number;
	standardProductionMeters?: number;
};

// --- CẤP 3: BẢNG CHI TIẾT CHẤT LƯỢNG CHO MONORAY ---
type MonorailQualityRow = {
	id: string;
	quality: string;
	productionMeters: number;
};

const MONORAIL_QUALITY_COLUMNS: ColumnDef<MonorailQualityRow>[] = [
	{
		accessorKey: 'quality',
		header: () => <span>Chất lượng thiết bị</span>,
		cell: ({ row }) => row.original.quality,
	},
	{
		accessorKey: 'productionMeters',
		header: () => <span>Sản lượng thực tế</span>,
		cell: ({ row }) => formatNumber(row.original.productionMeters ?? 0),
	},
];

// --- CẤP 3: BẢNG CHI TIẾT ĐƠN VỊ CHO TUYẾN VẬN TẢI ---
type RouteDepartmentRow = {
	id: string;
	department: string;
	productionMeters: number;
};

const ROUTE_DEPARTMENT_COLUMNS: ColumnDef<RouteDepartmentRow>[] = [
	{
		accessorKey: 'department',
		header: () => <span>Đơn vị</span>,
		cell: ({ row }) => row.original.department,
	},
	{
		accessorKey: 'productionMeters',
		header: () => <span>Sản lượng thực tế</span>,
		cell: ({ row }) => formatNumber(row.original.productionMeters ?? 0),
	},
];

// --- BẢNG CHO THIẾT BỊ KHÁC ---
type OtherEquipmentRow = {
	id: string;
	equipmentCode: string;
	equipmentName: string;
	productionMeters: number;
};

const OTHER_EQUIPMENT_COLUMNS: ColumnDef<OtherEquipmentRow>[] = [
	{
		accessorKey: 'equipmentCode',
		header: () => <span>Mã thiết bị</span>,
	},
	{
		accessorKey: 'equipmentName',
		header: () => <span>Tên thiết bị</span>,
		cell: ({ row }) => (
			<span className='whitespace-normal'>{row.original.equipmentName}</span>
		),
	},
	{
		accessorKey: 'productionMeters',
		header: () => <span>Sản lượng thực tế</span>,
		cell: ({ row }) => formatNumber(row.original.productionMeters ?? 0),
	},
];

type EntityGroupItem = {
	id: string;
	code?: string;
	name?: string;
	entityType: 'equipment' | 'route';
	totalStandardMeters?: number;
	totalProductionMeters: number;
	lines: ProductionOutputTransportLineRow[];
};

export type ProcessGroupItem = {
	id: string;
	code?: string;
	name?: string;
	totalStandardMeters?: number;
	totalProductionMeters: number;
	entities: EntityGroupItem[];
	rawLines: ProductionOutputTransportLineRow[];
};

export function Vtl3TierTreeList({
	transportLines,
}: {
	transportLines: ProductionOutputTransportLineRow[];
}) {
	const processGroups = useMemo(() => {
		const processMap = new Map<
			string,
			{
				id: string;
				code?: string;
				name?: string;
				totalStandardMeters: number;
				totalProductionMeters: number;
				entityMap: Map<string, EntityGroupItem>;
				rawLines: ProductionOutputTransportLineRow[];
			}
		>();

		(transportLines || []).forEach((line, index) => {
			const processId =
				line.productionProcessId ||
				line.productionProcessCode ||
				`process-${index}`;
			const processCode = line.productionProcessCode;
			const processName = line.productionProcessName;

			if (!processMap.has(processId)) {
				processMap.set(processId, {
					id: processId,
					code: processCode,
					name: processName,
					totalStandardMeters: 0,
					totalProductionMeters: 0,
					entityMap: new Map(),
					rawLines: [],
				});
			}

			const pGroup = processMap.get(processId)!;
			pGroup.totalStandardMeters += line.standardProductionMeters ?? 0;
			pGroup.totalProductionMeters += line.productionMeters ?? 0;
			pGroup.rawLines.push(line);

			const isRoute = Boolean(
				line.transportRouteId ||
					line.transportRouteCode ||
					line.transportRouteName,
			);
			const entityId = isRoute
				? (line.transportRouteId || line.transportRouteCode || `route-${index}`)
				: (line.equipmentId || line.equipmentCode || `eq-${index}`);
			const entityCode = isRoute
				? line.transportRouteCode
				: line.equipmentCode;
			const entityName = isRoute
				? line.transportRouteName
				: line.equipmentName;

			if (!pGroup.entityMap.has(entityId)) {
				pGroup.entityMap.set(entityId, {
					id: entityId,
					code: entityCode,
					name: entityName,
					entityType: isRoute ? 'route' : 'equipment',
					totalStandardMeters: 0,
					totalProductionMeters: 0,
					lines: [],
				});
			}

			const entityGroup = pGroup.entityMap.get(entityId)!;
			entityGroup.totalStandardMeters =
				(entityGroup.totalStandardMeters ?? 0) +
				(line.standardProductionMeters ?? 0);
			entityGroup.totalProductionMeters += line.productionMeters ?? 0;
			entityGroup.lines.push(line);
		});

		return Array.from(processMap.values()).map((p) => ({
			id: p.id,
			code: p.code,
			name: p.name,
			totalStandardMeters: p.totalStandardMeters,
			totalProductionMeters: p.totalProductionMeters,
			entities: Array.from(p.entityMap.values()),
			rawLines: p.rawLines,
		}));
	}, [transportLines]);

	const [openedProcesses, setOpenedProcesses] = useState<string[]>([]);
	const [openedEntities, setOpenedEntities] = useState<string[]>([]);

	useEffect(() => {
		setOpenedProcesses(processGroups.map((p) => p.id));
		const allEntityIds = processGroups.flatMap((p) =>
			p.entities.map((e) => e.id),
		);
		setOpenedEntities(allEntityIds);
	}, [processGroups]);

	if (processGroups.length === 0) {
		return (
			<div className='text-muted-foreground flex min-h-24 items-center justify-center text-sm'>
				Chưa có dữ liệu sản phẩm vận tải lò.
			</div>
		);
	}

	return (
		<Accordion
			type='multiple'
			className='flex w-full min-w-0 flex-col gap-2 pl-4'
			value={openedProcesses}
			onValueChange={setOpenedProcesses}
		>
			{processGroups.map((process) => {
				const mode = detectTransportMode(process.code, process.name);
				const isMonorail = isMonorailMode(mode);
				const isOther = isOtherMode(mode);

				const otherRows: OtherEquipmentRow[] = isOther
					? process.rawLines.map((line, idx) => ({
							id: `${process.id}-${line.equipmentId || line.equipmentCode || idx}`,
							equipmentCode: line.equipmentCode ?? '-',
							equipmentName: line.equipmentName ?? '-',
							productionMeters: line.productionMeters ?? 0,
						}))
					: [];

				return (
					<AccordionItem
						key={process.id}
						value={process.id}
						className='min-w-0 overflow-hidden border-none'
					>
						{/* CẤP 1: CÔNG ĐOẠN SẢN XUẤT */}
						<Item
							variant={'outline'}
							className='relative w-full flex-1 rounded-sm bg-gray-200 py-1.5'
						>
							<div className='flex w-full items-center gap-4'>
								<div className='flex flex-1 items-center'>
									<ItemTitle className='text-sm font-bold text-black'>
										{process.code
											? `${process.code} - ${process.name || ''}`
											: process.name || 'Công đoạn chưa xác định'}
									</ItemTitle>
								</div>
								<div className='me-2 w-12 text-right text-sm font-bold text-black'>
									{process.totalStandardMeters && process.totalStandardMeters > 0
										? formatNumber(process.totalStandardMeters)
										: ''}
								</div>
								<div className='w-24 text-right text-sm font-bold text-black'>
									{formatNumber(process.totalProductionMeters)}
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

						<AccordionContent className='p-0 pt-2 pl-4'>
							<div className='w-full min-w-0 overflow-x-auto'>
								{isOther ? (
									<DataTable
										columns={OTHER_EQUIPMENT_COLUMNS}
										items={otherRows}
										hasActions={false}
										hasPagination={false}
										hasSort={false}
										hasIndex={false}
										compact={true}
									/>
								) : (
									/* CẤP 2: DANH SÁCH THIẾT BỊ / TUYẾN */
									<Accordion
										type='multiple'
										className='flex w-full min-w-0 flex-col gap-2 pl-4'
										value={openedEntities}
										onValueChange={setOpenedEntities}
									>
										{process.entities.map((entity) => {
											const monorailQualityRows: MonorailQualityRow[] =
												isMonorail
													? entity.lines.map((line, idx) => ({
															id: `${entity.id}-${line.equipmentQuality || idx}`,
															quality: line.equipmentQuality
																? `Thiết bị loại ${line.equipmentQuality}`
																: '-',
															productionMeters: line.productionMeters ?? 0,
														}))
													: [];

											const routeDeptRows: RouteDepartmentRow[] =
												!isMonorail
													? entity.lines.map((line, idx) => ({
															id: `${entity.id}-${line.routeDepartmentId || idx}`,
															department:
																[
																	line.routeDepartmentCode,
																	line.routeDepartmentName,
																]
																	.filter(Boolean)
																	.join(' - ') || '-',
															productionMeters: line.productionMeters ?? 0,
														}))
													: [];

											return (
												<AccordionItem
													key={entity.id}
													value={entity.id}
													className='min-w-0 overflow-hidden border-none'
												>
													<Item
														variant={'outline'}
														className='relative w-full flex-1 rounded-sm bg-gray-100 py-1'
													>
														<div className='flex w-full items-center gap-4'>
															<div className='flex flex-1 items-center'>
																<ItemTitle className='text-xs font-bold text-black'>
																	{entity.code
																		? `${entity.code} - ${entity.name || ''}`
																		: entity.name || 'Chưa xác định'}
																</ItemTitle>
															</div>
															<div className='me-2 w-12 text-right text-xs font-bold text-black'>
																{entity.totalStandardMeters &&
																entity.totalStandardMeters > 0
																	? formatNumber(entity.totalStandardMeters)
																	: ''}
															</div>
															<div className='w-24 text-right text-xs font-bold text-black'>
																{formatNumber(entity.totalProductionMeters)}
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

													{/* CẤP 3: BẢNG CHI TIẾT BÊN TRONG CẤP 2 */}
													<AccordionContent className='p-0 pt-1.5 pl-3'>
														<div className='w-full min-w-0 overflow-x-auto'>
															{isMonorail ? (
																<DataTable
																	columns={MONORAIL_QUALITY_COLUMNS}
																	items={monorailQualityRows}
																	hasActions={false}
																	hasPagination={false}
																	hasSort={false}
																	hasIndex={false}
																	compact={true}
																/>
															) : (
																<DataTable
																	columns={ROUTE_DEPARTMENT_COLUMNS}
																	items={routeDeptRows}
																	hasActions={false}
																	hasPagination={false}
																	hasSort={false}
																	hasIndex={false}
																	compact={true}
																/>
															)}
														</div>
													</AccordionContent>
												</AccordionItem>
											);
										})}
									</Accordion>
								)}
							</div>
						</AccordionContent>
					</AccordionItem>
				);
			})}
		</Accordion>
	);
}



