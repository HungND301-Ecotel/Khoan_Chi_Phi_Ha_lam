/* eslint-disable react-hooks/incompatible-library */
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Asset } from '@/features/main/catalog/asset/types';
import { Clamp } from '@/features/main/catalog/parameter/clamp/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { NormFactor } from '@/features/main/catalog/norm-factor/columns';
import { PlanedMaterialCostType } from '@/features/main/cost/plan/planed-material-cost/columns';
import {
	PLAN_MATERIAL_COST_DEFAULT,
	planMaterialCostSchema,
	PlanMaterialCostSchema,
} from '@/features/main/cost/plan/planed-material-cost/schema';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { Check, Pencil, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { UnifiedMaterial } from './type';
import { ProcessGroupType } from '@/constants/process-group';

type SlideDetailListItem = {
	id: string;
	processGroupId?: string;
	passportId?: string;
	hardnessId?: string;
	materialId?: string;
	startMonth?: string;
	endMonth?: string;
	startDate?: string;
	endDate?: string;
};

function findMatchedNormFactor(
	normFactors: NormFactor[],
	params: {
		stoneClampRatioId: string;
		materialProcessId: string;
		materialHardnessId: string;
	},
) {
	const { stoneClampRatioId, materialProcessId, materialHardnessId } = params;

	// Strict rule:
	// Chỉ map khi đồng thời đúng productionProcessId + hardnessId + stoneClampRatioId
	return (
		normFactors.find(
			(normFactor) =>
				normFactor.productionProcessId === materialProcessId &&
				normFactor.hardnessId === materialHardnessId &&
				normFactor.stoneClampRatioId === stoneClampRatioId,
		) || null
	);
}

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

export function PlanMaterialCostForm({
	id,
	plan,
	output,
	callback,
}: ProductCostFormProps) {
	const [allNormFactors, setAllNormFactors] = useState<NormFactor[]>([]);
	const [stoneClampRatios, setStoneClampRatios] = useState<Clamp[]>([]);
	const [strengths, setStrengths] = useState<Strength[]>([]);
	const [materials, setMaterials] = useState<UnifiedMaterial[]>([]);
	const [assets, setAssets] = useState<Asset[]>([]);
	const [slideDetailList, setSlideDetailList] = useState<SlideDetailListItem[]>(
		[],
	);
	const [slideCode, setSlideCode] = useState<string>('MT');
	const [slideCodeDraft, setSlideCodeDraft] = useState<string>('MT');
	const [isEditingSlideCode, setIsEditingSlideCode] = useState<boolean>(false);

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
			materialReferenceId: '',
		},
	});

	const watchedStoneClampRatioReferenceId = form.watch(
		'stoneClampRatioReferenceId',
	);
	const watchedNormFactorId = form.watch('normFactorId');
	const watchedMaterialUnitPriceId = form.watch('materialUnitPriceId');
	const watchedMaterialReferenceId = form.watch('materialReferenceId');

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
			api.pagging<NormFactor>(API.CATALOG.NORM_FACTOR.LIST),
			api.pagging<Clamp>(API.CATALOG.PARAMETER.CLAMP.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
			api.pagging<UnifiedMaterial>(API.PRICING.MATERIAL.ALL),
			api.pagging<Asset>(API.CATALOG.ASSET.LIST, { ignorePagination: true }),
			api.pagging<SlideDetailListItem>(API.PRICING.SLIDE.DETAIL_LIST, {
				ignorePagination: true,
			}),
		]);

		promises.then(
			([
				normFactors,
				stoneClampRatios,
				strengths,
				materials,
				assets,
				slideDetailList,
			]) => {
				const normFactorsData = normFactors.result.data;
				setAllNormFactors(normFactorsData);
				setStoneClampRatios(stoneClampRatios.result.data);
				const strengthsData = strengths.result.data;
				setStrengths(strengthsData);
				setMaterials(materials.result.data);
				const assetData = assets.result.data;
				setAssets(assetData);
				const slideDetailData = slideDetailList.result.data;
				setSlideDetailList(slideDetailData);
				if (!id) return;
				api
					.get<PlanedMaterialCostType>(API.COST.PLANNED_MATERIAL.DETAIL(id))
					.then((detail) => {
						const {
							normFactorId,
							materialUnitPriceId,
							slideUnitPriceAssignmentCodeId,
						} = detail.result;
						const materialReferenceIdFromDetail =
							(
								detail.result as unknown as {
									materialReferenceId?: string;
									materialId?: string;
								}
							).materialReferenceId ||
							(detail.result as unknown as { materialId?: string })
								.materialId ||
							slideDetailData.find(
								(item) => item.id === slideUnitPriceAssignmentCodeId,
							)?.materialId ||
							null;
						const stoneClampRatioReferenceId =
							(
								detail.result as unknown as {
									stoneClampRatioReferenceId?: string;
									stoneClampRatioId?: string;
								}
							).stoneClampRatioReferenceId ||
							(detail.result as unknown as { stoneClampRatioId?: string })
								.stoneClampRatioId ||
							normFactorsData.find((item) => item.id === normFactorId)
								?.stoneClampRatioId ||
							null;

						form.reset({
							normFactorId: normFactorId || null,
							stoneClampRatioReferenceId,
							materialUnitPriceId,
							slideUnitPriceAssignmentCodeId:
								slideUnitPriceAssignmentCodeId || '',
							materialReferenceId: materialReferenceIdFromDetail || '',
							outputId: output?.id,
							productUnitPriceId: plan?.id,
						});

						const matchedAsset = assetData.find(
							(asset) => asset.id === materialReferenceIdFromDetail,
						);
						const assignmentCode = matchedAsset?.assignmentCode || '';
						setSlideCode(assignmentCode);
						setSlideCodeDraft(assignmentCode);
					});
			},
		);
	}, [id, form, output, plan]);

	useEffect(() => {
		if (isEditingSlideCode) return;
		setSlideCodeDraft(slideCode);
	}, [isEditingSlideCode, slideCode]);

	useEffect(() => {
		const selectedMaterial = materials.find(
			(material) => material.id === watchedMaterialUnitPriceId,
		);
		const stoneClampRatioReferenceId = watchedStoneClampRatioReferenceId;

		if (!selectedMaterial || !stoneClampRatioReferenceId) {
			form.setValue('normFactorId', null);
			return;
		}

		const matchedNormFactor = findMatchedNormFactor(allNormFactors, {
			stoneClampRatioId: stoneClampRatioReferenceId,
			materialProcessId: selectedMaterial.processId,
			materialHardnessId: selectedMaterial.hardnessId,
		});

		form.setValue('normFactorId', matchedNormFactor?.id || null);
	}, [
		allNormFactors,
		form,
		materials,
		watchedMaterialUnitPriceId,
		watchedStoneClampRatioReferenceId,
	]);

	const filteredSlideAssets = assets.filter((asset) => {
		const normalizedSlideCode = slideCode.trim().toLowerCase();

		// Khi mã máng trượt rỗng: chỉ lấy asset có isSlideAssignmentCode = true
		if (!normalizedSlideCode) {
			return asset.isSlideAssignmentCode;
		}

		// Khi có mã máng trượt: lấy toàn bộ asset và lọc theo assignmentCode
		return asset.assignmentCode.trim().toLowerCase() === normalizedSlideCode;
	});

	useEffect(() => {
		if (!watchedMaterialReferenceId) return;
		if (
			filteredSlideAssets.some(
				(asset) => asset.id === watchedMaterialReferenceId,
			)
		)
			return;
		form.setValue('materialReferenceId', '');
	}, [filteredSlideAssets, form, watchedMaterialReferenceId]);

	const resolveSlideAssignmentCodeId = (params: {
		materialReferenceId: string;
		material: UnifiedMaterial;
	}) => {
		const { materialReferenceId, material } = params;

		const matched = slideDetailList.find((item) => {
			if (item.materialId !== materialReferenceId) return false;
			if (item.processGroupId !== plan?.processGroupId) return false;
			if (item.passportId !== material.passportId) return false;
			if (item.hardnessId !== material.hardnessId) return false;

			return isTimeCovered(
				material.startMonth,
				material.endMonth,
				item.startMonth || item.startDate,
				item.endMonth || item.endDate,
			);
		});

		return matched?.id || null;
	};

	const handleMaterialCostSubmit = async (values: PlanMaterialCostSchema) => {
		const selectedMaterial = materials.find(
			(material) => material.id === values.materialUnitPriceId,
		);
		let resolvedSlideUnitPriceAssignmentCodeId: string | null = null;

		if (values.materialReferenceId && selectedMaterial) {
			resolvedSlideUnitPriceAssignmentCodeId = resolveSlideAssignmentCodeId({
				materialReferenceId: values.materialReferenceId,
				material: selectedMaterial,
			});
		}

		if (values.materialReferenceId && !resolvedSlideUnitPriceAssignmentCodeId) {
			// Không tìm thấy định mức máng trượt phù hợp điều kiện:
			// vẫn submit, giữ materialReferenceId theo combobox và để slideUnitPriceAssignmentCodeId = null.
			resolvedSlideUnitPriceAssignmentCodeId = null;
		}

		const submitValues: PlanMaterialCostSchema = {
			...values,
			slideUnitPriceAssignmentCodeId: resolvedSlideUnitPriceAssignmentCodeId,
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
			const hardnessOrPowerText =
				material.powerName?.trim() || material.hardnessName?.trim();

			const fields = [
				material.technologyName,
				hardnessOrPowerText,
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

	const selectedNormFactor = allNormFactors.find(
		(normFactor) => normFactor.id === watchedNormFactorId,
	);
	const selectedMaterial = materials.find(
		(material) => material.id === watchedMaterialUnitPriceId,
	);
	const referenceHardnessValue = (() => {
		if (!selectedMaterial) return '';

		const targetHardnessId = selectedNormFactor?.targetHardnessId;
		const isCurrentHardness =
			!targetHardnessId ||
			targetHardnessId === '00000000-0000-0000-0000-000000000000';

		if (isCurrentHardness) {
			return selectedMaterial.hardnessName || '';
		}

		return (
			strengths.find((strength) => strength.id === targetHardnessId)?.value ||
			selectedMaterial.hardnessName ||
			''
		);
	})();

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
				<FormRow>
					<div className='flex-2'>
						<FormComboBox
							control={form.control}
							name='materialReferenceId'
							label='Sử dụng máng trượt'
							placeholder='Chọn máng trượt'
							options={[
								{ label: 'Không sử dụng máng trượt', value: '' },
								...filteredSlideAssets.map((asset) => ({
									label: `${asset.code} - ${asset.name}`,
									value: asset.id,
								})),
							]}
						/>
					</div>
					<div className='flex-1 space-y-2'>
						<Label>Mã máng trượt</Label>
						<div className='flex gap-2'>
							<Input
								readOnly={!isEditingSlideCode}
								value={isEditingSlideCode ? slideCodeDraft : slideCode}
								onChange={(e) => setSlideCodeDraft(e.target.value)}
							/>
							{isEditingSlideCode ? (
								<>
									<Button
										type='button'
										variant='outline'
										size='icon'
										onClick={() => {
											setSlideCode(slideCodeDraft.trim());
											setIsEditingSlideCode(false);
										}}
									>
										<Check className='size-4' />
									</Button>
									<Button
										type='button'
										variant='outline'
										size='icon'
										onClick={() => {
											setSlideCodeDraft(slideCode);
											setIsEditingSlideCode(false);
										}}
									>
										<X className='size-4' />
									</Button>
								</>
							) : (
								<Button
									type='button'
									variant='outline'
									size='icon'
									onClick={() => setIsEditingSlideCode(true)}
								>
									<Pencil className='size-4' />
								</Button>
							)}
						</div>
					</div>
				</FormRow>
			)}

			<FormRow>
				<FormComboBox
					control={form.control}
					name='stoneClampRatioReferenceId'
					label='Tỷ lệ đá kẹp (Ckep)'
					placeholder='Chọn tỷ lệ đá kẹp (Ckep)'
					options={stoneClampRatios.map((clamp) => ({
						label: clamp.value,
						value: clamp.id,
					}))}
				/>

				<div className='flex-1 space-y-2'>
					<Label>Hệ số điều chỉnh định mức</Label>
					<Input readOnly value={selectedNormFactor?.value ?? ''} />
				</div>

				<div className='flex-1 space-y-2'>
					<Label>Định mức tham chiếu</Label>
					<Input readOnly value={referenceHardnessValue} />
				</div>
			</FormRow>
			<DataTableEditConfirm />
		</FormProvider>
	);
}
