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
import { ProcessGroupType } from '@/constants/process-group';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import { Clamp } from '@/features/main/catalog/parameter/clamp/columns';
import {
	PLANED_MATERIAL_COST_SUMMARY_COLUMNS,
	PlanedMaterialCostSummary,
	PlanedMaterialCostType,
} from '@/features/main/cost/plan/planed-material-cost/columns';
import { PlanMaterialCostForm } from '@/features/main/cost/plan/planed-material-cost/form';
import { UnifiedMaterial } from '@/features/main/cost/plan/planed-material-cost/type';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
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

export function PlanedMaterialCost({
	id,
	output,
	plan,
	callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [summary, setSummary] = useState<PlanedMaterialCostSummary[]>([]);
	const [total, setTotal] = useState<number>(0);
	const [loading, setLoading] = useState<boolean>(!!id);

	useEffect(() => {
		if (!id) {
			setSummary([]);
			setTotal(0);
			setLoading(false);
			return;
		}

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
				setTotal(
					result.totalPlannedMaterialPrice * (output?.productionMeters || 1),
				);

				const allClamps = clampsRes.result.data;
				const allMaterials = materialsRes.result.data;
				const allSlides = slidesRes.result.data;

				const filteredMaterials = allMaterials.filter((material) => {
					const groupType = plan?.processGroupType;
					if (groupType === ProcessGroupType.DL) {
						return material.type === 1;
					}
					if (groupType === ProcessGroupType.LC) {
						return material.type === 2;
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

				let slideUsage = '-';
				let slideUnitPriceCost = result.slideUnitPriceCost || 0;
				let stoneClampRatio = '-';

				if (plan?.processGroupType === ProcessGroupType.DL) {
					if (!result.slideUnitPriceAssignmentCodeId) {
						slideUsage = 'Không sử dụng máng trượt';
					} else if (selectedMaterial && plan) {
						const { processGroupId } = plan;
						const { startMonth, endMonth, passportId, hardnessId } =
							selectedMaterial;

						const matchedSlide = allSlides.find((slide) => {
							const targetStart = new Date(startMonth.slice(0, 7));
							const targetEnd = new Date(endMonth.slice(0, 7));
							const slideStart = new Date(slide.startMonth.slice(0, 7));
							const slideEnd = new Date(slide.endMonth.slice(0, 7));

							if (processGroupId !== slide.processGroupId) return false;
							if (passportId !== slide.passportId) return false;
							if (hardnessId !== slide.hardnessId) return false;

							const isTimeMatch =
								slideStart <= targetStart && slideEnd >= targetEnd;

							return isTimeMatch;
						});

						if (matchedSlide) {
							const slideDetail = await api.get<SlideDetail>(
								API.PRICING.SLIDE.DETAIL(matchedSlide.id),
							);

							const slideDetailMaterialCosts: SlideDetailMaterialCost[] = [];
							slideDetail.result.materialCost.forEach((materialCost) => {
								materialCost.costs.forEach((cost) =>
									slideDetailMaterialCosts.push(cost),
								);
							});

							slideUsage =
								slideDetailMaterialCosts.find(
									(item) => item.id === result.slideUnitPriceAssignmentCodeId,
								)?.materialName || '-';
							if (!slideUnitPriceCost) {
								slideUnitPriceCost =
									slideDetailMaterialCosts.find(
										(item) => item.id === result.slideUnitPriceAssignmentCodeId,
									)?.cost || 0;
							}
						}
					}
					const stoneClampRatioReferenceId =
						result.stoneClampRatioReferenceId ||
						(result as unknown as { stoneClampRatioId?: string })
							.stoneClampRatioId;
					stoneClampRatio =
						allClamps.find((clamp) => clamp.id === stoneClampRatioReferenceId)
							?.value || '-';
				}

				setSummary([
					{
						materialCode: selectedMaterial?.code || '-',
						materialUnitPriceCost: result.materialCost || 0,
						slideUsage,
						slideUnitPriceCost,
						stoneClampRatio,
						normFactorValue: result.normFactorValue || '-',
					},
				]);
			} finally {
				setLoading(false);
			}
		};

		fetchData();
	}, [id, plan, reloadKey, output?.productionMeters]);

	return (
		<AccordionItem value={'planed-material-cost'} className='border-none'>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Chi phí vật liệu kế hoạch ban đầu</ItemTitle>
				</ItemContent>
				<ItemContent className='me-7.5 w-24'>
					<ItemTitle>
						{loading ? <Spinner /> : formatNumber(Math.round(total))}
					</ItemTitle>
				</ItemContent>
				<ItemActions>
					<DialogProvider>
						<DataTableEditDialog
							type='Tạo mới'
							crumb='Chi phí vật liệu kế hoạch ban đầu'
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
							crumb='Chi phí vật liệu kế hoạch ban đầu'
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

			<AccordionContent className='p-0 px-2 pt-2'>
				{id && isOpen && (
					<div className='space-y-2'>
						<DataTable
							columns={PLANED_MATERIAL_COST_SUMMARY_COLUMNS}
							items={summary}
							compact={true}
							hasActions={false}
							hasPagination={false}
							hasSort={false}
							hasIndex={false}
						/>
					</div>
				)}
			</AccordionContent>
		</AccordionItem>
	);
}
