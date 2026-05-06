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
import { AdjustmentFactorType } from '@/constants/adjustment-factor-type';
import { ProcessGroupType } from '@/constants/process-group';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import {
	PLAN_MAINTAIN_ADJUSTMENT_DEFAULT,
	PLAN_MAINTAIN_COST_DEFAULT,
	planMaintainCostSchema,
	PlanMaintainCostSchema,
} from '@/features/main/cost/plan/planed-maintain-cost/schema';
import {
	AdjustmentDetail,
	PlannedMaintainCostAdjustmentSelection,
	PlannedMaintainCostDetail,
} from '@/features/main/cost/plan/planed-maintain-cost/types';
import { CostPlanAdjustmentFactorInput } from '@/features/main/cost/plan/components/cost-plan-adjustment-factor-input';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import { Tunneling } from '@/features/main/pricing/tunneling/maintenance/columns';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';

function isK6Adjustment(adjustment: AdjustmentDetail) {
	return (
		adjustment.fixedKeyKey === 'K6' ||
		adjustment.fixedKeyType === AdjustmentFactorType.K6
	);
}

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

export function PlanMaintainCostForm({
	id,
	plan,
	output,
	callback,
}: ProductCostFormProps) {
	const [tunnelings, setTunnelings] = useState<Tunneling[]>([]);
	const [adjustments, setAdjustments] = useState<AdjustmentDetail[]>([]);
	const [isInit, setIsInit] = useState<boolean>(!!id);

	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();

	const form = useForm<PlanMaintainCostSchema>({
		resolver: zodResolver(planMaintainCostSchema),
		mode: 'onSubmit',
		defaultValues: {
			...PLAN_MAINTAIN_COST_DEFAULT,
			productUnitPriceId: plan?.id,
			outputId: output?.id,
		},
	});

	const watchedMaintainUnitPriceIds = form.watch('maintainUnitPriceIds');
	const watchedCosts = form.watch('costs');
	const maintainAdjustmentCount =
		plan?.fixedKeyType === ProcessGroupType.DL ||
		plan?.fixedKeyType === ProcessGroupType.XL
			? 7
			: 8;
	const filteredTunnelings = useMemo(() => {
		const currentProcessGroupType = plan?.fixedKeyType as
			| ProcessGroupType
			| undefined;

		if (!currentProcessGroupType) {
			return tunnelings;
		}

		const maintainTypeByProcessGroup: Record<ProcessGroupType, number> = {
			[ProcessGroupType.None]: 0,
			[ProcessGroupType.DL]: 1,
			[ProcessGroupType.LC]: 2,
			[ProcessGroupType.XL]: 3,
		};

		return tunnelings.filter(
			(item) =>
				item.type === maintainTypeByProcessGroup[currentProcessGroupType],
		);
	}, [tunnelings, plan?.fixedKeyType]);
	const selectableAdjustments = useMemo(
		() => adjustments.filter((adj) => !isK6Adjustment(adj)),
		[adjustments],
	);

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<Tunneling>(API.PRICING.MAINTENANCE.LIST),
			api.get<AdjustmentDetail[]>(API.CATALOG.ADJUSTMENT.FACTOR.DETAILS, {
				processGroupId: plan?.processGroupId || '',
			}),
		]);

		promises.then(([tunnelings, adjustments]) => {
			setTunnelings(tunnelings.result.data);
			setAdjustments(
				adjustments.result
					.sort((a, b) =>
						(a.fixedKeyKey ?? a.code).localeCompare(b.fixedKeyKey ?? b.code),
					)
					.slice(0, maintainAdjustmentCount),
			);

			if (!id) return;

			api
				.get<PlannedMaintainCostDetail>(API.COST.PLANNED_MAINTAIN.DETAIL(id))
				.then((res) => {
					form.reset({
						productUnitPriceId: plan?.id,
						outputId: output?.id,
						trimmingCoefficient: (res.result.trimmingCoefficient || 1) * 100,
						maintainUnitPriceIds: res.result.costs.map((detail) => {
							return detail.maintainUnitPriceId;
						}),
						costs: res.result.costs.map(
							({
								maintainUnitPriceId,
								quantity,
								k6AdjustmentFactorValue,
								adjustmentFactorDescriptions,
							}) => {
								const sortedAdjustments = adjustments.result
									.sort((a, b) =>
										(a.fixedKeyKey ?? a.code).localeCompare(
											b.fixedKeyKey ?? b.code,
										),
									)
									.slice(0, maintainAdjustmentCount)
									.filter((adj) => !isK6Adjustment(adj)); // Exclude K6

								return {
									maintainUnitPriceId,
									quantity,
									k6AdjustmentFactorValue,
									adjustmentFactorDescriptions: sortedAdjustments.map(
										(adjustment) => {
											const description = adjustmentFactorDescriptions.find(
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
	}, [id, form, plan, output, maintainAdjustmentCount]);

	useEffect(() => {
		if (isInit) return;

		const currentCosts = form.getValues('costs') || [];

		const updatedCosts: PlanMaintainCostSchema['costs'] =
			watchedMaintainUnitPriceIds.map((selectedId) => {
				const existingEntry = currentCosts.find(
					(c) => c.maintainUnitPriceId === selectedId,
				);

				if (existingEntry) return existingEntry;

				return {
					maintainUnitPriceId: selectedId,
					quantity: NaN,
					k6AdjustmentFactorValue: 0,
					adjustmentFactorDescriptions: Array.from(
						{ length: selectableAdjustments.length || 7 },
						() => ({ ...PLAN_MAINTAIN_ADJUSTMENT_DEFAULT }),
					),
				};
			});

		if (updatedCosts.length !== currentCosts.length) {
			form.setValue('costs', updatedCosts);
		}
	}, [
		watchedMaintainUnitPriceIds,
		form,
		tunnelings,
		isInit,
		selectableAdjustments,
	]);

	// Sync maintainUnitPriceIds when costs are removed
	useEffect(() => {
		if (isInit) return;

		const currentCosts = form.getValues('costs') || [];
		const existingIds = currentCosts.map((c) => c.maintainUnitPriceId);

		const currentSelectedIds = form.getValues('maintainUnitPriceIds') || [];

		// Only keep IDs that still exist in costs
		const validIds = currentSelectedIds.filter((id) =>
			existingIds.includes(id),
		);

		if (validIds.length !== currentSelectedIds.length) {
			form.setValue('maintainUnitPriceIds', validIds, {
				shouldValidate: false,
			});
		}
	}, [watchedCosts, form, isInit]);

	const handleSubmit = async (values: PlanMaintainCostSchema) => {
		try {
			const payload = {
				...values,
				costs: values.costs.map((cost) => ({
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
						? values.trimmingCoefficient / 100
						: 1,
			};

			if (id) {
				await api.put(API.COST.PLANNED_MAINTAIN.UPDATE, { id, ...payload });
			} else {
				await api.post(API.COST.PLANNED_MAINTAIN.CREATE, payload);
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
				name='maintainUnitPriceIds'
				label='Mã thiết bị'
				placeholder='Chọn mã thiết bị'
				options={filteredTunnelings.map((item) => ({
					label: `${item.equipmentCode} - ${item.equipmentName}`,
					value: item.id,
				}))}
			/>
			<div className='scrollbar-sm flex max-h-100 flex-1 overflow-auto p-2'>
				<FormArray control={form.control} name='costs' hasAddButton={false}>
					{(index) => {
						const watchedQuantity = form.watch(`costs.${index}.quantity`);
						const watchedMaintainUnitPriceId = form.watch(
							`costs.${index}.maintainUnitPriceId`,
						);
						const watchedAdjustmentFactorDescriptions =
							form.watch(`costs.${index}.adjustmentFactorDescriptions`) || [];

						const currentTunneling = tunnelings.find(
							(tunneling) => tunneling.id === watchedMaintainUnitPriceId,
						);

						return (
							<>
								<div className='min-w-32 flex-1 space-y-2'>
									<Label>Mã thiết bị</Label>
									<Input readOnly value={currentTunneling?.equipmentCode} />
								</div>

								<div className='min-w-32 flex-1 space-y-2'>
									<Label>Tên thiết bị</Label>
									<Input readOnly value={currentTunneling?.equipmentName} />
								</div>

								<div className='flex-1 space-y-2'>
									<Label>Đơn giá phụ tùng (đ)</Label>
									<Input
										readOnly
										value={formatNumber(currentTunneling?.totalPrice || 0)}
									/>
								</div>

								<FormNumber
									control={form.control}
									name={`costs.${index}.quantity`}
									label='Số lượng'
									placeholder='Nhập số lượng'
									className='min-w-32'
								/>

								{(() => {
									let descriptionIndex = 0;
									return adjustments.map((adjustment) => {
										if (isK6Adjustment(adjustment)) {
											return (
												<div
													className='w-full min-w-64 flex-1'
													key={adjustment.id}
												>
													<FormNumber
														control={form.control}
														name={`costs.${index}.k6AdjustmentFactorValue`}
														label={adjustment.code}
														placeholder={`Nhập ${adjustment.code}`}
														className='min-w-32'
													/>
												</div>
											);
										}

										const currentIndex = descriptionIndex++;
										return (
											<div
												className='w-full min-w-64 flex-1'
												key={adjustment.id}
											>
												<CostPlanAdjustmentFactorInput
													label={adjustment.code}
													placeholder={`Chọn ${adjustment.code}`}
													customPlaceholder={`Nhập ${adjustment.code}`}
													adjustmentFactorId={adjustment.id}
													value={
														watchedAdjustmentFactorDescriptions[currentIndex]
													}
													error={
														form.formState.errors.costs?.[index]
															?.adjustmentFactorDescriptions?.[currentIndex]
															?.adjustmentFactorDescriptionId?.message ??
														form.formState.errors.costs?.[index]
															?.adjustmentFactorDescriptions?.[currentIndex]
															?.customValue?.message
													}
													onChange={(
														value: PlannedMaintainCostAdjustmentSelection,
													) => {
														form.setValue(
															`costs.${index}.adjustmentFactorDescriptions.${currentIndex}`,
															value,
															{ shouldValidate: true, shouldDirty: true },
														);
													}}
													options={adjustment.adjustmentFactorDescriptions.map(
														({
															id,
															description,
															maintenanceAdjustmentValue,
														}) => ({
															label: formatAdjustmentOptionLabel(
																description,
																maintenanceAdjustmentValue,
															),
															sortValue: maintenanceAdjustmentValue,
															value: id,
														}),
													)}
												/>
											</div>
										);
									});
								})()}

								<div className='flex-1 space-y-2'>
									<Label>Doanh thu SCTX kế hoạch ban đầu (đ)</Label>
									<Input
										readOnly
										value={formatNumber(
											(() => {
												let total =
													watchedQuantity * (currentTunneling?.totalPrice || 0);

												const watchedK6Value = form.watch(
													`costs.${index}.k6AdjustmentFactorValue`,
												);

												total *= watchedK6Value || 1;

												const nonK6Adjustments = adjustments.filter(
													(adj) => !isK6Adjustment(adj),
												);

												nonK6Adjustments.forEach(
													(adjustment, adjustmentIdx) => {
														const selectedDescriptionId =
															watchedAdjustmentFactorDescriptions[
																adjustmentIdx
															];

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

														total *=
															description?.maintenanceAdjustmentValue || 1;
													},
												);

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
