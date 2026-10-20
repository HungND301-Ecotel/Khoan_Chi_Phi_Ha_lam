import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import {
	FormBatch,
	reportBatchResult,
	useFormBatch,
} from '@/components/form/form-batch';
import { FormInput } from '@/components/form/form-input';
import { FormSelect } from '@/components/form/form-select';
import { FormProvider } from '@/components/form/form-provider';
import { FormDate } from '@/components/form/form-date';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Employee } from '@/features/main/catalog/employee/columns';
import {
	EMPLOYEE_FORM_DEFAULT,
	employeeBatchSchema,
	EmployeeBatchSchema,
	EmployeeFormSchema,
} from '@/features/main/catalog/employee/schema';
import { useCatalogQuickCreate } from '@/features/main/catalog/quick-create';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

type EmployeeFormProps = ActionDialogProps<Employee> & {
	isDuplicate?: boolean;
	/** Chỉ tạo một bản ghi (vd: tạo nhanh từ dropdown) */
	single?: boolean;
};

// Chức vụ để trống (thay vì 0) để dropdown hiển thị placeholder
const EMPLOYEE_ITEM_DEFAULT: EmployeeFormSchema = {
	...EMPLOYEE_FORM_DEFAULT,
	// eslint-disable-next-line @typescript-eslint/no-explicit-any -- positionId dùng z.coerce, giá trị rỗng chỉ để hiển thị
	positionId: '' as any,
};

// Giới tính lưu dạng chuỗi 'true'/'false' trên form, gửi lên dạng boolean
const toPayload = (item: EmployeeFormSchema) => {
	// eslint-disable-next-line @typescript-eslint/no-explicit-any -- genre có thể là boolean hoặc chuỗi từ dropdown
	const isMale = item.genre === true || (item.genre as any) === 'true';
	return { ...item, genre: isMale, gender: isMale };
};

