import { api, PaggingRequest } from '@/lib/api';
import {
	ColumnDef,
	ColumnFiltersState,
	ExpandedState,
	getCoreRowModel,
	getExpandedRowModel,
	getFilteredRowModel,
	getPaginationRowModel,
	getSortedRowModel,
	PaginationState,
	SortingState,
	Table,
	useReactTable,
} from '@tanstack/react-table';
import { useCallback, useEffect, useState } from 'react';

export type UseDataTable<TData> = {
	data: TData[];
	loading: boolean;
	refresh: () => Promise<void>;
	table: Table<TData>;
};

export function useDataTable<TData>(
	columns: ColumnDef<TData>[],
	expandable = false,
	hasPagination = true,
	hasSort = true,
	url?: string,
	query?: PaggingRequest,
	items?: TData[],
): UseDataTable<TData> {
	const [data, setData] = useState<TData[]>([]);
	const [loading, setLoading] = useState(false);
	const [sorting, setSorting] = useState<SortingState>([]);
	const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
	const [expanded, setExpanded] = useState<ExpandedState>({});
	const [pagination, setPagination] = useState<PaginationState>({
		pageIndex: 0,
		pageSize: 10,
	});

	const refresh = useCallback(async () => {
		try {
			setLoading(true);
			if (!url) return;
			const response = await api.pagging<TData>(url, query);
			const result = response.result.data || [];
			setData(result);
		} finally {
			setLoading(false);
			// bỏ setExpanded({}) để không tự đóng expand
		}
	}, [url, query]);

	useEffect(() => {
		refresh();
	}, [refresh]);

	const table = useReactTable({
		data: items ? items : data,
		columns,
		getCoreRowModel: getCoreRowModel(),
		getRowId: (originalRow, index) => {
			if (
				typeof originalRow === 'object' &&
				originalRow !== null &&
				'id' in originalRow
			) {
				const rowId = (originalRow as { id?: string | number }).id;
				if (rowId !== undefined && rowId !== null) return String(rowId);
			}
			return String(index);
		},
		...(hasPagination && {
			getPaginationRowModel: getPaginationRowModel(),
			onPaginationChange: setPagination,
		}),
		...(hasSort && {
			onSortingChange: setSorting,
			getSortedRowModel: getSortedRowModel(),
			enableSortingRemoval: true,
		}),
		getFilteredRowModel: getFilteredRowModel(),
		onColumnFiltersChange: setColumnFilters,
		getRowCanExpand: () => expandable,
		getExpandedRowModel: getExpandedRowModel(),
		onExpandedChange: setExpanded,
		autoResetPageIndex: false,
		autoResetExpanded: false,
		state: {
			sorting,
			columnFilters,
			expanded,
			...(hasPagination ? { pagination } : {}),
		},
	});

	return {
		data,
		loading,
		refresh,
		table,
	};
}
