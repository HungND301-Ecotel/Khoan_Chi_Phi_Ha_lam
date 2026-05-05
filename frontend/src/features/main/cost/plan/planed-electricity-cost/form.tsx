/* eslint-disable react-hooks/incompatible-library */
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { ProcessGroupType } from '@/constants/process-group';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import {
	PLAN_ELECTRICITY_ADJUSTMENT_DEFAULT,
	PLAN_ELECTRICITY_COST_DEFAULT,
	planElectricityCostSchema,
	PlanElectricityCostSchema,
} from '@/features/main/cost/plan/planed-electricity-cost/schema';
import {
	PlanedElectricityCostAdjustmentSelection,
	PlanedElectricityCostDetail,
} from '@/features/main/cost/plan/planed-electricity-cost/types';
import { AdjustmentDetail } from '@/features/main/cost/plan/planed-maintain-cost/types';
import { CostPlanAdjustmentFactorInput } from '@/features/main/cost/plan/components/cost-plan-adjustment-factor-input';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import { Electricity } from '@/features/main/pricing/tunneling/electricity/columns';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

function formatAdjustmentOptionLabel(
	description: string,
	value?: number | null,
) {
	if (value === null || value === undefined) {
		return description;
	}

	const valueText = formatNumber(value);

	return `${description} - ${valueText}`;
}

