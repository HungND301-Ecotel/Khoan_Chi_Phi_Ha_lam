/* eslint-disable react-hooks/incompatible-library */
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Clamp } from '@/features/main/catalog/parameter/clamp/columns';
import { PlanedMaterialCostType } from '@/features/main/cost/plan/planed-material-cost/columns';
import {
	PLAN_MATERIAL_COST_DEFAULT,
	planMaterialCostSchema,
	PlanMaterialCostSchema,
} from '@/features/main/cost/plan/planed-material-cost/schema';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import {
	Slide,
	SlideDetail,
	SlideDetailMaterialCost,
} from '@/features/main/pricing/tunneling/slide/columns';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { UnifiedMaterial } from './type';
import { ProcessGroupType } from '@/constants/process-group';

export function PlanMaterialCostForm({
	id,
	plan,
	output,
	callback,
}: ProductCostFormProps) {
	const [isInit, setIsInit] = useState<boolean>(!!id);
	const [clamps, setClamps] = useState<Clamp[]>([]);
	const [allClamps, setAllClamps] = useState<Clamp[]>([]);
	const [materials, setMaterials] = useState<UnifiedMaterial[]>([]);
	const [slides, setSlides] = useState<Slide[]>([]);
	const [slideDetailMaterialCosts, setSlideDetailMaterialCosts] = useState<
		SlideDetailMaterialCost[]
	>([]);

	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();

	const form = useForm<PlanMaterialCostSchema>({
		resolver: zodResolver(planMaterialCostSchema),
		mode: 'onSubmit',
		defaultValues: {
			...PLAN_MATERIAL_COST_DEFAULT,
			outputId: output?.id,
			productUnitPriceId: plan?.id,
			slideUnitPriceAssignmentCodeId: '',
		},
	});

	const watchedStoneClampRatioId = form.watch('stoneClampRatioId');
	const watchedMaterialUnitPriceId = form.watch('materialUnitPriceId');

	// Filter materials based on processGroupType
	const filteredMaterials = materials.filter((material) => {
		const groupType = plan?.processGroupType;
		if (groupType === ProcessGroupType.DL) {
			return material.type === 1;
		}
		if (groupType === ProcessGroupType.LC) {
			return material.type === 2;
		}
		return true;
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Clamp>(API.CATALOG.PARAMETER.CLAMP.LIST),
			api.pagging<UnifiedMaterial>(API.PRICING.MATERIAL.ALL),
			api.pagging<Slide>(API.PRICING.SLIDE.LIST),
		]);

		promises.then(([clamps, materials, slides]) => {
			setAllClamps(clamps.result.data);
			setClamps(clamps.result.data);
			setMaterials(materials.result.data);
			setSlides(slides.result.data);

			if (!id) return;
			api
				.get<PlanedMaterialCostType>(API.COST.PLANNED_MATERIAL.DETAIL(id))
				.then((detail) => {
					const {
						stoneClampRatioId,
						materialUnitPriceId,
						slideUnitPriceAssignmentCodeId,
					} = detail.result;

					form.reset({
						stoneClampRatioId,
						materialUnitPriceId,
						slideUnitPriceAssignmentCodeId:
							slideUnitPriceAssignmentCodeId || '',
						outputId: output?.id,
						productUnitPriceId: plan?.id,
					});
				})
				.finally(() => setIsInit(false));
		});
	}, [id, form, output, plan]);

	useEffect(() => {
		// Only clear slide selection when creating a new planned-material-cost (no `id`)
		if (!isInit && !id) form.setValue('slideUnitPriceAssignmentCodeId', '');

		if (!watchedMaterialUnitPriceId || !plan)
			return setSlideDetailMaterialCosts([]);

		const selectedMaterial = materials.find(
			(material) => material.id === watchedMaterialUnitPriceId,
		);

		if (!selectedMaterial) {
			// setSlideDetailMaterialCosts([]);
			// restore clamps to full list when no material selected
			setClamps(allClamps);
			return;
		}

		const { processGroupId } = plan;
		const { startMonth, endMonth, passportId, hardnessId } = selectedMaterial;

		const matchedSlide = slides.find((slide) => {
			const targetStart = new Date(startMonth.slice(0, 7));
			const targetEnd = new Date(endMonth.slice(0, 7));
			const slideStart = new Date(slide.startMonth.slice(0, 7));
			const slideEnd = new Date(slide.endMonth.slice(0, 7));

			if (processGroupId !== slide.processGroupId) return false;
			if (passportId !== slide.passportId) return false;
			if (hardnessId !== slide.hardnessId) return false;

			const isTimeMatch = slideStart <= targetStart && slideEnd >= targetEnd;

			return isTimeMatch;
		});

		if (!matchedSlide) return setSlideDetailMaterialCosts([]);

		// filter clamps to only those matching material.hardnessId
		const filteredClamps = allClamps.filter(
			(clamp) => clamp.hardnessId === selectedMaterial.hardnessId,
		);
		setClamps(filteredClamps);

		api
			.get<SlideDetail>(API.PRICING.SLIDE.DETAIL(matchedSlide.id))
			.then((res) => {
				const slideDetailMaterialCosts: SlideDetailMaterialCost[] = [];
				res.result.materialCost.forEach((materialCost) => {
					materialCost.costs.forEach((cost) =>
						slideDetailMaterialCosts.push(cost),
					);
				});
				setSlideDetailMaterialCosts(slideDetailMaterialCosts);
			});
	}, [watchedMaterialUnitPriceId, materials, plan, slides, form, isInit]);

	const handleMaterialCostSubmit = async (values: PlanMaterialCostSchema) => {
		const submitValues = {
			...values,
			slideUnitPriceAssignmentCodeId:
				values.slideUnitPriceAssignmentCodeId === ''
					? null
					: values.slideUnitPriceAssignmentCodeId,
		};
		return handleSubmit(submitValues);
	};

	const getMaterialLabel = (material: UnifiedMaterial, groupType?: number) => {
		const { code } = material;
		let detailText = '';

		if (groupType === ProcessGroupType.DL) {
			// Trường hợp Đào lò (DL)
			const fields = [
				material.hardnessName,
				material.passportName,
				material.insertItemName,
				material.supportStepName,
			];
			detailText = fields.filter(Boolean).join(' | ');
		} else if (groupType === ProcessGroupType.LC) {
			// Trường hợp Luồng cày/Khai thác (LC)

			const fields = [
				material.technologyName,
				material.seamFaceName,
				material.longwallParametersName,
				material.cuttingThicknessName,
			];
			detailText = fields.filter(Boolean).join(' | ');
		}

		return detailText ? `${code} - ${detailText}` : code;
	};

	const handleSubmit = async (values: PlanMaterialCostSchema) => {
		try {
			const submitData = {
				...values,
				slideUnitPriceAssignmentCodeId:
					values.slideUnitPriceAssignmentCodeId === ''
						? null
						: values.slideUnitPriceAssignmentCodeId,
			};

			if (id) {
				await api.put(API.COST.PLANNED_MATERIAL.UPDATE, { id, ...submitData });
			} else {
				await api.post(API.COST.PLANNED_MATERIAL.CREATE, { ...submitData });
			}

			setOpen(false);
			success(
				`${breadcrumb} đã được ${id ? 'cập nhật' : 'tạo mới'} thành công.`,
			);
			await callback?.();
		} catch (err) {
			error(err);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleMaterialCostSubmit}>
			<FormRow>
				<div className='flex-1 space-y-2'>
					<Label>Ngày bắt đầu</Label>
					<Input readOnly value={formatDate(output?.startMonth || '')} />
				</div>

				<div className='flex-1 space-y-2'>
					<Label>Ngày kết thúc</Label>
					<Input readOnly value={formatDate(output?.endMonth || '')} />
				</div>
			</FormRow>

			<FormRow>
				<div className='flex-1 space-y-2'>
					<Label>Mã sản phẩm</Label>
					<Input readOnly value={plan?.productCode} />
				</div>

				<div className='flex-1 space-y-2'>
					<Label>Tên sản phẩm</Label>
					<Input readOnly value={plan?.productCode} />
				</div>
			</FormRow>

			<FormRow>
				<div className='flex-1 space-y-2'>
					<Label>Mã nhóm CĐSX</Label>
					<Input readOnly value={plan?.processGroupCode} />
				</div>

				<div className='flex-1 space-y-2'>
					<Label>Tên nhóm CĐSX</Label>
					<Input readOnly value={plan?.processGroupName} />
				</div>

				<div className='flex-1 space-y-2'>
					<Label>Sản lượng</Label>
					<Input readOnly value={output?.productionMeters} />
				</div>

				<div className='flex-1 space-y-2'>
					<Label>Đơn vị tính</Label>
					<Input readOnly value={plan?.unitOfMeasureName} />
				</div>
			</FormRow>

			<FormComboBox
				control={form.control}
				name='materialUnitPriceId'
				label='Mã định mức đơn giá vật liệu'
				placeholder='Chọn mã định mức đơn giá vật liệu'
				options={filteredMaterials.map((material) => ({
					label: getMaterialLabel(material, plan?.processGroupType),
					value: material.id,
				}))}
			/>
			{plan?.processGroupType === ProcessGroupType.DL && (
				<FormComboBox
					control={form.control}
					name='slideUnitPriceAssignmentCodeId'
					label='Sử dụng máng trượt'
					placeholder='Chọn máng trượt'
					options={[
						{ label: 'Không sử dụng máng trượt', value: '' },
						...slideDetailMaterialCosts.map((opt) => ({
							label: opt.materialName,
							value: opt.id,
						})),
					]}
				/>
			)}

			{plan?.processGroupType === ProcessGroupType.DL && (
				<FormRow>
					<FormComboBox
						control={form.control}
						name='stoneClampRatioId'
						label='Tỷ lệ đá kẹp (Ckep)'
						placeholder='Chọn tỷ lệ đá kẹp (Ckep)'
						options={clamps.map((clamp) => ({
							label: clamp.value,
							value: clamp.id,
						}))}
					/>

					<div className='flex-1 space-y-2'>
						<Label>Hệ số điều chỉnh định mức</Label>
						<Input
							readOnly
							value={
								clamps.find((clamp) => clamp.id === watchedStoneClampRatioId)
									?.coefficientValue ?? ''
							}
						/>
					</div>
				</FormRow>
			)}

			<DataTableEditConfirm />
		</FormProvider>
	);
}
