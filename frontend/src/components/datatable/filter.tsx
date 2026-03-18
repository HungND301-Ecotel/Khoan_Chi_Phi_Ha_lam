import {
	InputGroup,
	InputGroupAddon,
	InputGroupInput,
} from '@/components/ui/input-group';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
} from '@/components/ui/select';
import FilterListIcon from '@mui/icons-material/FilterList';
import SearchIcon from '@mui/icons-material/Search';
import type { Table } from '@tanstack/react-table';
import { useState } from 'react';

interface DataTableFilterProps<TData> {
	table: Table<TData>;
	filters?: { key: keyof TData; label: string }[];
}

export function Filter<TData>({ table, filters }: DataTableFilterProps<TData>) {
	const [columnKey, setColumnKey] = useState<keyof TData>(
		filters?.[0].key as keyof TData,
	);

	const filterValue = table.getColumn(String(columnKey))?.getFilterValue() as
		| string
		| undefined;

	return (
		<div className='flex flex-1 gap-4'>
			{filters && (
				<Select
					value={String(columnKey)}
					onValueChange={(value) => setColumnKey(value as keyof TData)}
				>
					<SelectTrigger className='hover:bg-muted flex h-10! w-fit cursor-pointer bg-white shadow-[0px_3px_1px_-2px_rgba(0,0,0,0.2),0px_2px_2px_0px_rgba(0,0,0,0.14),0px_1px_5px_0px_rgba(0,0,0,0.12)] [&_.lucide-chevron-down]:hidden'>
						<div className='flex flex-nowrap items-center gap-3 px-2'>
							<FilterListIcon fontSize='small' />
							<span className='hidden font-medium lg:block'>Lọc</span>
						</div>
					</SelectTrigger>
					<SelectContent>
						{filters?.map((filter) => (
							<SelectItem key={String(filter.key)} value={String(filter.key)}>
								{filter.label}
							</SelectItem>
						))}
					</SelectContent>
				</Select>
			)}

			<InputGroup className='peer-focus:border-primary rounded-sm border-[#d4d5d7] shadow-none peer-focus:border-2 hover:border-black'>
				<InputGroupInput
					placeholder='Tìm kiếm'
					value={filterValue ?? ''}
					onChange={(event) =>
						table
							.getColumn(String(columnKey))
							?.setFilterValue(event.target.value)
					}
					className='peer'
				/>
				<InputGroupAddon align={'inline-end'}>
					<SearchIcon className='size-4' />
				</InputGroupAddon>
			</InputGroup>
		</div>
	);
}
