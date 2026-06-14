import { DataTable } from '@/components/datatable';
import {
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { ContractCode } from '@/features/main/catalog/contract-code/columns';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import {
	ADDITIONAL_COST_COLUMNS,
	type AdditionalCostRow,
} from '@/features/main/cost/producttion/production/additional-cost/columns';
import {
	AcceptanceReportDetail,
	AcceptanceReportItem,
	AdditionalCost as AdditionalCostType,
	OtherMaterialDetail,
} from '@/features/main/cost/producttion/production/raw-acceptance-report/types';
import { api } from '@/lib/api';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';

type ProductionOrderLookupDto = {
	id: string;
	code: string;
	name: string;
};

type ProductionOrderDisplayInfo = {
	code: string;
	name: string;
};

const DEFAULT_GROUP_NAME = 'Không thuộc nhóm vật tư, tài sản';
const DEFAULT_MISC_LABEL = 'Vật tư khác';

const OTHER_MATERIAL_DETAIL_LABELS: Record<number, string> = {
	[OtherMaterialDetail.BaoHoLaoDong]: 'Bảo hộ lao động',
	[OtherMaterialDetail.VatTuPhucVuCongTacAnToan]:
		'Vật tư phục vụ công tác an toàn',
};

function formatCodeName(code?: string | null, name?: string | null) {
	return [code, name].filter(Boolean).join(' - ');
}

function getTrackedMaterialCode(item: AcceptanceReportItem) {
	return item.materialCode || item.trackedMaterialCode || item.partCode || '-';
}

function getTrackedMaterialName(item: AcceptanceReportItem) {
	return item.materialName || item.trackedMaterialName || item.partName || '-';
}

function resolveOrderInfo(
	item: AcceptanceReportItem,
	productionOrderById: Record<string, ProductionOrderDisplayInfo>,
) {
	if (!item.additionalCostProductionOrderId) {
		return null;
	}

	return (
		productionOrderById[item.additionalCostProductionOrderId] || {
			code: item.additionalCostProductionOrderId,
			name: item.additionalCostProductionOrderId,
		}
	);
}

function resolveGroupInfo(
	item: AcceptanceReportItem,
	assignmentById: Record<string, ContractCode>,
) {
	if (item.additionalCostAssignmentCodeId) {
		const assignment = assignmentById[item.additionalCostAssignmentCodeId];
		return {
			groupKey: item.additionalCostAssignmentCodeId,
			groupCode: assignment?.code || '',
			groupName:
				formatCodeName(assignment?.code, assignment?.name) ||
				DEFAULT_GROUP_NAME,
		};
	}

	if (item.additionalCost === AdditionalCostType.OtherMaterial) {
		const label =
			OTHER_MATERIAL_DETAIL_LABELS[item.otherMaterialDetail] ||
			DEFAULT_GROUP_NAME;
		return {
			groupKey: `other-${item.otherMaterialDetail || 'unknown'}`,
			groupCode: '',
			groupName: label,
		};
	}

	return {
		groupKey: 'ungrouped',
		groupCode: '',
		groupName: DEFAULT_GROUP_NAME,
	};
}

function buildAdditionalCostRows(params: {
	items: AcceptanceReportItem[];
	type: number;
	productionOrderById: Record<string, ProductionOrderDisplayInfo>;
	assignmentById: Record<string, ContractCode>;
}) {
	const { items, type, productionOrderById, assignmentById } = params;
	const orderMap = new Map<
		string,
		{
			orderInfo: ProductionOrderDisplayInfo | null;
			groups: Map<
				string,
				{
					groupCode: string;
					groupName: string;
					items: AcceptanceReportItem[];
				}
			>;
		}
	>();

	items
		.filter((item) => item.additionalCost === type)
		.forEach((item) => {
			const orderKey = item.additionalCostProductionOrderId || 'no-order';
			const orderInfo = resolveOrderInfo(item, productionOrderById);

			if (!orderMap.has(orderKey)) {
				orderMap.set(orderKey, {
					orderInfo,
					groups: new Map(),
				});
			}

			const { groupKey, groupCode, groupName } = resolveGroupInfo(
				item,
				assignmentById,
			);
			const orderGroup = orderMap.get(orderKey)!;

			if (!orderGroup.groups.has(groupKey)) {
				orderGroup.groups.set(groupKey, {
					groupCode,
					groupName,
					items: [],
				});
			}

			orderGroup.groups.get(groupKey)?.items.push(item);
		});

	const rows: AdditionalCostRow[] = [];
	let orderIndex = 0;

	orderMap.forEach((orderGroup, orderKey) => {
		const hasProductionOrder = orderKey !== 'no-order';
		const ungroupedItems: AcceptanceReportItem[] = [];
		let groupIndex = 0;
		let fallbackOrderEmitted = false;

		const emitFallbackOrderRow = () => {
			if (hasProductionOrder || fallbackOrderEmitted) {
				return;
			}

			orderIndex += 1;
			rows.push({
				id: `order-${type}-${orderKey}`,
				stt: `${orderIndex}`,
				rowType: 'order',
				code: 'VTK',
				name: DEFAULT_MISC_LABEL,
			});
			fallbackOrderEmitted = true;
		};

		if (hasProductionOrder) {
			orderIndex += 1;
			rows.push({
				id: `order-${type}-${orderKey}`,
				stt: `${orderIndex}`,
				rowType: 'order',
				code: orderGroup.orderInfo?.code || orderKey,
				name: orderGroup.orderInfo?.name || orderKey,
			});
		}

		orderGroup.groups.forEach((group, groupKey) => {
			const hasAssignmentGroup =
				group.groupCode.trim().length > 0 || group.groupName !== DEFAULT_GROUP_NAME;

			if (!hasAssignmentGroup) {
				ungroupedItems.push(...group.items);
				return;
			}

			emitFallbackOrderRow();
			groupIndex += 1;
			rows.push({
				id: `group-${type}-${orderKey}-${groupKey}`,
				stt: `${orderIndex}.${groupIndex}`,
				rowType: 'group',
				code: group.groupCode,
				name: group.groupName,
			});

			group.items.forEach((item) => {
				rows.push({
					id: `item-${type}-${item.id}`,
					rowType: 'item',
					code: getTrackedMaterialCode(item),
					name: getTrackedMaterialName(item),
					unitOfMeasure: item.unitOfMeasureName || '',
					quantity: item.additionalCostQuantity || 0,
				});
			});
		});

		if (!hasProductionOrder && ungroupedItems.length > 0) {
			emitFallbackOrderRow();

			ungroupedItems.forEach((item) => {
				rows.push({
					id: `item-${type}-${orderKey}-misc-${item.id}`,
					rowType: 'item',
					code: getTrackedMaterialCode(item),
					name: getTrackedMaterialName(item),
					unitOfMeasure: item.unitOfMeasureName || '',
					quantity: item.additionalCostQuantity || 0,
				});
			});
		}

		if (hasProductionOrder && ungroupedItems.length > 0) {
			ungroupedItems.forEach((item) => {
				rows.push({
					id: `item-${type}-${orderKey}-ungrouped-${item.id}`,
					rowType: 'item',
					code: getTrackedMaterialCode(item),
					name: getTrackedMaterialName(item),
					unitOfMeasure: item.unitOfMeasureName || '',
					quantity: item.additionalCostQuantity || 0,
				});
			});
		}
	});

	return rows;
}

export function AdditionalCost({
	output,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [materials, setMaterials] = useState<AdditionalCostRow[]>([]);
	const [maintainMaterials, setMaintainMaterials] = useState<
		AdditionalCostRow[]
	>([]);
	const [otherMaterials, setOtherMaterials] = useState<AdditionalCostRow[]>([]);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState<string | null>(null);

	useEffect(() => {
		if (!isOpen || !output?.acceptanceReportId) {
			return;
		}

		let active = true;

		const fetchAdditionalCost = async () => {
			setLoading(true);
			setError(null);

			try {
				const [reportResponse, productionOrderResponse, assignmentResponse] =
					await Promise.all([
						api.get<AcceptanceReportDetail>(
							API.PRODUCTION.ACCEPTANCE_REPORT.RAW_DETAIL(
								output.acceptanceReportId!,
							),
						),
						api.pagging<ProductionOrderLookupDto>(
							API.CATALOG.PARAMETER.PRODUCTION_ORDER.LIST,
							{ ignorePagination: true },
						),
						api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST, {
							ignorePagination: true,
						}),
					]);

				const items = reportResponse.result.items ?? [];
				const productionOrderById = Object.fromEntries(
					(productionOrderResponse.result.data ?? []).map((item) => [
						item.id,
						{
							code: item.code || item.id,
							name: item.name || item.code || item.id,
						} satisfies ProductionOrderDisplayInfo,
					]),
				);
				const assignmentById = Object.fromEntries(
					(assignmentResponse.result.data ?? []).map((item) => [item.id, item]),
				);

				if (!active) return;

				setMaterials(
					buildAdditionalCostRows({
						items,
						type: AdditionalCostType.Material,
						productionOrderById,
						assignmentById,
					}),
				);
				setMaintainMaterials(
					buildAdditionalCostRows({
						items,
						type: AdditionalCostType.Maintain,
						productionOrderById,
						assignmentById,
					}),
				);
				setOtherMaterials(
					buildAdditionalCostRows({
						items,
						type: AdditionalCostType.OtherMaterial,
						productionOrderById,
						assignmentById,
					}),
				);
			} catch (err) {
				if (!active) return;

				console.error('Failed to fetch additional cost:', err);
				setError(
					err instanceof Error
						? err.message
						: 'Không thể tải dữ liệu bổ sung chi phí',
				);
				setMaterials([]);
				setMaintainMaterials([]);
				setOtherMaterials([]);
			} finally {
				if (active) {
					setLoading(false);
				}
			}
		};

		fetchAdditionalCost();

		return () => {
			active = false;
		};
	}, [isOpen, output?.acceptanceReportId, reloadKey]);

	const displayedMaterials =
		isOpen && output?.acceptanceReportId ? materials : [];
	const displayedMaintainMaterials =
		isOpen && output?.acceptanceReportId ? maintainMaterials : [];
	const displayedOtherMaterials =
		isOpen && output?.acceptanceReportId ? otherMaterials : [];

	return (
		<AccordionItem
			value={'additional-cost'}
			className='min-w-0 overflow-hidden border-none'
		>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Bổ sung chi phí</ItemTitle>
				</ItemContent>
				<ItemActions>
					<div className='size-5'></div>
					<div className='size-5'></div>
					<div className='size-5'></div>
					<AccordionTrigger
						disabled={false}
						className='group p-0 disabled:opacity-50'
					>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
					</AccordionTrigger>
				</ItemActions>
			</Item>

			{isOpen && (
				<AccordionContent className='max-h-96 overflow-hidden overflow-y-auto p-0 px-2 pt-2'>
					<div className='w-full min-w-0 overflow-x-auto'>
						{loading ? (
							<div className='flex justify-center py-8'>
								<Spinner />
							</div>
						) : error ? (
							<div className='flex justify-center py-8 text-red-500'>
								<p>Lỗi tải dữ liệu: {error}</p>
							</div>
						) : (
							<div className='flex flex-col gap-6'>
								<div className='flex flex-col gap-2'>
									{displayedMaterials.length > 0 && (
										<>
											<h4 className='text-sm font-semibold'>Vật liệu</h4>
											<DataTable
												columns={ADDITIONAL_COST_COLUMNS}
												items={displayedMaterials}
												compact={true}
												hasActions={false}
												hasPagination={false}
												hasSort={false}
												hasIndex={false}
											/>
										</>
									)}
								</div>

								<div className='flex flex-col gap-2'>
									{displayedMaintainMaterials.length > 0 && (
										<>
											<h4 className='text-sm font-semibold'>SCTX</h4>
											<DataTable
												columns={ADDITIONAL_COST_COLUMNS}
												items={displayedMaintainMaterials}
												compact={true}
												hasActions={false}
												hasPagination={false}
												hasSort={false}
												hasIndex={false}
											/>
										</>
									)}
								</div>

								<div className='flex flex-col gap-2'>
									{displayedOtherMaterials.length > 0 && (
										<>
											<h4 className='text-sm font-semibold'>Vật tư khác</h4>
											<DataTable
												columns={ADDITIONAL_COST_COLUMNS}
												items={displayedOtherMaterials}
												compact={true}
												hasActions={false}
												hasPagination={false}
												hasSort={false}
												hasIndex={false}
											/>
										</>
									)}
								</div>
							</div>
						)}
					</div>
				</AccordionContent>
			)}
		</AccordionItem>
	);
}
