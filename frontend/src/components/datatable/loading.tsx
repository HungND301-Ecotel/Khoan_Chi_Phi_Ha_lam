import { Skeleton } from '@/components/ui/skeleton';
import { TableCell, TableRow } from '@/components/ui/table';
import { Table } from '@tanstack/react-table';

type DataTableLoadingProps<TData> = {
	table: Table<TData>;
	columns: number;
};

export function DataTableLoading<TData>({
	columns,
}: DataTableLoadingProps<TData>) {
	const rows = Array.from({ length: 5 });
	const cols = Array.from({ length: columns });

	return (
		<>
			{rows.map((_, index) => (
				<TableRow key={index} className='h-14'>
					{cols.map((_, index) => (
						<TableCell key={index}>
							<Skeleton className='h-4 w-full rounded-full' />
						</TableCell>
					))}
				</TableRow>
			))}
		</>
	);
}
