import { DataTable } from '@/components/datatable';
import { DataTableEditDialog } from '@/components/datatable/edit';
import {
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import { Button } from '@/components/ui/button';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { LowValuePerishableSupplyInclusion } from '@/constants/low-value-perishable-supply';
import { ProcessGroupType } from '@/constants/process-group';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import { type NormFactor } from '@/features/main/catalog/norm-factor/columns';
import { Clamp } from '@/features/main/catalog/parameter/clamp/columns';
import {
	getPlanedMaterialCostSummaryColumns,
	getPlannedMaterialDetail,
	PLANNED_MATERIAL_BREAKDOWN_COLUMNS,
	type PlannedMaterialBreakdownRow,
	PlanedMaterialCostSummary,
	PlanedMaterialCostType,
} from '@/features/main/cost/plan/planed-material-cost/columns';
import { PlanMaterialCostForm } from '@/features/main/cost/plan/planed-material-cost/form';
import { UnifiedMaterial } from '@/features/main/cost/plan/planed-material-cost/type';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import { LongwallMaterialDetail } from '@/features/main/pricing/longwall-panel/material/type';
import {
	MaterialDetail,
	MaterialDetailCost,
} from '@/features/main/pricing/tunneling/material/type';
import {
	Slide,
	SlideDetail,
	SlideDetailMaterialCost,
} from '@/features/main/pricing/tunneling/slide/columns';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import AddIcon from '@mui/icons-material/Add';
import CreateIcon from '@mui/icons-material/Create';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';

type MaterialBreakdownCost = MaterialDetailCost & {
	assignmentCodeId: string;
};

type MaterialBreakdownDetail = {
	costs: MaterialBreakdownCost[];
	otherMaterialValue?: number;
};

type SlideBreakdownCost = MaterialBreakdownCost & {
	detailId: string;
	amount: number;
};

function parseMonth(value?: string) {
	if (!value) return null;
	return new Date(value.slice(0, 7));
}

function isTimeCovered(
	targetStart?: string,
	targetEnd?: string,
	sourceStart?: string,
	sourceEnd?: string,
) {
	const tStart = parseMonth(targetStart);
	const tEnd = parseMonth(targetEnd);
	const sStart = parseMonth(sourceStart);
	const sEnd = parseMonth(sourceEnd);

	if (!tStart || !tEnd || !sStart || !sEnd) return false;
	return sStart <= tStart && sEnd >= tEnd;
}

function getPlannedMaterialDetailUrl(
	materialUnitPriceId: string,
	fixedKeyType?: number,
) {
	if (fixedKeyType === ProcessGroupType.DL) {
		return API.PRICING.MATERIAL.TUNNELING.DETAIL(materialUnitPriceId);
	}

	if (fixedKeyType === ProcessGroupType.XL) {
		return API.PRICING.MATERIAL.TRIMMING.DETAIL(materialUnitPriceId);
	}

	if (fixedKeyType === ProcessGroupType.LC) {
		return API.PRICING.MATERIAL.LONGWALL_PANEL.DETAIL(materialUnitPriceId);
	}

	return null;
}

function normalizeMaterialDetail(
	detail?: MaterialDetail | LongwallMaterialDetail | null,
): MaterialBreakdownDetail {
	if (!detail) {
		return { costs: [] };
	}

	return {
		costs: (detail.costs ?? []).map((cost) => ({
			assignmentCodeId: cost.assignmentCodeId,
			assignmentCode: cost.assignmentCode,
			assignmentCodeName: cost.assignmentCodeName,
			materialId: cost.materialId,
			materialCode: cost.materialCode,
			materialName: cost.materialName,
			unitOfMeasureName: cost.unitOfMeasureName,
			unitPrice: cost.unitPrice,
			norm: cost.norm,
			totalPrice: cost.totalPrice,
		})),
		otherMaterialValue: detail.otherMaterialValue,
	};
}

function flattenSlideDetailCosts(
	detail?: SlideDetail | null,
): SlideBreakdownCost[] {
	if (!detail) return [];

	return detail.materialCost.flatMap((group) =>
		group.costs.map((cost: SlideDetailMaterialCost) => ({
			detailId: cost.id,
			assignmentCodeId: group.assignmentCodeId,
			assignmentCode: group.assignmentCode,
			assignmentCodeName:
				group.assignmentCodeName ||
				(group.assignmentCode === 'MT' ? 'Máng trượt' : ''),
			materialId: cost.materialId,
			materialCode: cost.materialCode,
			materialName: cost.materialName,
			unitOfMeasureName: cost.unitOfMeasureName,
			unitPrice: cost.cost,
			norm: cost.amount,
			amount: cost.amount,
			totalPrice: cost.amount,
		})),
	);
}

function resolveMatchedSlide(
	slides: Slide[],
	selectedMaterial?: UnifiedMaterial,
	processGroupId?: string,
) {
	if (!selectedMaterial || !processGroupId) return undefined;

	const { startMonth, endMonth, passportId, hardnessId } = selectedMaterial;

	return slides.find((slide) => {
		const targetStart = new Date(startMonth.slice(0, 7));
		const targetEnd = new Date(endMonth.slice(0, 7));
		const slideStart = new Date(slide.startMonth.slice(0, 7));
		const slideEnd = new Date(slide.endMonth.slice(0, 7));

		if (processGroupId !== slide.processGroupId) return false;
		if (passportId !== slide.passportId) return false;
		if (hardnessId !== slide.hardnessId) return false;

		return slideStart <= targetStart && slideEnd >= targetEnd;
	});
}

function resolveTargetMaterial(
	materials: UnifiedMaterial[],
	selectedMaterial: UnifiedMaterial,
	targetHardnessId: string,
	fixedKeyType?: number,
	effectiveStartMonth?: string,
	effectiveEndMonth?: string,
) {
	if (
		fixedKeyType !== ProcessGroupType.DL &&
		fixedKeyType !== ProcessGroupType.XL
	) {
		return undefined;
	}

	const selectedTechnologyId = selectedMaterial.technologyId || '';

	return materials
		.filter((material) => {
			if (material.type !== selectedMaterial.type) return false;
			if (material.processId !== selectedMaterial.processId) return false;
			if (material.passportId !== selectedMaterial.passportId) return false;
			if (material.insertItemId !== selectedMaterial.insertItemId) return false;
			if (material.supportStepId !== selectedMaterial.supportStepId)
				return false;
			if ((material.technologyId || '') !== selectedTechnologyId) return false;
			if (material.hardnessId !== targetHardnessId) return false;

			return isTimeCovered(
				effectiveStartMonth,
				effectiveEndMonth,
				material.startMonth,
				material.endMonth,
			);
		})
		.sort((a, b) => {
			const startDiff =
				(parseMonth(b.startMonth)?.getTime() || 0) -
				(parseMonth(a.startMonth)?.getTime() || 0);

			if (startDiff !== 0) return startDiff;

			return (
				(parseMonth(b.endMonth)?.getTime() || 0) -
				(parseMonth(a.endMonth)?.getTime() || 0)
			);
		})[0];
}

function buildPlannedMaterialBreakdownRows(params: {
	currentDetail: MaterialBreakdownDetail;
	slideCost?: SlideBreakdownCost;
	normFactorAssignments?: NormFactor['assignmentCodes'];
	targetCostsByAssignmentId: Map<string, MaterialBreakdownCost[]>;
	materialCostTotal?: number | null;
}) {
	const {
		currentDetail,
		slideCost,
		normFactorAssignments,
		targetCostsByAssignmentId,
		materialCostTotal,
	} = params;
	const currentGroups = new Map<string, MaterialBreakdownCost[]>();
	const normAssignments = normFactorAssignments ?? [];
	const affectedAssignmentIds = new Set(
		normAssignments.map((item) => item.assignmentCodeId),
	);

	currentDetail.costs.forEach((cost) => {
		const existingGroup = currentGroups.get(cost.assignmentCodeId) ?? [];
		existingGroup.push(cost);
		currentGroups.set(cost.assignmentCodeId, existingGroup);
	});

	const orderedAssignmentIds = Array.from(currentGroups.keys());
	targetCostsByAssignmentId.forEach((_, assignmentCodeId) => {
		if (!orderedAssignmentIds.includes(assignmentCodeId)) {
			orderedAssignmentIds.push(assignmentCodeId);
		}
	});

	if (
		slideCost?.assignmentCodeId &&
		!orderedAssignmentIds.includes(slideCost.assignmentCodeId)
	) {
		orderedAssignmentIds.push(slideCost.assignmentCodeId);
	}

	const rows: PlannedMaterialBreakdownRow[] = [];
	const materialRowsForOther: PlannedMaterialBreakdownRow[] = [];

	orderedAssignmentIds.forEach((assignmentCodeId) => {
		const normAssignment = normAssignments.find(
			(item) => item.assignmentCodeId === assignmentCodeId,
		);
		const currentRows = currentGroups.get(assignmentCodeId) ?? [];
		const targetRows = normAssignment?.targetHardnessId
			? (targetCostsByAssignmentId.get(assignmentCodeId) ?? [])
			: [];
		const selectedRows = targetRows.length > 0 ? targetRows : currentRows;
		const itemRows: PlannedMaterialBreakdownRow[] = [];

		if (slideCost?.assignmentCodeId === assignmentCodeId) {
			itemRows.push({
				rowType: 'material-item',
				assignmentCodeId,
				assignmentCode: slideCost.assignmentCode,
				assignmentCodeName: slideCost.assignmentCodeName,
				materialId: slideCost.materialId,
				materialCode: slideCost.materialCode,
				materialName: slideCost.materialName,
				unitPrice: slideCost.amount,
				originalQuantity: null,
				coefficientValue: 1,
				totalPrice: slideCost.totalPrice,
			});
		}

		selectedRows.forEach((row) => {
			const coefficientValue = normAssignment?.value ?? 1;
			const totalPrice = row.totalPrice * coefficientValue;

			const breakdownRow: PlannedMaterialBreakdownRow = {
				rowType: 'material-item',
				assignmentCodeId,
				assignmentCode: row.assignmentCode,
				assignmentCodeName: row.assignmentCodeName,
				materialId: row.materialId,
				materialCode: row.materialCode,
				materialName: row.materialName,
				unitPrice: row.unitPrice,
				originalQuantity: row.norm,
				coefficientValue,
				totalPrice,
			};

			itemRows.push(breakdownRow);
			materialRowsForOther.push(breakdownRow);
		});

		if (!itemRows.length) return;

		rows.push({
			rowType: 'group-summary',
			assignmentCodeId,
			assignmentCode: itemRows[0].assignmentCode,
			assignmentCodeName: itemRows[0].assignmentCodeName,
			totalPrice: itemRows.reduce((sum, item) => sum + item.totalPrice, 0),
		});
		rows.push(...itemRows);
	});

	const otherMaterialRate = Number(currentDetail.otherMaterialValue ?? 0);
	const renderedMaterialTotal = materialRowsForOther.reduce(
		(sum, row) => sum + row.totalPrice,
		0,
	);

	const hasMaterialCostTotal =
		materialCostTotal !== null &&
		materialCostTotal !== undefined &&
		!Number.isNaN(materialCostTotal);

	if (!otherMaterialRate && !hasMaterialCostTotal) return rows;

	const baseRows =
		affectedAssignmentIds.size > 0
			? materialRowsForOther.filter((row) =>
					affectedAssignmentIds.has(row.assignmentCodeId),
				)
			: materialRowsForOther;

	const fallbackOtherMaterialTotal =
		(baseRows.reduce((sum, row) => sum + row.totalPrice, 0) *
			otherMaterialRate) /
		100;
	const otherMaterialTotal = hasMaterialCostTotal
		? Math.max((materialCostTotal as number) - renderedMaterialTotal, 0)
		: fallbackOtherMaterialTotal;
	const otherMaterialSummaryRow: PlannedMaterialBreakdownRow = {
		rowType: 'group-summary',
		assignmentCodeId: 'VTK',
		assignmentCode: 'VTK',
		assignmentCodeName: 'Vật tư khác',
		totalPrice: otherMaterialTotal,
	};

	return [...rows, otherMaterialSummaryRow];
}

export function PlanedMaterialCost({
	id,
	output,
	plan,
	callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [summary, setSummary] = useState<PlanedMaterialCostSummary[]>([]);
	const [breakdownRows, setBreakdownRows] = useState<
		PlannedMaterialBreakdownRow[]
	>([]);
	const [plannedMaterialPrice, setPlannedMaterialPrice] = useState<number>(0);
	const [total, setTotal] = useState<number>(0);
	const [loading, setLoading] = useState<boolean>(false);

	useEffect(() => {
		if (!id) return;

		let active = true;

		const fetchData = async () => {
			setLoading(true);

			try {
				const [detailRes, clampsRes, materialsRes, slidesRes] =
					await Promise.all([
						api.get<PlanedMaterialCostType>(
							API.COST.PLANNED_MATERIAL.DETAIL(id),
						),
						api.pagging<Clamp>(API.CATALOG.PARAMETER.CLAMP.LIST),
						api.pagging<UnifiedMaterial>(API.PRICING.MATERIAL.ALL),
						api.pagging<Slide>(API.PRICING.SLIDE.LIST),
					]);

				const { result } = detailRes;

				const allClamps = clampsRes.result.data;
				const allMaterials = materialsRes.result.data;
				const allSlides = slidesRes.result.data;

				const filteredMaterials = allMaterials.filter((material) => {
					const groupType = plan?.fixedKeyType;
					if (groupType === ProcessGroupType.DL) {
						return material.type === 1;
					}
					if (groupType === ProcessGroupType.LC) {
						return material.type === 2;
					}
					if (groupType === ProcessGroupType.XL) {
						return material.type === 4;
					}
					return true;
				});

				const selectedMaterial =
					filteredMaterials.find(
						(material) => material.id === result.materialUnitPriceId,
					) ||
					allMaterials.find(
						(material) => material.id === result.materialUnitPriceId,
					);

				const detailUrl = selectedMaterial
					? getPlannedMaterialDetailUrl(selectedMaterial.id, plan?.fixedKeyType)
					: null;
				const matchedSlide =
					plan?.fixedKeyType === ProcessGroupType.DL && selectedMaterial && plan
						? resolveMatchedSlide(
								allSlides,
								selectedMaterial,
								plan.processGroupId,
							)
						: undefined;

				const [materialDetailRes, normFactorRes, slideDetailRes] =
					await Promise.all([
						detailUrl
							? api.get<MaterialDetail | LongwallMaterialDetail>(detailUrl)
							: Promise.resolve(null),
						result.normFactorId
							? api.get<NormFactor>(
									API.CATALOG.NORM_FACTOR.DETAIL(result.normFactorId),
								)
							: Promise.resolve(null),
						matchedSlide
							? api.get<SlideDetail>(API.PRICING.SLIDE.DETAIL(matchedSlide.id))
							: Promise.resolve(null),
					]);

				const normalizedMaterialDetail = normalizeMaterialDetail(
					materialDetailRes?.result,
				);
				const normFactor = normFactorRes?.result;
				const flattenedSlideCosts = flattenSlideDetailCosts(
					slideDetailRes?.result,
				);
				const selectedSlideCost = flattenedSlideCosts.find(
					(item) => item.detailId === result.slideUnitPriceAssignmentCodeId,
				);
				const targetCostsByAssignmentId = new Map<
					string,
					MaterialBreakdownCost[]
				>();

				if (
					selectedMaterial &&
					normFactor?.assignmentCodes?.length &&
					output?.startMonth &&
					output?.endMonth
				) {
					const targetMaterials = normFactor.assignmentCodes
						.filter((assignment) => assignment.targetHardnessId)
						.map((assignment) => ({
							assignmentCodeId: assignment.assignmentCodeId,
							targetMaterial: resolveTargetMaterial(
								allMaterials,
								selectedMaterial,
								String(assignment.targetHardnessId),
								plan?.fixedKeyType,
								output.startMonth,
								output.endMonth,
							),
						}))
						.filter(
							(
								item,
							): item is {
								assignmentCodeId: string;
								targetMaterial: UnifiedMaterial;
							} => Boolean(item.targetMaterial),
						);

					const uniqueTargetMaterials = Array.from(
						new Map(
							targetMaterials.map((item) => [
								item.targetMaterial.id,
								item.targetMaterial,
							]),
						).values(),
					);

					const targetDetailResponses: Array<
						readonly [string, MaterialBreakdownDetail]
					> = await Promise.all(
						uniqueTargetMaterials.map(
							async (
								material,
							): Promise<readonly [string, MaterialBreakdownDetail]> => {
								const targetDetailUrl = getPlannedMaterialDetailUrl(
									material.id,
									plan?.fixedKeyType,
								);

								if (!targetDetailUrl) {
									const emptyDetail: MaterialBreakdownDetail = { costs: [] };
									return [material.id, emptyDetail] as const;
								}

								const response = await api.get<
									MaterialDetail | LongwallMaterialDetail
								>(targetDetailUrl);
								return [
									material.id,
									normalizeMaterialDetail(response.result),
								] as const;
							},
						),
					);

					const targetDetailsByMaterialId = new Map(targetDetailResponses);
					targetMaterials.forEach((item) => {
						const targetRows =
							targetDetailsByMaterialId
								.get(item.targetMaterial.id)
								?.costs.filter(
									(cost) => cost.assignmentCodeId === item.assignmentCodeId,
								) ?? [];

						if (targetRows.length > 0) {
							targetCostsByAssignmentId.set(item.assignmentCodeId, targetRows);
						}
					});
				}

				let slideUsage = '-';
				let slideUnitPriceCost = result.slideUnitPriceCost || 0;
				const stoneClampRatioReferenceId =
					result.stoneClampRatioReferenceId ||
					(result as unknown as { stoneClampRatioId?: string })
						.stoneClampRatioId;
				const stoneClampRatio =
					allClamps.find((clamp) => clamp.id === stoneClampRatioReferenceId)
						?.value || '-';

				if (plan?.fixedKeyType === ProcessGroupType.DL) {
					if (!result.slideUnitPriceAssignmentCodeId) {
						slideUsage = 'Không sử dụng máng trượt';
					} else if (selectedSlideCost) {
						slideUsage = selectedSlideCost.materialName || '-';
						if (!slideUnitPriceCost) {
							slideUnitPriceCost = selectedSlideCost.amount;
						}
					}
				}

				if (!active) return;

				setPlannedMaterialPrice(result.totalPlannedMaterialPrice || 0);
				setTotal(
					result.totalPlannedMaterialPrice * (output?.productionMeters || 1),
				);
				setSummary([
					{
						materialCode: selectedMaterial?.code || '-',
						materialDetail: getPlannedMaterialDetail(
							selectedMaterial,
							plan?.fixedKeyType,
						),
						materialUnitPriceCost: result.materialCost || 0,
						slideUsage,
						slideUnitPriceCost,
						lowValuePerishableSupplyUsage:
							result.lowValuePerishableSupplyInclusion ===
							LowValuePerishableSupplyInclusion.Include
								? 'Gồm đơn giá vật tư mau hỏng rẻ tiền'
								: 'Không gồm đơn giá vật tư mau hỏng rẻ tiền',
						lowValuePerishableSupplyUnitPriceCost:
							result.lowValuePerishableSupplyUnitPriceCost || 0,
						stoneClampRatio,
						normFactorValue: result.normFactorValue || '-',
					},
				]);
				setBreakdownRows(
					buildPlannedMaterialBreakdownRows({
						currentDetail: normalizedMaterialDetail,
						slideCost: selectedSlideCost,
						normFactorAssignments: normFactor?.assignmentCodes,
						targetCostsByAssignmentId,
						materialCostTotal: result.materialCost,
					}),
				);
			} finally {
				if (active) {
					setLoading(false);
				}
			}
		};

		fetchData();

		return () => {
			active = false;
		};
	}, [id, output, plan, reloadKey]);

	const displayedSummary = id ? summary : [];
	const displayedBreakdownRows = id ? breakdownRows : [];
	const displayedPlannedMaterialPrice = id ? plannedMaterialPrice : 0;
	const displayedTotal = id ? total : 0;
	const isLoading = !!id && loading;

	return (
		<AccordionItem value={'planed-material-cost'} className='border-none'>
			<Item
				variant={'outline'}
				size='sm'
				className='w-full flex-1 rounded-sm border-[#b8b8b8] bg-[#f3f4f6] py-2.5 shadow-none'
			>
				<ItemContent>
					<ItemTitle>Doanh thu vật liệu kế hoạch ban đầu</ItemTitle>
				</ItemContent>
				<ItemContent className='me-2 w-24'>
					<ItemTitle>
						{isLoading ? (
							<Spinner />
						) : (
							formatNumber(displayedPlannedMaterialPrice)
						)}
					</ItemTitle>
				</ItemContent>
				<ItemContent className='me-7.5 w-24'>
					<ItemTitle>
						{isLoading ? <Spinner /> : formatNumber(displayedTotal)}
					</ItemTitle>
				</ItemContent>
				<ItemActions className='gap-1'>
					<DialogProvider>
						<DataTableEditDialog
							type='Tạo mới'
							crumb='Doanh thu vật liệu kế hoạch ban đầu'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
									disabled={!!id}
								>
									<AddIcon />
								</Button>
							}
						>
							<PlanMaterialCostForm
								plan={plan}
								output={output}
								callback={callback}
							/>
						</DataTableEditDialog>
					</DialogProvider>
					<AccordionTrigger
						disabled={!id}
						className='group p-0 disabled:opacity-50'
					>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
					</AccordionTrigger>
					<DialogProvider>
						<DataTableEditDialog
							type='Chỉnh sửa'
							crumb='Doanh thu vật liệu kế hoạch ban đầu'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
									disabled={!id}
								>
									<CreateIcon />
								</Button>
							}
						>
							<PlanMaterialCostForm
								id={id}
								plan={plan}
								output={output}
								callback={callback}
							/>
						</DataTableEditDialog>
					</DialogProvider>
				</ItemActions>
			</Item>

			<AccordionContent className='p-0 px-1.5 pt-1.5'>
				{id && isOpen && (
					<div className='space-y-2'>
						<DataTable
							columns={getPlanedMaterialCostSummaryColumns(plan?.fixedKeyType)}
							items={displayedSummary}
							compact={true}
							hasActions={false}
							hasPagination={false}
							hasSort={false}
							hasIndex={false}
						/>

						{displayedBreakdownRows.length > 0 && (
							<>
								<div className='mx-1 my-4 border-t border-[#d9d9d9]' />
								<DataTable
									columns={PLANNED_MATERIAL_BREAKDOWN_COLUMNS}
									items={displayedBreakdownRows}
									compact={true}
									hasActions={false}
									hasPagination={false}
									hasSort={false}
									hasIndex={false}
								/>
							</>
						)}
					</div>
				)}
			</AccordionContent>
		</AccordionItem>
	);
}
