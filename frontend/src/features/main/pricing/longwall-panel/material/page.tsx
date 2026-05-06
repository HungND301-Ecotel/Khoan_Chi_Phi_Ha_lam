import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { ContractCode } from '@/features/main/catalog/contract-code/columns';
import { Seamface } from '@/features/main/catalog/parameter/seamface/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { Technology } from '@/features/main/catalog/parameter/technology/columns';
import { Power } from '@/features/main/catalog/parameter/power/columns';
import { LongwallMaterialForm } from '@/features/main/pricing/longwall-panel/material/form';
import {
	ExpandLongwallMaterialAssignmentCost,
	ExpandLongwallMaterialDetail,
	LONGWALL_MATERIAL_DETAIL_CGH_COLUMNS,
	LONGWALL_MATERIAL_DETAIL_NON_CGH_COLUMNS,
	LONGWALL_MATERIAL_EXPAND_SUMMARY_COLUMNS,
	LONGWALL_MATERIAL_COLUMNS,
	LongwallMaterial,
} from '@/features/main/pricing/longwall-panel/material/columns';
import { api } from '@/lib/api';
import { useEffect, useMemo, useState } from 'react';

type LongwallMaterialDetailResponse = {
	id: string;
	code: string;
	technologyId?: string;
	powerId?: string;
	hardnessId?: string;
	seamFaceId?: string;
	longwallParameters?: {
		id: string;
		llc: string;
		lkc: number;
		mk: number;
	};
	cuttingThickness?: {
		id: string;
		value?: string;
		from?: string;
		to?: string;
	};
	costs: Array<{
		assignmentCodeId: string;
		totalPrice: number;
	}>;
};

export function LongwallPanelMaterialPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<LongwallMaterial>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.MATERIAL.LONGWALL_PANEL.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.PRICING.MATERIAL.LONGWALL_PANEL.EXPORT,
			);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<LongwallMaterial>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MATERIAL.LONGWALL_PANEL.IMPORT,
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
			url={API.PRICING.MATERIAL.LONGWALL_PANEL.LIST}
			columns={LONGWALL_MATERIAL_COLUMNS}
			filters={[
				{ key: 'code', label: 'Mã định mức vật liệu' },
				{ key: 'technologyName', label: 'Công nghệ khai thác' },
			]}
			onCreate={(props) => <LongwallMaterialForm {...props} />}
			onDuplicate={(props) => <LongwallMaterialForm {...props} isDuplicate />}
			onUpdate={(props) => <LongwallMaterialForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
			onExpand={(props) => <LongwallMaterialExpand {...props} />}
		/>
	);
}

function LongwallMaterialExpand({ row }: ActionDialogProps<LongwallMaterial>) {
	const [detail, setDetail] = useState<ExpandLongwallMaterialDetail>();
	const [costs, setCosts] = useState<ExpandLongwallMaterialAssignmentCost[]>(
		[],
	);

	useEffect(() => {
		if (!row) return;

		const load = async () => {
			const [detailRes, contractsRes] = await Promise.all([
				api.get<LongwallMaterialDetailResponse>(
					API.PRICING.MATERIAL.LONGWALL_PANEL.DETAIL(row.id),
				),
				api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST),
			]);

			const materialDetail = detailRes.result;

			const [technologyRes, powerRes, hardnessRes, seamFaceRes] =
				await Promise.all([
					materialDetail.technologyId
						? api.get<Technology>(
								API.CATALOG.PARAMETER.TECHNOLOGY.DETAIL(
									materialDetail.technologyId,
								),
							)
						: Promise.resolve(undefined),
					materialDetail.powerId
						? api.get<Power>(
								API.CATALOG.PARAMETER.POWER.DETAIL(materialDetail.powerId),
							)
						: Promise.resolve(undefined),
					materialDetail.hardnessId
						? api.get<Strength>(
								API.CATALOG.PARAMETER.STRENGTH.DETAIL(
									materialDetail.hardnessId,
								),
							)
						: Promise.resolve(undefined),
					materialDetail.seamFaceId
						? api.get<Seamface>(
								API.CATALOG.PARAMETER.SEAMFACE.DETAIL(
									materialDetail.seamFaceId,
								),
							)
						: Promise.resolve(undefined),
				]);

			const longwallParametersValue = materialDetail.longwallParameters
				? `Llc ${materialDetail.longwallParameters.llc}; Lkc ${materialDetail.longwallParameters.lkc}; Mk ${materialDetail.longwallParameters.mk}`
				: '';

			const cuttingThicknessValue =
				materialDetail.cuttingThickness?.value ??
				(materialDetail.cuttingThickness?.from &&
				materialDetail.cuttingThickness?.to
					? `${materialDetail.cuttingThickness.from} - ${materialDetail.cuttingThickness.to}`
					: (row.cuttingThickness?.value ?? ''));

			const isCGH = !!row.isLongwallMaterialUnitPriceCGH;

			setDetail({
				technologyName:
					technologyRes?.result?.value ?? row.technologyName ?? '',
				powerOrHardnessValue: isCGH
					? (powerRes?.result?.value ?? row.powerName ?? '')
					: (hardnessRes?.result?.value ?? row.hardnessName ?? ''),
				longwallParametersValue,
				cuttingThicknessValue,
				seamFaceValue: seamFaceRes?.result?.value ?? row.seamFaceName ?? '',
			});

			const contractMap = new Map(
				contractsRes.result.data.map((item) => [item.id, item]),
			);

			setCosts(
				(materialDetail.costs ?? []).map((item) => {
					const contract = contractMap.get(item.assignmentCodeId);
					return {
						assignmentCodeId: item.assignmentCodeId,
						assignmentCode: contract?.code ?? '',
						assignmentCodeName: contract?.name ?? '',
						totalPrice: item.totalPrice,
					};
				}),
			);
		};

		load();
	}, [row]);

	const detailColumns = useMemo(
		() =>
			row?.isLongwallMaterialUnitPriceCGH
				? LONGWALL_MATERIAL_DETAIL_CGH_COLUMNS
				: LONGWALL_MATERIAL_DETAIL_NON_CGH_COLUMNS,
		[row?.isLongwallMaterialUnitPriceCGH],
	);

	const detailItems = useMemo(() => [detail ?? {}], [detail]);

	return (
		<div className='mx-32 flex flex-col gap-4'>
			<DataTable
				columns={detailColumns}
				items={detailItems}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				hasIndex={false}
				compact={true}
			/>

			<div className='bg-border h-0.5' />

			<DataTable
				columns={LONGWALL_MATERIAL_EXPAND_SUMMARY_COLUMNS}
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
