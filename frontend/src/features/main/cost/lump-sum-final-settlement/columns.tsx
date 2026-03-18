import { formatNumber } from '@/lib/utils';
import { ColumnDef } from '@tanstack/react-table';
import { LumpSumFinalSettlement } from './types';

export const LUMP_SUM_FINAL_SETTLEMENT_COLUMNS: ColumnDef<LumpSumFinalSettlement>[] =
	[
		{
			id: 'stt',
			header: () => <div className='text-center font-bold'>STT</div>,
			cell: ({ row }) => (
				<div className='text-center font-semibold'>{row.index + 1}</div>
			),
			size: 50,
		},
		{
			accessorKey: 'tenPhim',
			header: () => <div className='text-center font-bold'>SẢN PHẨM</div>,
			cell: ({ row }) => (
				<div className='whitespace-normal'>{row.original.productName}</div>
			),
			size: 300,
		},
		{
			accessorKey: 'dvt',
			header: () => <div className='text-center font-bold'>ĐVT</div>,
			cell: ({ row }) => (
				<div className='text-center'>{row.original.unitOfMeasureName}</div>
			),
			size: 50,
		},
		{
			accessorKey: 'khoiLuong',
			header: () => <div className='text-center font-bold'>KH</div>,
			cell: ({ row }) => (
				<div className='text-left'>
					{formatNumber(row.original.plannedQuantity ?? 0, {
						maximumFractionDigits: 3,
					})}
				</div>
			),
			size: 100,
		},
		{
			accessorKey: 'thanhTien',
			header: () => <div className='text-center font-bold'>TH</div>,
			cell: ({ row }) => (
				<div className='text-left'>
					{formatNumber(row.original.actualQuantity ?? 0, {
						maximumFractionDigits: 3,
					})}
				</div>
			),
			size: 100,
		},
		{
			id: 'vatLieu',
			header: () => (
				<div className='border-l-2 border-gray-300 text-center font-bold'>
					VẬT LIỆU
				</div>
			),
			columns: [
				{
					accessorKey: 'donGiaVatLieu',
					header: () => (
						<div className='text-center text-xs font-bold'>ĐƠN GIÁ</div>
					),
					cell: ({ row }) => (
						<div className='text-left'>
							{formatNumber(Math.round(row.original.materials?.unitPrice ?? 0))}
						</div>
					),
					size: 120,
				},
				{
					accessorKey: 'thanhTienVatLieu',
					header: () => (
						<div className='text-center text-xs font-bold'>THÀNH TIỀN</div>
					),
					cell: ({ row }) => (
						<div className='text-left font-bold'>
							{formatNumber(
								Math.round(row.original.materials?.totalAmount ?? 0),
							)}
						</div>
					),
					size: 120,
				},
			],
		},
		{
			id: 'suaChua',
			header: () => (
				<div className='border-l-2 border-gray-300 text-center font-bold'>
					SỬA CHỮA THƯỜNG XUYÊN
				</div>
			),
			columns: [
				{
					accessorKey: 'donGiaSuaChua',
					header: () => (
						<div className='text-center text-xs font-bold'>ĐƠN GIÁ</div>
					),
					cell: ({ row }) => (
						<div className='text-left'>
							{formatNumber(Math.round(row.original.maintains?.unitPrice ?? 0))}
						</div>
					),
					size: 120,
				},
				{
					accessorKey: 'thanhTienSuaChua',
					header: () => (
						<div className='text-center text-xs font-bold'>THÀNH TIỀN</div>
					),
					cell: ({ row }) => (
						<div className='text-left font-bold'>
							{formatNumber(
								Math.round(row.original.maintains?.totalAmount ?? 0),
							)}
						</div>
					),
					size: 120,
				},
			],
		},
		{
			id: 'dongLuc',
			header: () => (
				<div className='border-l-2 border-gray-300 text-center font-bold'>
					ĐỘNG LỰC (ĐIỆN NĂNG)
				</div>
			),
			columns: [
				{
					accessorKey: 'donGiaDongLuc',
					header: () => (
						<div className='text-center text-xs font-bold'>ĐƠN GIÁ</div>
					),
					cell: ({ row }) => (
						<div className='text-left'>
							{formatNumber(
								Math.round(row.original.electricities?.unitPrice ?? 0),
							)}
						</div>
					),
					size: 120,
				},
				{
					accessorKey: 'thanhTienDongLuc',
					header: () => (
						<div className='text-center text-xs font-bold'>THÀNH TIỀN</div>
					),
					cell: ({ row }) => (
						<div className='text-left font-bold'>
							{formatNumber(
								Math.round(row.original.electricities?.totalAmount ?? 0),
							)}
						</div>
					),
					size: 120,
				},
			],
		},
		{
			id: 'tong',
			header: () => (
				<div className='border-l-2 border-gray-300 text-center font-bold'>
					TỔNG
				</div>
			),
			columns: [
				{
					accessorKey: 'tongThanhTien',
					header: () => (
						<div className='text-center text-xs font-bold'>THÀNH TIỀN</div>
					),
					cell: ({ row }) => (
						<div className='text-left font-semibold'>
							{formatNumber(Math.round(row.original.totalAmount ?? 0))}
						</div>
					),
					size: 150,
				},
			],
		},
	];
