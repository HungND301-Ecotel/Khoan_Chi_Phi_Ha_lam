import {
	InputGroup,
	InputGroupAddon,
	InputGroupInput,
} from '@/components/ui/input-group';
import SearchIcon from '@mui/icons-material/Search';
import type { Table } from '@tanstack/react-table';

interface DataTableFilterProps<TData> {
	table: Table<TData>;
	filters?: { key: keyof TData; label: string }[];
}

export function Filter<TData>({ table }: DataTableFilterProps<TData>) {
	const filterValue = table.getState().globalFilter as string | undefined;

	return (
		<div className='flex flex-1 gap-4'>
			<InputGroup className='peer-focus:border-primary rounded-sm border-[#d4d5d7] shadow-none peer-focus:border-2 hover:border-black'>
				<InputGroupInput
					placeholder='Tìm kiếm'
					value={filterValue ?? ''}
					onChange={(event) => table.setGlobalFilter(event.target.value)}
					className='peer'
				/>
				<InputGroupAddon align={'inline-end'}>
					<SearchIcon className='size-4' />
				</InputGroupAddon>
			</InputGroup>
		</div>
	);
}
