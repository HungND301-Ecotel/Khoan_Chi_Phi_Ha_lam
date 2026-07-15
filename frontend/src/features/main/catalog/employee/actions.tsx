import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormInput } from '@/components/form/form-input';
import { FormSelect } from '@/components/form/form-select';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Employee } from '@/features/main/catalog/employee/columns';
import {
	EMPLOYEE_FORM_DEFAULT,
	EmployeeFormSchema,
	employeeFormSchema,
} from '@/features/main/catalog/employee/schema';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

type EmployeeFormProps = ActionDialogProps<Employee> & {
	isDuplicate?: boolean;
};

export function EmployeeForm({
	data,
	row,
	isDuplicate = false,
}: EmployeeFormProps) {
	const { setOpen } = useDialog();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const [departments, setDepartments] = useState<
		{ value: string; label: string }[]
	>([]);
	const [positions, setPositions] = useState<
		{ value: string; label: string }[]
	>([]);

	const form = useForm<EmployeeFormSchema>({
		resolver: zodResolver(employeeFormSchema) as any,
		mode: 'onSubmit',
		defaultValues: {
			...EMPLOYEE_FORM_DEFAULT,
			positionId: '' as any,
		},
	});

	useEffect(() => {
		const fetchLookupData = async () => {
			try {
				const [deptRes, posRes] = await Promise.all([
					api.pagging<any>(API.CATALOG.DEPARTMENT.LIST, {
						ignorePagination: true,
					}),
					api.pagging<any>(API.CATALOG.POSITION.LIST, {
						ignorePagination: true,
					}),
				]);

				if (deptRes?.result?.data) {
					setDepartments(
						deptRes.result.data.map((d: any) => ({
							value: d.id.toString(),
							label: d.name,
						})),
					);
				}
				if (posRes?.result?.data) {
					setPositions(
						posRes.result.data.map((p: any) => ({
							value: p.id.toString(),
							label: p.name,
						})),
					);
				}
			} catch (error) {
				console.error('Lỗi lấy danh sách tham chiếu', error);
			}
		};
		fetchLookupData();
	}, []);

	useEffect(() => {
		if (!row) return;
		form.reset({
			fullName: row.fullName,
			userName: row.userName || '',
			positionId: row.positionId?.toString() as any,
			departmentId: row.departmentId?.toString() || '',
			email: row.email || '',
			phoneNumber: row.phoneNumber || '',
			cccd: row.cccd || '',
			province: row.province || '',
			district: row.district || '',
			ward: row.ward || '',
			streetAddress: row.streetAddress || '',
		});
	}, [row, form, isDuplicate]);

	const handleSubmit = async (values: EmployeeFormSchema) => {
		try {
			if (row?.id && !isDuplicate) {
				await api.put(`${API.CATALOG.EMPLOYEE.UPDATE}/${row.id}`, {
					id: row.id,
					...values,
				});
			} else {
				await api.post(API.CATALOG.EMPLOYEE.CREATE, values);
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
		<FormProvider context={form as any} onSubmit={handleSubmit as any}>
			<div className='grid grid-cols-2 gap-4'>
				<FormInput
					control={form.control}
					name='fullName'
					label='Họ và tên'
					placeholder='Nhập họ và tên'
				/>
				<FormInput
					control={form.control}
					name='userName'
					label='Tên đăng nhập'
					placeholder='Nhập tên đăng nhập'
					disabled={!!row && !isDuplicate}
				/>
				<FormSelect
					control={form.control}
					name='departmentId'
					label='Phòng ban'
					placeholder='Chọn phòng ban'
					options={departments}
				/>
				<FormSelect
					control={form.control}
					name='positionId'
					label='Chức vụ'
					placeholder='Chọn chức vụ'
					options={positions}
				/>
				<FormInput
					control={form.control}
					name='email'
					label='Email'
					placeholder='Nhập email'
				/>
				<FormInput
					control={form.control}
					name='phoneNumber'
					label='Số điện thoại'
					placeholder='Nhập số điện thoại'
				/>
				<FormInput
					control={form.control}
					name='cccd'
					label='CCCD/CMND'
					placeholder='Nhập số CCCD/CMND'
				/>
				<FormInput
					control={form.control}
					name='province'
					label='Tỉnh/Thành phố'
					placeholder='Nhập Tỉnh/Thành phố'
				/>
				<FormInput
					control={form.control}
					name='district'
					label='Quận/Huyện'
					placeholder='Nhập Quận/Huyện'
				/>
				<FormInput
					control={form.control}
					name='ward'
					label='Phường/Xã'
					placeholder='Nhập Phường/Xã'
				/>
				<FormInput
					control={form.control}
					name='streetAddress'
					label='Số nhà, tên đường'
					placeholder='Nhập số nhà, tên đường'
				/>
			</div>
			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}
