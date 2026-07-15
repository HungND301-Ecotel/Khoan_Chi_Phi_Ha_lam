import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { Insert } from '@/features/main/catalog/parameter/insert/columns';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Step } from '@/features/main/catalog/parameter/step/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { MaterialForm } from '@/features/main/pricing/trimming/material/form';
import {
	ExpandMaterialCostRow,
	ExpandMaterialDetail,
	MAIN_PRICING_MATERIAL_COLUMNS,
	MAIN_PRICING_MATERIAL_DETAIL_COLUMNS,
	MAIN_PRICING_MATERIAL_EXPAND_COLUMNS,
} from '@/features/main/pricing/trimming/material/columns';
import { api } from '@/lib/api';
import { usePermission } from '@/hooks/use-permission';
import { useEffect, useMemo, useState } from 'react';
import { Material, MaterialDetail, MaterialDetailCost } from './type';

function buildGroupedExpandRows(
	costItems: MaterialDetailCost[],
	otherMaterialValue?: number | null,
): ExpandMaterialCostRow[] {
	const groupedRows = new Map<
		string,
		{
			assignmentCodeId: string;
			assignmentCode: string;
			assignmentCodeName: string;
			items: ExpandMaterialCostRow[];
		}
	>();

	costItems.forEach((item) => {
		const groupKey =
			item.assignmentCodeId ||
			`${item.assignmentCode}-${item.assignmentCodeName}`;

		if (!groupedRows.has(groupKey)) {
			groupedRows.set(groupKey, {
				assignmentCodeId: item.assignmentCodeId || groupKey,
				assignmentCode: item.assignmentCode,
				assignmentCodeName: item.assignmentCodeName,
				items: [],
			});
		}

		groupedRows.get(groupKey)?.items.push({
			rowType: 'material-item',
			assignmentCodeId: item.assignmentCodeId,
			assignmentCode: item.assignmentCode,
			assignmentCodeName: item.assignmentCodeName,
			materialId: item.materialId,
			materialCode: item.materialCode,
			materialName: item.materialName,
			unitPrice: item.unitPrice,
			norm: item.norm,
			totalPrice: item.totalPrice,
		});
	});

	const rows = Array.from(groupedRows.values()).flatMap((group) => {
		const groupTotal = group.items.reduce(
			(sum, item) => sum + item.totalPrice,
			0,
		);

		return [
			{
				rowType: 'group-summary' as const,
				assignmentCodeId: group.assignmentCodeId,
				assignmentCode: group.assignmentCode,
				assignmentCodeName: group.assignmentCodeName,
				materialId: `group-${group.assignmentCodeId}`,
				materialCode: '',
				materialName: '',
				unitPrice: null,
				norm: '',
				totalPrice: groupTotal,
			},
			...group.items,
		];
	});

	if (otherMaterialValue === undefined || otherMaterialValue === null) {
		return rows;
	}

	const baseTotal = costItems.reduce((sum, item) => sum + item.totalPrice, 0);
	const otherMaterialTotal =
		(baseTotal * (Number(otherMaterialValue) || 0)) / 100;

	return [
		...rows,
		{
			rowType: 'group-summary',
			assignmentCodeId: 'VTK',
			assignmentCode: 'VTK',
			assignmentCodeName: 'Vật tư khác',
			materialId: 'group-VTK',
			materialCode: '',
			materialName: '',
			unitPrice: null,
			norm: '',
			totalPrice: otherMaterialTotal,
		},
	];
}

export function MainPricingTrimmingMaterialPage() {
	const { hasPermission } = usePermission();
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Material>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.MATERIAL.TRIMMING.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.MATERIAL.TRIMMING.EXPORT);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Material>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MATERIAL.TRIMMING.IMPORT,
				file,
			);
			if (typeof result === 'string') {
				popup.success(`Đã tải về danh sách lỗi: ${result}`);
			} else {
				popup.success(`Nhập dữ liệu thành công`);
				await data?.refresh();
			}
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<DataTable
			url={API.PRICING.MATERIAL.TRIMMING.LIST}
			columns={MAIN_PRICING_MATERIAL_COLUMNS}
			filters={[
				{ key: 'code', label: 'Mã định mức vật liệu' },
				{ key: 'processName', label: 'Công đoạn sản xuất' },
				{ key: 'materialDetail', label: 'Thông số' },
			]}
			onCreate={hasPermission('pricing.trimmingmaterialunitpricing.create') ? (props) => <MaterialForm {...props} /> : undefined}
			onDuplicate={hasPermission('pricing.trimmingmaterialunitpricing.create') ? (props) => <MaterialForm {...props} isDuplicate /> : undefined}
			onUpdate={hasPermission('pricing.trimmingmaterialunitpricing.update') ? (props) => <MaterialForm {...props} /> : undefined}
			onDelete={hasPermission('pricing.trimmingmaterialunitpricing.delete') ? handleDelete : undefined}
			onExport={hasPermission('pricing.trimmingmaterialunitpricing.export') ? handleExport : undefined}
			onImport={hasPermission('pricing.trimmingmaterialunitpricing.import') ? handleImport : undefined}
			onExpand={(props) => <MaterialDetailExpand {...props} />}
		/>
	);
}

function MaterialDetailExpand({ row }: ActionDialogProps<Material>) {
	const [detail, setDetail] = useState<ExpandMaterialDetail>();
	const [costs, setCosts] = useState<ExpandMaterialCostRow[]>([]);

	useEffect(() => {
		if (!row) return;
		let cancelled = false;

		const promises = Promise.all([
			api.get<Passport>(API.CATALOG.PARAMETER.PASSPORT.DETAIL(row.passportId)),
			api.get<Strength>(API.CATALOG.PARAMETER.STRENGTH.DETAIL(row.hardnessId)),
			api.get<Insert>(API.CATALOG.PARAMETER.INSERT.DETAIL(row.insertItemId)),
			api.get<Step>(API.CATALOG.PARAMETER.STEP.DETAIL(row.supportStepId)),
			api.get<MaterialDetail>(API.PRICING.MATERIAL.TRIMMING.DETAIL(row.id)),
		]);

		promises
			.then(([passport, strength, insert, step, materialDetail]) => {
				if (cancelled) return;

				setDetail({
					passport: passport.result,
					strength: strength.result,
					insert: insert.result,
					step: step.result,
				});
				setCosts(
					buildGroupedExpandRows(
						materialDetail.result.costs ?? [],
						materialDetail.result.otherMaterialValue,
					),
				);
			})
			.catch(() => {
				if (cancelled) return;

				setDetail(undefined);
				setCosts([]);
			});

		return () => {
			cancelled = true;
		};
	}, [row]);

	const detailItems = useMemo(() => [detail ?? {}], [detail]);

	return (
		<div className='mx-32 flex flex-col gap-4'>
			<DataTable
				columns={MAIN_PRICING_MATERIAL_DETAIL_COLUMNS}
				items={detailItems}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				hasIndex={false}
				compact={true}
			/>

			<div className='bg-border h-0.5' />

			<DataTable
				columns={MAIN_PRICING_MATERIAL_EXPAND_COLUMNS}
				items={costs}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				hasIndex={false}
				compact={true}
			/>
		</div>
	);
}
