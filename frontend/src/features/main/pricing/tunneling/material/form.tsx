import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Insert } from '@/features/main/catalog/parameter/insert/columns';
import type { Passport } from '@/features/main/catalog/parameter/passport/columns';
import type { Step } from '@/features/main/catalog/parameter/step/columns';
import type { Strength } from '@/features/main/catalog/parameter/strength/columns';
import type { ProcessStep } from '@/features/main/catalog/process/step/columns';
import type { Material } from '@/features/main/pricing/tunneling/material/columns';
import {
	MATERIAL_FORM_DEFAULT,
	materialFormSchema,
	type MaterialFormSchema,
} from '@/features/main/pricing/tunneling/material/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export function MaterialForm({ data, row }: ActionDialogProps<Material>) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [processes, setProcesses] = useState<ProcessStep[]>([]);
	const [passports, setPassports] = useState<Passport[]>([]);
	const [strengths, setStrengths] = useState<Strength[]>([]);
	const [inserts, setInserts] = useState<Insert[]>([]);
	const [steps, setSteps] = useState<Step[]>([]);

	const form = useForm<MaterialFormSchema>({
		resolver: zodResolver(materialFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...MATERIAL_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	useEffect(() => {
		const promises = Promise.all([
			api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
			api.pagging<Passport>(API.CATALOG.PARAMETER.PASSPORT.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
			api.pagging<Insert>(API.CATALOG.PARAMETER.INSERT.LIST),
			api.pagging<Step>(API.CATALOG.PARAMETER.STEP.LIST),
		]);

		promises.then(([processes, passports, strengths, inserts, steps]) => {
			setProcesses(processes.result.data);
			setPassports(passports.result.data);
			setStrengths(strengths.result.data);
			setInserts(inserts.result.data);
			setSteps(steps.result.data);

			if (row) {
				form.reset({
					startMonth: row.startMonth.substring(0, 10),
					endMonth: row.endMonth.substring(0, 10),
					code: row.code,
					processId: row.processId,
					passportId: row.passportId,
					hardnessId: row.hardnessId,
					insertItemId: row.insertItemId,
					supportStepId: row.supportStepId,
					// costs: [],
					totalPrice: row.totalPrice,
				});
			}
		});
	}, [form, row]);

	const handleSubmit = async (values: MaterialFormSchema) => {
		try {
			const processedValues = {
				...values,
			};
			if (row?.id) {
				await api.put(API.PRICING.MATERIAL.TUNNELING.UPDATE, {
					id: row.id,
					...processedValues,
				});
			} else {
				await api.post(API.PRICING.MATERIAL.TUNNELING.CREATE, processedValues);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormRow>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Thời gian bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control}
					name='endMonth'
					label='Thời gian kết thúc'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

			<FormInput
				control={form.control}
				name='code'
				label='Mã định mức vật liệu'
				placeholder='Nhập mã định mức vật liệu'
			/>

			<FormComboBox
				control={form.control}
				name='processId'
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={processes.map((process) => ({
					label: process.name,
					value: process.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='passportId'
				label='Hộ chiếu, Sđ, Sc'
				placeholder='Chọn hộ chiếu'
				options={passports.map((passport) => ({
					label: `H/c ${passport.name}; ${passport.sd}; ${passport.sc}`,
					value: passport.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='hardnessId'
				label='Độ kiên cố đá, than (f)'
				placeholder='Chọn Độ kiên cố đá, than (f)'
				options={strengths.map((strength) => ({
					label: strength.value,
					value: strength.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='insertItemId'
				label='Chèn'
				placeholder='Chọn chèn'
				options={inserts.map((item) => ({
					label: item.value,
					value: item.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='supportStepId'
				label='Bước chống'
				placeholder='Chọn bước chống'
				options={steps.map((item) => ({
					label: item.value,
					value: item.id,
				}))}
			/>

			<FormNumber
				control={form.control}
				name='totalPrice'
				label='Đơn giá vật liệu (đ/m)'
				placeholder='Nhập đơn giá vật liệu'
			/>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