export function PlanElectricityCostForm({
	id,
	plan,
	output,
	callback,
}: ProductCostFormProps) {
	const [isInit, setIsInit] = useState<boolean>(!!id);
	const [electricities, setElectricities] = useState<Electricity[]>([]);
	const [adjustments, setAdjustments] = useState<AdjustmentDetail[]>([]);

	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();

	const form = useForm<PlanElectricityCostSchema>({
		resolver: zodResolver(planElectricityCostSchema),
		mode: 'onSubmit',
		defaultValues: {
			...PLAN_ELECTRICITY_COST_DEFAULT,
			productUnitPriceId: plan?.id,
			outputId: output?.id,
		},
	});

	const watchedElectricityUnitPriceIds = form.watch('electricityUnitPriceIds');
	const watchedCosts = form.watch('costs');

	useEffect(() => {
		const electricityEndpoint =
			plan?.fixedKeyType === ProcessGroupType.LC
				? API.PRICING.ELECTRICITY.LONGWALL_PANEL.LIST
				: plan?.fixedKeyType === ProcessGroupType.XL
					? API.PRICING.ELECTRICITY.TRIMMING.LIST
					: API.PRICING.ELECTRICITY.TUNNELING.LIST;

		const promises = Promise.all([
			api.pagging<Electricity>(electricityEndpoint),
			api.get<AdjustmentDetail[]>(API.CATALOG.ADJUSTMENT.FACTOR.DETAILS, {
				processGroupId: plan?.processGroupId || '',
			}),
		]);

		promises.then(([electricityResponse, adjustments]) => {
			setElectricities(electricityResponse.result.data);

			let filteredAdjustments: AdjustmentDetail[] = [];
			if (
				plan?.fixedKeyType === ProcessGroupType.DL ||
				plan?.fixedKeyType === ProcessGroupType.XL
			) {
				filteredAdjustments = adjustments.result
					.sort((a, b) =>
						(a.fixedKeyKey ?? a.code).localeCompare(b.fixedKeyKey ?? b.code),
					)
					.slice(0, 3);
			} else if (plan?.fixedKeyType === ProcessGroupType.LC) {
				filteredAdjustments = adjustments.result
					.sort((a, b) =>
						(a.fixedKeyKey ?? a.code).localeCompare(b.fixedKeyKey ?? b.code),
					)
					.slice(0, 1);
			}

			setAdjustments(filteredAdjustments);

			if (!id) return;

			api
				.get<PlanedElectricityCostDetail>(
					API.COST.PLANNED_ELECTRICITY.DETAIL(id),
				)
				.then((res) => {
					form.reset({
						productUnitPriceId: plan?.id,
						outputId: output?.id,
						trimmingCoefficient: (res.result.trimmingCoefficient || 1) * 100,
						electricityUnitPriceIds: res.result.costs.map((detail) => {
							return detail.electricityUnitPriceEquipmentId;
						}),
						costs: res.result.costs.map(
							({
								electricityUnitPriceEquipmentId,
								quantity,
								adjustmentFactorDescriptions,
							}) => {
								const sortedDescriptions = adjustmentFactorDescriptions.sort(
									(a, b) =>
										a.adjustmentFactorCode.localeCompare(
											b.adjustmentFactorCode,
										),
								);

								return {
									electricityUnitPriceEquipmentId,
									quantity,
									adjustmentFactorDescriptions: filteredAdjustments.map(
										(adjustment) => {
											const description = sortedDescriptions.find(
												(desc) => desc.adjustmentFactorId === adjustment.id,
											);

											return {
												adjustmentFactorDescriptionId:
													description?.adjustmentFactorDescriptionId ?? '',
												adjustmentFactorId:
													description?.customValue !== null &&
													description?.customValue !== undefined
														? description.adjustmentFactorId
														: '',
												customValue: description?.customValue ?? null,
											};
										},
									),
								};
							},
						),
					});
					setIsInit(false);
				});
		});
	}, [id, form, plan, output]);

	useEffect(() => {
		if (isInit) return;

		const currentCosts = form.getValues('costs') || [];

		const updatedCosts = watchedElectricityUnitPriceIds.map((selectedId) => {
			const existingEntry = currentCosts.find(
				(c) => c.electricityUnitPriceEquipmentId === selectedId,
			);

			if (existingEntry) return existingEntry;

			return {
				electricityUnitPriceEquipmentId: selectedId,
				quantity: NaN,
				adjustmentFactorDescriptions: Array.from(
					{ length: adjustments.length || 3 },
					() => ({ ...PLAN_ELECTRICITY_ADJUSTMENT_DEFAULT }),
				),
			};
		});

		if (updatedCosts.length !== currentCosts.length) {
			form.setValue('costs', updatedCosts);
		}
	}, [
		watchedElectricityUnitPriceIds,
		form,
		electricities,
		isInit,
		adjustments,
	]);

	// Sync electricityUnitPriceIds when costs are removed
	useEffect(() => {
		if (isInit) return;

		const currentCosts = form.getValues('costs') || [];
		const existingIds = currentCosts.map(
			(c) => c.electricityUnitPriceEquipmentId,
		);

		const currentSelectedIds = form.getValues('electricityUnitPriceIds') || [];

		// Only keep IDs that still exist in costs
		const validIds = currentSelectedIds.filter((id) =>
			existingIds.includes(id),
		);

		if (validIds.length !== currentSelectedIds.length) {
			form.setValue('electricityUnitPriceIds', validIds, {
				shouldValidate: false,
			});
		}
	}, [watchedCosts, form, isInit]);

	const handleSubmit = async ({
		productUnitPriceId,
		outputId,
		trimmingCoefficient,
		costs,
	}: PlanElectricityCostSchema) => {
		try {
			const payload = {
				productUnitPriceId,
				outputId,
				costs: costs.map((cost) => ({
					...cost,
					adjustmentFactorDescriptions: cost.adjustmentFactorDescriptions.map(
						(adjustment) => ({
							adjustmentFactorDescriptionId:
								adjustment.adjustmentFactorDescriptionId || null,
							adjustmentFactorId: adjustment.adjustmentFactorId || null,
							customValue: adjustment.customValue,
						}),
					),
				})),
				trimmingCoefficient:
					plan?.fixedKeyType === ProcessGroupType.XL
						? trimmingCoefficient / 100
						: 1,
			};

			if (id) {
				await api.put(API.COST.PLANNED_ELECTRICITY.UPDATE, {
					id,
					...payload,
				});
			} else {
				await api.post(API.COST.PLANNED_ELECTRICITY.CREATE, payload);
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
		<FormProvider context={form} onSubmit={handleSubmit}>
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
			{plan?.fixedKeyType === ProcessGroupType.XL && (
				<FormNumber
					control={form.control}
					name='trimmingCoefficient'
					label='Hệ số xén lò (%)'
					placeholder='Nhập hệ số xén lò'
				/>
			)}

			<FormMultiSelect
				control={form.control}
				name='electricityUnitPriceIds'
				label='Mã thiết bị'
				placeholder='Chọn mã thiết bị'
				options={electricities.map((item) => ({
					label: `${item.equipmentCode} - ${item.equipmentName}`,
					value: item.id,
				}))}
			/>
			<div className='scrollbar-sm flex max-h-100 flex-1 overflow-auto p-2'>
				<FormArray control={form.control} name='costs' hasAddButton={false}>
					{(index) => {
						const watchedQuantity = form.watch(`costs.${index}.quantity`);
						const watchedelEctricityUnitPriceEquipmentId = form.watch(
							`costs.${index}.electricityUnitPriceEquipmentId`,
						);
						const watchedAdjustmentFactorDescriptions =
							form.watch(`costs.${index}.adjustmentFactorDescriptions`) || [];

						const currentElectricity = electricities.find(
							(electricity) =>
								electricity.id === watchedelEctricityUnitPriceEquipmentId,
						);

						return (
							<>
								<div className='min-w-32 flex-1 space-y-2'>
									<Label>Mã thiết bị</Label>
									<Input readOnly value={currentElectricity?.equipmentCode} />
								</div>

								<div className='min-w-32 flex-1 space-y-2'>
									<Label>Tên thiết bị</Label>
									<Input readOnly value={currentElectricity?.equipmentName} />
								</div>

								<div className='flex-1 space-y-2'>
									<Label>Đơn giá điện năng (đ/kWh)</Label>
									<Input
										readOnly
										value={formatNumber(
											currentElectricity?.electricityCostPerMetres || 0,
										)}
									/>
								</div>

								<FormNumber
									control={form.control}
									name={`costs.${index}.quantity`}
									label='Số lượng'
									placeholder='Nhập số lượng'
									className='min-w-32'
								/>

								{adjustments.map((adjustment, idx) => {
									return (
										<div className='w-full min-w-64 flex-1' key={adjustment.id}>
											<CostPlanAdjustmentFactorInput
												label={adjustment.code}
												placeholder={`Chọn ${adjustment.code}`}
												customPlaceholder={`Nhập ${adjustment.code}`}
												adjustmentFactorId={adjustment.id}
												value={watchedAdjustmentFactorDescriptions[idx]}
												error={
													form.formState.errors.costs?.[index]
														?.adjustmentFactorDescriptions?.[idx]
														?.adjustmentFactorDescriptionId?.message ??
													form.formState.errors.costs?.[index]
														?.adjustmentFactorDescriptions?.[idx]?.customValue
														?.message
												}
												onChange={(
													value: PlanedElectricityCostAdjustmentSelection,
												) => {
													form.setValue(
														`costs.${index}.adjustmentFactorDescriptions.${idx}`,
														value,
														{ shouldValidate: true, shouldDirty: true },
													);
												}}
												options={adjustment.adjustmentFactorDescriptions.map(
													({
														id,
														description,
														electricityAdjustmentValue,
														maintenanceAdjustmentValue,
													}) => ({
														label: formatAdjustmentOptionLabel(
															description,
															electricityAdjustmentValue ??
																maintenanceAdjustmentValue,
														),
														value: id,
													}),
												)}
											/>
										</div>
									);
								})}

								<div className='flex-1 space-y-2'>
									<Label>Doanh thu SCTX kế hoạch ban đầu (đ)</Label>
									<Input
										readOnly
										value={formatNumber(
											(() => {
												let total =
													watchedQuantity *
													(currentElectricity?.electricityCostPerMetres || 0);

												adjustments.forEach((adjustment, adjustmentIdx) => {
													const selectedDescriptionId =
														watchedAdjustmentFactorDescriptions[adjustmentIdx];

													if (!selectedDescriptionId) {
														return;
													}

													if (selectedDescriptionId.customValue !== null) {
														total *= selectedDescriptionId.customValue;
														return;
													}

													const description =
														adjustment.adjustmentFactorDescriptions.find(
															(desc) =>
																desc.id ===
																selectedDescriptionId.adjustmentFactorDescriptionId,
														);

													total *= description?.electricityAdjustmentValue || 1;
												});

												return total || 0;
											})(),
										)}
									/>
								</div>
							</>
						);
					}}
				</FormArray>
			</div>
			<DataTableEditConfirm isEdit={!!id} />
		</FormProvider>
	);
}
