import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { API } from '@/constants/api-enpoint';
import { LowValuePerishableSupplyType } from '@/constants/low-value-perishable-supply';
import { ProcessGroupType } from '@/constants/process-group';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Department } from '@/features/main/catalog/department/columns';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import {
	LOW_VALUE_PERISHABLE_SUPPLY_FORM_DEFAULT,
	lowValuePerishableSupplyFormSchema,
	LowValuePerishableSupplyFormSchema,
} from '@/features/main/pricing/low-value-perishable-supply/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { LowValuePerishableSupplyUnitPrice } from './columns';

type LowValuePerishableSupplyFormProps =
	ActionDialogProps<LowValuePerishableSupplyUnitPrice> & {
		type: LowValuePerishableSupplyType;
		isDuplicate?: boolean;
	};

export function LowValuePerishableSupplyForm({
	data,
	row,
	type,
	isDuplicate = false,
}: LowValuePerishableSupplyFormProps) {
	useMeta();
	const { setOpen } = useDialog();
	const [departments, setDepartments] = useState<Department[]>([]);
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);

	const form = useForm<LowValuePerishableSupplyFormSchema>({
		resolver: zodResolver(lowValuePerishableSupplyFormSchema),
		mode: 'onSubmit',
		defaultValues: LOW_VALUE_PERISHABLE_SUPPLY_FORM_DEFAULT,
	});

	useEffect(() => {
		Promise.all([
			api.pagging<Department>(API.CATALOG.DEPARTMENT.LIST, {
				ignorePagination: true,
			}),
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
				ignorePagination: true,
			}),
		]).then(([departmentsRes, processGroupsRes]) => {
			setDepartments(departmentsRes.result.data);
			setProcessGroups(
				(processGroupsRes.result.data ?? []).map((item) => ({
					...item,
					fixedKeyType: item.fixedKeyType,
				})),
			);

			if (!row) {
				return;
			}

			form.reset({
				departmentId: row.departmentId,
				processGroupId: row.processGroupId,
				startMonth: row.startMonth.substring(0, 10),
				endMonth: row.endMonth.substring(0, 10),
				totalPrice: row.totalPrice,
			});
		});
	}, [form, row]);

	const filteredProcessGroups = useMemo(() => {
		const processGroupType =
			type === LowValuePerishableSupplyType.TunnelExcavation
				? ProcessGroupType.DL
				: ProcessGroupType.LC;

		return processGroups.filter(
			(item) => item.fixedKeyType === processGroupType,
		);
	}, [processGroups, type]);

	const handleSubmit = async (values: LowValuePerishableSupplyFormSchema) => {
		const payload = {
			...values,
			type,
		};

		if (row && !isDuplicate) {
			await api.put(
				type === LowValuePerishableSupplyType.TunnelExcavation
					? API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TUNNELING.UPDATE
					: API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.LONGWALL.UPDATE,
				{ id: row.id, ...payload },
			);
		} else {
			await api.post(
				type === LowValuePerishableSupplyType.TunnelExcavation
					? API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TUNNELING.CREATE
					: API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.LONGWALL.CREATE,
				[payload],
			);
		}

		setOpen(false);
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
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

			<FormComboBox
				control={form.control}
				name='departmentId'
				label='Đơn vị'
				placeholder='Chọn đơn vị'
				options={departments.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='processGroupId'
				label='Nhóm công đoạn'
				placeholder='Chọn nhóm công đoạn'
				options={filteredProcessGroups.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
			/>

			<FormNumber
				control={form.control}
				name='totalPrice'
				label='Đơn giá (đ/m)'
				placeholder='Nhập đơn giá'
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
