import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Equipment } from '@/features/main/catalog/equipment/columns';
import { ProductCostFormProps } from '@/features/main/cost/plan/types';
import {
	PRODUCTION_ELECTRICITY_COST_DEFAULT,
	productionElectricityCostSchema,
	ProductionElectricityCostSchema,
} from '@/features/main/cost/producttion/production/electricity-cost/schema';
import {
	ProductionActualElectricityCostDetail,
	ProductionElectricityCostItem,
} from '@/features/main/cost/producttion/production/electricity-cost/types';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';

type ProductionElectricityCostFormProps = ProductCostFormProps & {
	initialItems?: ProductionElectricityCostItem[];
	onSave?: (payload: {
		id: string;
		items: ProductionElectricityCostItem[];
	}) => void;
};

export function ProductionElectricityCostForm({
	id,
	output,
	callback,
	initialItems = [],
	onSave,
}: ProductionElectricityCostFormProps) {
	const [equipments, setEquipments] = useState<Equipment[]>([]);

	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();

	const form = useForm<ProductionElectricityCostSchema>({
		resolver: zodResolver(productionElectricityCostSchema),
		mode: 'onSubmit',
		defaultValues: {
			...PRODUCTION_ELECTRICITY_COST_DEFAULT,
			equipmentIds: initialItems.map((item) => item.equipmentId),
			costs: initialItems.map((item) => ({
				equipmentId: item.equipmentId,
				electricityConsumption: item.electricityConsumption,
			})),
		},
	});

	const watchedEquipmentIds = form.watch('equipmentIds');
	const watchedCosts = form.watch('costs');
	const initialItemsKey = useMemo(
		() =>
			initialItems
				.map((item) => `${item.equipmentId}:${item.electricityConsumption}`)
				.join('|'),
		[initialItems],
	);

	useEffect(() => {
		api
			.pagging<Equipment>(API.CATALOG.EQUIPMENT.LIST, {
				ignorePagination: true,
				...(output?.startMonth && { date: output.startMonth }),
			})
			.then((res) => {
				setEquipments(res.result.data);
				form.reset({
					equipmentIds: initialItems.map((item) => item.equipmentId),
					costs: initialItems.map((item) => ({
						equipmentId: item.equipmentId,
						electricityConsumption: item.electricityConsumption,
					})),
				});
			})
			.catch((err) => {
				console.error(
					'Failed to load equipment list for electricity cost:',
					err,
				);
			});
	}, [form, output?.startMonth, initialItems, initialItemsKey]);

	useEffect(() => {
		const currentCosts = form.getValues('costs') || [];

		const updatedCosts = watchedEquipmentIds.map((selectedId) => {
			const existingCost = currentCosts.find(
				(cost) => cost.equipmentId === selectedId,
			);

			if (existingCost) return existingCost;

			return {
				equipmentId: selectedId,
				electricityConsumption: NaN,
			};
		});

		const shouldUpdateCosts =
			updatedCosts.length !== currentCosts.length ||
			updatedCosts.some(
				(cost, index) => cost.equipmentId !== currentCosts[index]?.equipmentId,
			);

		if (shouldUpdateCosts) {
			form.setValue('costs', updatedCosts);
		}
	}, [form, watchedEquipmentIds]);

	useEffect(() => {
		const currentCosts = form.getValues('costs') || [];
		const existingIds = currentCosts
			.map((cost) => cost.equipmentId)
			.filter(Boolean);
		const currentSelectedIds = form.getValues('equipmentIds') || [];
		const validIds = currentSelectedIds.filter((id) =>
			existingIds.includes(id),
		);

		if (validIds.length !== currentSelectedIds.length) {
			form.setValue('equipmentIds', validIds, {
				shouldValidate: false,
			});
		}
	}, [watchedCosts, form]);

	const handleSubmit = async (values: ProductionElectricityCostSchema) => {
		try {
			if (!output?.acceptanceReportId) {
				throw new Error('AcceptanceReportId is required');
			}

			const equipmentsPayload = values.costs.map((item) => ({
				equipmentId: item.equipmentId,
				actualElectricityConsumption: item.electricityConsumption || 0,
			}));

			if (id) {
				await api.put(API.COST.ACTUAL_ELECTRICITY.UPDATE, {
					id,
					acceptanceReportId: output.acceptanceReportId,
					equipments: equipmentsPayload,
				});
			} else {
				await api.post(API.COST.ACTUAL_ELECTRICITY.CREATE, {
					acceptanceReportId: output.acceptanceReportId,
					equipments: equipmentsPayload,
				});
			}

			const detailRes = await api.get<ProductionActualElectricityCostDetail>(
				API.COST.ACTUAL_ELECTRICITY.DETAIL(output.acceptanceReportId),
			);

			const mappedItems: ProductionElectricityCostItem[] =
				detailRes.result.equipments.map((item, index) => ({
					id: `${item.equipmentId}-${index}`,
					equipmentId: item.equipmentId,
					equipmentCode: item.equipmentCode,
					equipmentName: item.equipmentName,
					electricityUnitPrice: item.electricityUnitPrice || 0,
					electricityConsumption: item.actualElectricityConsumption || 0,
					electricityCost: item.totalPrice || 0,
				}));

			onSave?.({
				id: detailRes.result.id,
				items: mappedItems,
			});
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
			<FormMultiSelect
				control={form.control}
				name='equipmentIds'
				label='Mã thiết bị'
				placeholder='Chọn mã thiết bị'
				options={equipments.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
			/>

			{watchedEquipmentIds.length > 0 && (
				<div className='scrollbar-sm flex max-h-100 flex-1 overflow-auto p-2'>
					<FormArray control={form.control} name='costs' hasAddButton={false}>
						{(index) => {
							const watchedEquipmentId = form.watch(
								`costs.${index}.equipmentId`,
							);
							const watchedConsumption = form.watch(
								`costs.${index}.electricityConsumption`,
							);
							const currentEquipment = equipments.find(
								(equipmentItem) => equipmentItem.id === watchedEquipmentId,
							);
							const electricityUnitPrice = currentEquipment?.currentPrice || 0;
							const electricityCost =
								electricityUnitPrice * (watchedConsumption || 0);

							return (
								<>
									<div className='min-w-32 flex-1 space-y-2'>
										<Label>Mã thiết bị</Label>
										<Input readOnly value={currentEquipment?.code || ''} />
									</div>
									<div className='min-w-32 flex-1 space-y-2'>
										<Label>Tên thiết bị</Label>
										<Input readOnly value={currentEquipment?.name || ''} />
									</div>
									<div className='min-w-48 flex-1 space-y-2'>
										<Label>Đơn giá điện năng (đ/kWh)</Label>
										<Input
											readOnly
											value={formatNumber(electricityUnitPrice)}
										/>
									</div>
									<FormNumber
										control={form.control}
										name={`costs.${index}.electricityConsumption`}
										label='Điện năng tiêu thụ (kWh)'
										placeholder='Nhập điện năng tiêu thụ'
										className='min-w-40'
									/>
									<div className='min-w-48 flex-1 space-y-2'>
										<Label>Chi phí điện năng (đ)</Label>
										<Input
											readOnly
											value={formatNumber(Math.round(electricityCost))}
										/>
									</div>
								</>
							);
						}}
					</FormArray>
				</div>
			)}

			<DataTableEditConfirm isEdit={!!id} />
		</FormProvider>
	);
}
