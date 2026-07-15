import type { ColumnDef } from '@tanstack/react-table';
import { Switch } from '@/components/ui/switch';
import { Button } from '@/components/ui/button';
import { api } from '@/lib/api';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useState, useEffect } from 'react';
import LockResetIcon from '@mui/icons-material/LockReset';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';
import { ActionDialog } from '@/components/datatable/actions';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import {
	DialogHeader,
	DialogTitle,
	DialogDescription,
	DialogFooter,
	DialogClose,
} from '@/components/ui/dialog';

export type Employee = {
	id: number;
	fullName: string;
	positionId: number;
	positionName?: string;
	departmentId: string;
	departmentName?: string;
	userName?: string;
	dob: string;
	genre?: boolean;
	cccd: string;
	avatar: string;
	email?: string;
	phoneNumber?: string;
	isActive?: boolean;
};

const EmployeeStatusToggle = ({ row }: { row: Employee }) => {
	const popup = usePopup();
	const [isActive, setIsActive] = useState(row.isActive ?? true);
	const { hasPermission } = usePermission();
	const canUpdate = hasPermission(PERMISSIONS.CATALOG.EMPLOYEE.UPDATE);

	useEffect(() => {
		setIsActive(row.isActive ?? true);
	}, [row.isActive]);

	const handleToggle = async (checked: boolean) => {
		try {
			await api.patch(
				`${API.CATALOG.EMPLOYEE.LIST}/${row.id}/lock?isLocked=${!checked}`,
				{},
			);
			setIsActive(checked);
			popup.success(`Đã ${checked ? 'mở khóa' : 'khóa'} tài khoản thành công.`);
		} catch (error) {
			popup.error(error);
		}
	};

	if (!canUpdate) {
		return <Switch checked={isActive} disabled />;
	}

	return (
		<DialogProvider>
			<ActionDialog
				className='min-h-auto sm:max-w-md'
				trigger={
					<div
						className='relative flex items-center'
						role='button'
						tabIndex={0}
					>
						<Switch checked={isActive} className='pointer-events-none' />
					</div>
				}
			>
				<DialogHeader>
					<DialogTitle className='text-center uppercase'>
						Xác nhận {isActive ? 'khóa' : 'mở khóa'}
					</DialogTitle>
					<DialogDescription className='text-center'>
						Bạn có chắc chắn muốn {isActive ? 'khóa' : 'mở khóa'} tài khoản nhân
						viên này không?
					</DialogDescription>
				</DialogHeader>
				<DialogFooter className='flex w-full items-center sm:justify-center'>
					<DialogClose asChild>
						<Button variant='secondary' className='w-24'>
							Huỷ
						</Button>
					</DialogClose>
					<DialogClose asChild>
						<Button
							variant='warning'
							onClick={() => handleToggle(!isActive)}
							className='w-24'
						>
							Đồng ý
						</Button>
					</DialogClose>
				</DialogFooter>
			</ActionDialog>
		</DialogProvider>
	);
};

const EmployeeActions = ({ row }: { row: Employee }) => {
	const popup = usePopup();
	const { hasPermission } = usePermission();
	const canUpdate = hasPermission(PERMISSIONS.CATALOG.EMPLOYEE.UPDATE);

	const handleResetPassword = async () => {
		try {
			await api.post(
				`${API.CATALOG.EMPLOYEE.LIST}/${row.id}/reset-password`,
				{},
			);
			popup.success('Đã reset mật khẩu thành công.');
		} catch (error) {
			popup.error(error);
		}
	};

	if (!canUpdate) return null;

	return (
		<DialogProvider>
			<ActionDialog
				className='min-h-auto sm:max-w-md'
				trigger={
					<Button
						variant='ghost'
						size='icon-lg'
						className='rounded-full bg-transparent text-[#6e6e6e] shadow-none hover:bg-[#f0f0f0] hover:text-[#6e6e6e] hover:shadow-none'
					>
						<LockResetIcon fontSize='medium' />
					</Button>
				}
			>
				<DialogHeader>
					<DialogTitle className='text-center uppercase'>
						Xác nhận reset mật khẩu
					</DialogTitle>
					<DialogDescription className='text-center'>
						Bạn có chắc chắn muốn reset mật khẩu cho nhân viên này không?
					</DialogDescription>
				</DialogHeader>
				<DialogFooter className='flex w-full items-center sm:justify-center'>
					<DialogClose asChild>
						<Button variant='secondary' className='w-24'>
							Huỷ
						</Button>
					</DialogClose>
					<DialogClose asChild>
						<Button
							variant='warning'
							onClick={handleResetPassword}
							className='w-24'
						>
							Đồng ý
						</Button>
					</DialogClose>
				</DialogFooter>
			</ActionDialog>
		</DialogProvider>
	);
};

export const CATALOG_EMPLOYEE_COLUMNS: ColumnDef<Employee>[] = [
	{
		accessorKey: 'fullName',
		header: 'Họ và tên',
	},
	{
		accessorKey: 'userName',
		header: 'Tên đăng nhập',
	},
	{
		accessorKey: 'departmentName',
		header: 'Đơn vị',
	},
	{
		accessorKey: 'positionName',
		header: 'Chức vụ',
	},
	{
		accessorKey: 'dob',
		header: 'Ngày sinh',
		cell: ({ row }) => {
			const dob = row.original.dob;
			if (!dob) return '';
			const parts = dob.split('-');
			if (parts.length === 3) {
				return `${parts[2]}/${parts[1]}/${parts[0]}`;
			}
			return new Date(dob).toLocaleDateString('vi-VN');
		}
	},
	{
		accessorKey: 'genre',
		header: 'Giới tính',
		cell: ({ row }) => <span>{row.original.genre === false ? 'Nữ' : 'Nam'}</span>,
	},
	{
		accessorKey: 'phoneNumber',
		header: 'Số điện thoại',
	},
	{
		accessorKey: 'email',
		header: 'Email',
	},
	{
		accessorKey: 'isActive',
		header: () => <div className='text-center'>Trạng thái</div>,
		cell: ({ row }) => (
			<div className='flex justify-center'>
				<EmployeeStatusToggle row={row.original} />
			</div>
		),
	},
	{
		id: 'actions',
		header: () => <div className='text-center'>Reset mật khẩu</div>,
		cell: ({ row }) => (
			<div className='flex justify-center'>
				<EmployeeActions row={row.original} />
			</div>
		),
	},
];
