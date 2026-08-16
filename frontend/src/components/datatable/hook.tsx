import { api, PaggingRequest } from '@/lib/api';
import {
	ColumnDef,
	ExpandedState,
	FilterFn,
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
	refreshVersion: number;
	refresh: () => Promise<void>;
	table: Table<TData>;
	totalCount: number;
};

export function useDataTable<TData>(
	columns: ColumnDef<TData>[],
	expandable = false,
	hasPagination = true,
	hasSort = true,
	url?: string,
	query?: PaggingRequest,
	items?: TData[],
	transformData?: (rows: TData[]) => TData[],
	customGetRowId?: (row: TData, index: number) => string,
	searchableColumnIds?: string[],
): UseDataTable<TData> {
	const [data, setData] = useState<TData[]>([]);
	const [loading, setLoading] = useState(false);
	const [refreshVersion, setRefreshVersion] = useState(0);
	const [sorting, setSorting] = useState<SortingState>([]);
	const [globalFilter, setGlobalFilter] = useState('');
	const [expanded, setExpanded] = useState<ExpandedState>({});
	const [pagination, setPagination] = useState<PaginationState>({
		pageIndex: 0,
		pageSize: 10,
	});
	const [totalCount, setTotalCount] = useState(0);

	const isServerPaginated =
		hasPagination && !!url && query?.ignorePagination !== true;

	useEffect(() => {
		if (!isServerPaginated) return;
		setPagination((prev) =>
			prev.pageIndex === 0 ? prev : { ...prev, pageIndex: 0 },
		);
	}, [url, query, globalFilter, pagination.pageSize, isServerPaginated]);

	const globalSearchFilter: FilterFn<TData> = (row, _columnId, filterValue) => {
		const normalizedFilter = String(filterValue ?? '')
			.trim()
			.toLowerCase();
		if (!normalizedFilter) return true;

		const normalizeValue = (value: unknown): string => {
			if (value === null || value === undefined) return '';
			if (
				typeof value === 'string' ||
				typeof value === 'number' ||
				typeof value === 'boolean'
			) {
				return String(value).toLowerCase();
			}
			if (Array.isArray(value)) {
				return value.map(normalizeValue).join(' ');
			}
			if (typeof value === 'object') {
				return Object.values(value as Record<string, unknown>)
					.map(normalizeValue)
					.join(' ');
			}
			return '';
		};

		const valuesToSearch =
			searchableColumnIds && searchableColumnIds.length > 0
				? searchableColumnIds.map((columnId) => row.getValue(columnId))
				: row.getVisibleCells().map((cell) => cell.getValue());

		return valuesToSearch.some((value) =>
			normalizeValue(value).includes(normalizedFilter),
		);
	};

	const refresh = useCallback(async () => {
		try {
			setLoading(true);
			if (!url) return;
			const mergedQuery: PaggingRequest = {
				...query,
				...(isServerPaginated
					? {
							pageIndex: pagination.pageIndex + 1, // BE dùng pageIndex 1-based
							pageSize: pagination.pageSize,
						}
					: {}),
				...(globalFilter ? { search: globalFilter } : {}),
			};
			const response = await api.pagging<TData>(url, mergedQuery);
			const result = response.result.data || [];
			setData(transformData ? transformData(result) : result);
			setTotalCount(
				isServerPaginated
					? (response.result.totalCount ?? result.length)
					: result.length,
			);
			setRefreshVersion((prev) => prev + 1);
		} finally {
			setLoading(false);
			// bỏ setExpanded({}) để không tự đóng expand
		}
	}, [
		url,
		query,
		transformData,
		globalFilter,
		isServerPaginated,
		pagination.pageIndex,
		pagination.pageSize,
	]);

	useEffect(() => {
		refresh();
	}, [refresh]);

	const table = useReactTable({
		data: items ? (transformData ? transformData(items) : items) : data,
		columns,
		getCoreRowModel: getCoreRowModel(),
		getRowId:
			customGetRowId ??
			((originalRow, index) => {
				if (
					typeof originalRow === 'object' &&
					originalRow !== null &&
					'id' in originalRow
				) {
					const rowId = (originalRow as { id?: string | number }).id;
					if (rowId !== undefined && rowId !== null) return String(rowId);
				}
				return String(index);
			}),
		...(hasPagination && {
			getPaginationRowModel: getPaginationRowModel(),
			onPaginationChange: setPagination,
			...(isServerPaginated && {
				manualPagination: true,
				pageCount: Math.max(1, Math.ceil(totalCount / pagination.pageSize)),
			}),
		}),
		...(hasSort && {
			onSortingChange: setSorting,
			getSortedRowModel: getSortedRowModel(),
			enableSortingRemoval: true,
		}),
		getFilteredRowModel: getFilteredRowModel(),
		globalFilterFn: globalSearchFilter,
		onGlobalFilterChange: setGlobalFilter,
		getRowCanExpand: () => expandable,
		getExpandedRowModel: getExpandedRowModel(),
		onExpandedChange: setExpanded,
		autoResetPageIndex: false,
		autoResetExpanded: false,
		state: {
			sorting,
			globalFilter,
			expanded,
			...(hasPagination ? { pagination } : {}),
		},
	});

	return {
		data,
		loading,
		refreshVersion,
		refresh,
		table,
		totalCount: isServerPaginated
			? totalCount
			: table.getFilteredRowModel().rows.length,
	};
}
