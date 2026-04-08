import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
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
	PLAN_MAINTAIN_COST_DEFAULT,
	planMaintainCostSchema,
	PlanMaintainCostSchema,
} from '@/features/main/cost/plan/planed-maintain-cost/schema';
import {
	AdjustmentDetail,
	PlannedMaintainCostDetail,
} from '@/features/main/cost/plan/planed-maintain-cost/types';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import { Tunneling } from '@/features/main/pricing/tunneling/maintenance/columns';
import { api } from '@/lib/api';
import { formatDate, formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { AdjustmentFactorType } from '@/constants/adjustment-factor-type';

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
	const filteredTunnelings = useMemo(() => {
		const currentProcessGroupType = plan?.processGroupType as
			| ProcessGroupType
			| undefined;

		if (!currentProcessGroupType) {
			return tunnelings;
		}

		return tunnelings.filter((item) =>
			item.processGroupTypes?.includes(currentProcessGroupType),
		);
	}, [tunnelings, plan?.processGroupType]);

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
					.sort((a, b) => a.code.localeCompare(b.code))
					.slice(0, 8),
			);

			if (!id) return;

			api
				.get<PlannedMaintainCostDetail>(API.COST.PLANNED_MAINTAIN.DETAIL(id))
				.then((res) => {
					form.reset({
						productUnitPriceId: plan?.id,
						outputId: output?.id,
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
									.sort((a, b) => a.code.localeCompare(b.code))
									.slice(0, 8)
									.filter((adj) => adj.type !== AdjustmentFactorType.K6); // Exclude K6

								return {
									maintainUnitPriceId,
									quantity,
									k6AdjustmentFactorValue,
									adjustmentFactorDescriptions: sortedAdjustments.map(
										(adjustment) => {
											// Find the matching description by adjustmentFactorId
											const description = adjustmentFactorDescriptions.find(
												(desc) => desc.adjustmentFactorId === adjustment.id,
											);

											return description?.id || '';
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
						{
							length:
								adjustments.filter(
									(adj) => adj.type !== AdjustmentFactorType.K6,
								).length || 7,
						},
						() => '',
					),
				};
			});

		if (updatedCosts.length !== currentCosts.length) {
			form.setValue('costs', updatedCosts);
		}
	}, [watchedMaintainUnitPriceIds, form, tunnelings, isInit, adjustments]);

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
			if (id) {
				await api.put(API.COST.PLANNED_MAINTAIN.UPDATE, { id, ...values });
			} else {
				await api.post(API.COST.PLANNED_MAINTAIN.CREATE, values);
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
										value={formatNumber(
											Math.round(currentTunneling?.totalPrice || 0),
											{
												maximumFractionDigits: 0,
											},
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

								{(() => {
									let descriptionIndex = 0;
									return adjustments.map((adjustment) => {
										if (adjustment.type === AdjustmentFactorType.K6) {
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
												<FormComboBox
													control={form.control}
													name={`costs.${index}.adjustmentFactorDescriptions.${currentIndex}`}
													label={adjustment.code}
													placeholder={`Chọn ${adjustment.code}`}
													options={adjustment.adjustmentFactorDescriptions.map(
														({ id, description }) => ({
															label: description,
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
													(adj) => adj.type !== AdjustmentFactorType.K6,
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

														const description =
															adjustment.adjustmentFactorDescriptions.find(
																(desc) => desc.id === selectedDescriptionId,
															);

														total *=
															description?.maintenanceAdjustmentValue || 1;
													},
												);

												return Math.round(total || 0);
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
