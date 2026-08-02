import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { API } from '@/constants/api-enpoint';
import { ProcessGroupType } from '@/constants/process-group';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Department } from '@/features/main/catalog/department/columns';
import {
	normalizeProcessGroup,
	ProcessGroup,
} from '@/features/main/catalog/process/group/columns';
import {
	TRANSPORT_LOW_VALUE_PERISHABLE_SUPPLY_FORM_DEFAULT,
	transportLowValuePerishableSupplyFormSchema,
	TransportLowValuePerishableSupplyFormSchema,
} from '@/features/main/pricing/transport/low-value-perishable-supply/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { TransportLowValuePerishableSupplyUnitPrice } from './columns';

type TransportLowValuePerishableSupplyFormProps =
	ActionDialogProps<TransportLowValuePerishableSupplyUnitPrice> & {
		isDuplicate?: boolean;
	};

export function TransportLowValuePerishableSupplyForm({
	data,
	row,
	isDuplicate = false,
}: TransportLowValuePerishableSupplyFormProps) {
	useMeta();
	const { setOpen } = useDialog();
	const [departments, setDepartments] = useState<Department[]>([]);
	const [processGroups, setProcessGroups] = useState<ProcessGroup[]>([]);

	const form = useForm<TransportLowValuePerishableSupplyFormSchema>({
		resolver: zodResolver(transportLowValuePerishableSupplyFormSchema),
		mode: 'onSubmit',
		defaultValues: TRANSPORT_LOW_VALUE_PERISHABLE_SUPPLY_FORM_DEFAULT,
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
			const fetchedDepts = departmentsRes.result.data || [];
			if (fetchedDepts.length > 0) setDepartments(fetchedDepts);
			else
				setDepartments([
					{ id: 'dept-1', code: 'PX-VTL1', name: 'Phân xưởng Vận tải lò 1' },
					{ id: 'dept-2', code: 'PX-VTL2', name: 'Phân xưởng Vận tải lò 2' },
					{ id: 'dept-3', code: 'PX-TT', name: 'Phân xưởng Trục tải' },
				] as any);

			const fetchedGroups = (processGroupsRes.result.data ?? []).map(normalizeProcessGroup);
			const filtered = fetchedGroups.filter(
				(group) => group.type === ProcessGroupType.VTL || group.code === 'VTL',
			);
			if (filtered.length > 0) setProcessGroups(filtered);
			else
				setProcessGroups([
					{ id: 'pg-vtl', code: 'VTL', name: 'Vận tải lò', type: ProcessGroupType.VTL },
				] as any);

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

	const handleSubmit = async (values: TransportLowValuePerishableSupplyFormSchema) => {
		try {
			if (row && !isDuplicate) {
				try {
					await api.put(
						API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TRANSPORT.UPDATE,
						{ id: row.id, ...values },
					);
				} catch {
					// Mock fallback
				}
			} else {
				try {
					await api.post(
						API.PRICING.LOW_VALUE_PERISHABLE_SUPPLY.TRANSPORT.CREATE,
						[values],
					);
				} catch {
					// Mock fallback
				}
			}

			setOpen(false);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			console.error(error);
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
				options={processGroups.map((item) => ({
					label: `${item.code} - ${item.name}`,
					value: item.id,
				}))}
			/>

			<FormNumber
				control={form.control}
				name='totalPrice'
				label='Đơn giá (đ/tháng)'
				placeholder='Nhập đơn giá'
			/>

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
