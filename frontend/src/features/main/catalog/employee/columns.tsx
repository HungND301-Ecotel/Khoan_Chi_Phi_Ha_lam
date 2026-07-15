import type { ColumnDef } from '@tanstack/react-table';

export type Employee = {
	id: number;
	fullName: string;
	positionId: number;
	positionName?: string;
	departmentId: string;
	departmentName?: string;
	userName?: string;
	province: string;
	district: string;
	ward: string;
	streetAddress: string;
	dob: string;
	cccd: string;
	avatar: string;
	email?: string;
	phoneNumber?: string;
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
		header: 'Phòng ban',
	},
	{
		accessorKey: 'positionName',
		header: 'Chức vụ',
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
		accessorKey: 'province',
		header: 'Tỉnh/Thành phố',
	},
	{
		accessorKey: 'district',
		header: 'Quận/Huyện',
	},
	{
		accessorKey: 'ward',
		header: 'Phường/Xã',
	},
	{
		accessorKey: 'streetAddress',
		header: 'Số nhà, đường',
	},
];
