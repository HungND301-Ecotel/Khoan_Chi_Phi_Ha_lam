import {
	Empty,
	EmptyDescription,
	EmptyHeader,
	EmptyMedia,
	EmptyTitle,
} from '@/components/ui/empty';
import { TableCell, TableRow } from '@/components/ui/table';
import { Table } from '@tanstack/react-table';
import { InboxIcon } from 'lucide-react';

type DataTableEmptyProps<TData> = {
	table: Table<TData>;
	span: number;
};

export function DataTableEmpty<TData>({ span }: DataTableEmptyProps<TData>) {
	return (
		<TableRow>
			<TableCell colSpan={span} className='h-24 text-center'>
				<Empty>
					<EmptyHeader>
						<EmptyMedia variant={'icon'} className='border'>
							<InboxIcon />
						</EmptyMedia>
						<EmptyTitle>Chưa có dữ liệu</EmptyTitle>
						<EmptyDescription>
							Chưa có dữ liệu Hiện tại chưa có bản ghi nào được tạo
						</EmptyDescription>
					</EmptyHeader>
				</Empty>
			</TableCell>
		</TableRow>
	);
}
