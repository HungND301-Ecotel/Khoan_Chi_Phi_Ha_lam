import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Unit } from '@/features/main/catalog/unit/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { OtherPart } from './columns';
import {
	OTHER_PART_SCHEMA_DEFAULT,
	otherPartSchema,
	OtherPartSchema,
} from './schema';

export type OtherPartDetail = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	costs: Array<{
		startMonth: string;
		endMonth: string;
		costType: number;
		amount: number;
		actualAmount: number;
	}>;
};

type OtherPartFormProps = ActionDialogProps<OtherPart> & {
	isDuplicate?: boolean;
};

export function OtherPartForm({
	data,
	row,
	isDuplicate = false,
}: OtherPartFormProps) {
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const popup = usePopup();
	const [units, setUnits] = useState<Unit[]>([]);

	const form = useForm<OtherPartSchema>({
		resolver: zodResolver(otherPartSchema),
		defaultValues: OTHER_PART_SCHEMA_DEFAULT,
		mode: 'onSubmit',
	});

	const lastSyncedPlanRef = useRef<Record<number, number>>({});
	const costs = useWatch({
		control: form.control,
		name: 'costs',
	});

	useEffect(() => {
		costs?.forEach((cost, index) => {
			const lastSyncedPlan = lastSyncedPlanRef.current[index];
			const isActualAmountEmpty =
				cost.actualAmount === undefined ||
				cost.actualAmount === null ||
				Number.isNaN(cost.actualAmount);
			const hasPlanAmount =
				cost.amount !== undefined &&
				cost.amount !== null &&
				!Number.isNaN(cost.amount);
			const wasAutoFilled =
				lastSyncedPlan !== undefined && cost.actualAmount === lastSyncedPlan;

			if ((isActualAmountEmpty || wasAutoFilled) && hasPlanAmount) {
				form.setValue(`costs.${index}.actualAmount`, cost.amount, {
					shouldDirty: true,
				});
				lastSyncedPlanRef.current[index] = cost.amount;
			}
		});
	}, [costs, form]);

	useEffect(() => {
		const promises = Promise.all([
			api
				.pagging<Unit>(API.CATALOG.UNIT.LIST)
				.then((res) => setUnits(res.result.data)),
		]);

		promises.then(() => {
			if (!row) return;
			api.get<OtherPartDetail>(API.CATALOG.PART.DETAIL(row.id)).then((res) => {
				const { costs, ...otherPart } = res.result;
				form.reset({
					...otherPart,
					code: isDuplicate ? '' : otherPart.code,
					costs: costs?.length
						? costs.map((cost) => ({
								startMonth: cost.startMonth.substring(0, 10),
								endMonth: cost.endMonth.substring(0, 10),
								amount: cost.amount,
								actualAmount: cost.actualAmount,
							}))
						: OTHER_PART_SCHEMA_DEFAULT.costs,
				});
			});
		});
	}, [row, form, isDuplicate]);

	const handleSubmit = async (values: OtherPartSchema) => {
		try {
			const processedValues = {
				...values,
				partType: 2,
			};
			if (row?.id && !isDuplicate) {
				await api.put(API.CATALOG.PART.UPDATE, {
					id: row?.id,
					...processedValues,
				});
			} else {
				await api.post(API.CATALOG.PART.CREATE, processedValues);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id && !isDuplicate ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormInput
				control={form.control}
				name='code'
				label='Mã phụ tùng'
				placeholder='Nhập mã phụ tùng, ví dụ: DL'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên phụ tùng'
				placeholder='Nhập tên phụ tùng, ví dụ: Đào lò'
			/>

			<FormComboBox
				control={form.control}
				name='unitOfMeasureId'
				label='Đơn vị tính'
				placeholder='Chọn đơn vị tính'
				options={units.map((unit) => ({
					value: unit.id,
					label: unit.name,
				}))}
			/>

			<FormArray control={form.control} name='costs' label='Đơn giá vật tư (đ)'>
				{(index) => (
					<div className='flex w-full gap-4'>
						<FormMonthYear
							control={form.control}
							name={`costs.${index}.startMonth`}
							label='Thời gian bắt đầu'
							className='flex-1'
						/>
						<FormMonthYear
							control={form.control}
							name={`costs.${index}.endMonth`}
							label='Thời gian kết thúc'
							className='flex-1'
						/>
						<div className='flex-1'>
							<FormNumber
								control={form.control}
								name={`costs.${index}.amount`}
								label='Đơn giá kế hoạch (đ)'
								placeholder='Nhập đơn giá kế hoạch (đ)'
							/>
						</div>
						<div className='flex-1'>
							<FormNumber
								control={form.control}
								name={`costs.${index}.actualAmount`}
								label='Đơn giá thực tế (đ)'
								placeholder='Nhập đơn giá thực tế (đ)'
							/>
						</div>
					</div>
				)}
			</FormArray>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