export function EmployeeForm({
	data,
	row,
	isDuplicate = false,
	single = false,
}: EmployeeFormProps) {
	const { setOpen } = useDialog();
	const popup = usePopup();
	const { breadcrumb } = useMeta();
	const isEdit = !!row?.id && !isDuplicate;

	const [departments, setDepartments] = useState<
		{ value: string; label: string }[]
	>([]);
	const [positions, setPositions] = useState<
		{ value: string; label: string }[]
	>([]);

	const form = useForm<EmployeeBatchSchema>({
		// eslint-disable-next-line @typescript-eslint/no-explicit-any -- positionId dùng z.coerce nên kiểu input/output khác nhau
		resolver: zodResolver(employeeBatchSchema) as any,
		mode: 'onSubmit',
		defaultValues: {
			items: [
				row
					? {
							code: isDuplicate ? '' : row.code || '',
							fullName: row.fullName,
							userName: row.userName || '',
							positionId: row.positionId ?? 0,
							departmentId: row.departmentId
								? String(row.departmentId).toLowerCase()
								: '',
							email: row.email || '',
							phoneNumber: row.phoneNumber || '',
							cccd: row.cccd || '',
							dob: row.dob || '',
							genre: row.genre === false ? 'false' : 'true',
							note: row.note || '',
						}
					: EMPLOYEE_ITEM_DEFAULT,
			],
		},
	});
	const batch = useFormBatch(form);

	// Nạp danh sách lựa chọn Đơn vị / Chức vụ (gọi lại sau khi tạo nhanh từ dropdown)
	const loadOptions = useCallback(
		(
			url: string,
			setOptions: (options: { value: string; label: string }[]) => void,
		) =>
			api
				.pagging<{ id: string | number; name: string }>(url, {
					ignorePagination: true,
				})
				.then((res) => {
					if (!res?.result?.data) return;
					setOptions(
						res.result.data.map((item) => ({
							value: String(item.id).toLowerCase(),
							label: item.name,
						})),
					);
				})
				.catch((error) => console.error('Lỗi lấy danh sách tham chiếu', error)),
		[],
	);

	const loadDepartments = useCallback(
		() => loadOptions(API.CATALOG.DEPARTMENT.LIST, setDepartments),
		[loadOptions],
	);

	const loadPositions = useCallback(
		() => loadOptions(API.CATALOG.POSITION.LIST, setPositions),
		[loadOptions],
	);

	useEffect(() => {
		loadDepartments();
		loadPositions();
	}, [loadDepartments, loadPositions]);

	const departmentQuickCreate = useCatalogQuickCreate(
		'department',
		loadDepartments,
	);
	const positionQuickCreate = useCatalogQuickCreate('position', loadPositions);

	const refresh = async () => {
		await data?.refresh();
		data?.table.toggleAllRowsSelected(false);
	};

	const handleSubmit = async ({ items }: EmployeeBatchSchema) => {
		if (isEdit) {
			try {
				await api.put(`${API.CATALOG.EMPLOYEE.UPDATE}/${row.id}`, {
					id: row.id,
					...toPayload(items[0]),
				});
				setOpen(false);
				popup.success(`${breadcrumb} đã được Cập nhật thành công.`);
				await refresh();
			} catch (error) {
				popup.error(error);
			}
			return;
		}

		const result = await batch.submit(items, (item) =>
			api.post(API.CATALOG.EMPLOYEE.CREATE, toPayload(item)),
		);
		await reportBatchResult(result, {
			breadcrumb,
			popup,
			close: () => setOpen(false),
			refresh,
		});
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormBatch
				form={form}
				batch={batch}
				canAdd={!isEdit && !single}
				// Dòng mới giữ đơn vị và chức vụ của dòng trước
				newItem={(previous) => ({
					...EMPLOYEE_ITEM_DEFAULT,
					departmentId: previous?.departmentId ?? '',
					positionId: previous?.positionId ?? EMPLOYEE_ITEM_DEFAULT.positionId,
				})}
				plainClassName='grid grid-cols-2 gap-4'
				columns={[
					{
						key: 'code',
						header: 'Mã nhân viên',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.code`}
								label={label}
								placeholder='Nhập mã nhân viên'
							/>
						),
					},
					{
						key: 'fullName',
						header: 'Họ và tên',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.fullName`}
								label={label}
								placeholder='Nhập họ và tên'
							/>
						),
					},
					{
						key: 'userName',
						header: 'Tên đăng nhập',
						required: true,
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.userName`}
								label={label}
								placeholder='Nhập tên đăng nhập'
								disabled={isEdit}
							/>
						),
					},
					{
						key: 'departmentId',
						header: 'Đơn vị',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormSelect
								control={form.control}
								name={`items.${index}.departmentId`}
								label={label}
								placeholder='Chọn đơn vị'
								options={departments}
								quickCreate={departmentQuickCreate}
							/>
						),
					},
					{
						key: 'positionId',
						header: 'Chức vụ',
						required: true,
						className: 'min-w-48',
						render: ({ index, label }) => (
							<FormSelect
								control={form.control}
								name={`items.${index}.positionId`}
								label={label}
								placeholder='Chọn chức vụ'
								options={positions}
								quickCreate={positionQuickCreate}
							/>
						),
					},
					{
						key: 'email',
						header: 'Email',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.email`}
								label={label}
								placeholder='Nhập email'
							/>
						),
					},
					{
						key: 'phoneNumber',
						header: 'Số điện thoại',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.phoneNumber`}
								label={label}
								placeholder='Nhập số điện thoại'
							/>
						),
					},
					{
						key: 'cccd',
						header: 'CCCD/CMND',
						render: ({ index, label }) => (
							<FormInput
								control={form.control}
								name={`items.${index}.cccd`}
								label={label}
								placeholder='Nhập số CCCD/CMND'
							/>
						),
					},
					{
						key: 'dob',
						header: 'Ngày sinh',
						render: ({ index, label }) => (
							<FormDate
								control={form.control}
								name={`items.${index}.dob`}
								label={label}
								placeholder='Chọn ngày sinh'
							/>
						),
					},
					{
						key: 'genre',
						header: 'Giới tính',
						render: ({ index, label }) => (
							<FormSelect
								control={form.control}
								name={`items.${index}.genre`}
								label={label}
								placeholder='Chọn giới tính'
								options={[
									{ value: 'true', label: 'Nam' },
									{ value: 'false', label: 'Nữ' },
								]}
							/>
						),
					},
					{
						key: 'note',
						header: 'Ghi chú',
						// Form thường: ghi chú chiếm cả hàng
						render: ({ index, label }) => (
							<div className='col-span-2'>
								<FormInput
									control={form.control}
									name={`items.${index}.note`}
									label={label}
									placeholder='Nhập ghi chú (nếu có)'
								/>
							</div>
						),
					},
				]}
			/>
			<DataTableEditConfirm isEdit={isEdit} />
		</FormProvider>
	);
}
