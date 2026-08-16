import { ColumnDef } from '@tanstack/react-table';
import {
	getProcessGroupType,
	ProcessGroupType,
} from '@/constants/process-group';

export type ProcessGroup = {
	id: string;
	fixedKeyId?: string | null;
	code: string;
	name: string;
	fixedKeyType?: ProcessGroupType;
	type?: ProcessGroupType;
};

export function normalizeProcessGroup(group: ProcessGroup): ProcessGroup {
	const code = (group.code || '').trim().toUpperCase();
	let fixedKeyType = group.fixedKeyType;
	if (fixedKeyType === undefined || fixedKeyType === null) {
		const rawType = group.type as unknown as number;
		if (code === 'VTL' || rawType === 4 || rawType === 12) {
			fixedKeyType = ProcessGroupType.VTL;
		} else if (code === 'VTCG' || rawType === 5 || rawType === 13) {
			fixedKeyType = ProcessGroupType.VTCG;
		} else if (code === 'DL' || rawType === 1) {
			fixedKeyType = ProcessGroupType.DL;
		} else if (code === 'LC' || rawType === 2) {
			fixedKeyType = ProcessGroupType.LC;
		} else if (code === 'XL' || rawType === 3) {
			fixedKeyType = ProcessGroupType.XL;
		} else {
			fixedKeyType = group.type ?? getProcessGroupType(group.code);
		}
	}
	return {
		...group,
		fixedKeyType,
	};
}

export const CATALOG_PROCESS_GROUP_COLUMNS: ColumnDef<ProcessGroup>[] = [
	{
		accessorKey: 'code',
		header: 'Mã nhóm công đoạn sản xuất',
	},
	{
		accessorKey: 'name',
		header: 'Tên nhóm công đoạn sản xuất',
	},
];
