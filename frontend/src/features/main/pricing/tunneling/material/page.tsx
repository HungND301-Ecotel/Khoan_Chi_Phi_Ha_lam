import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { ContractCode } from '@/features/main/catalog/contract-code/columns';
import { Insert } from '@/features/main/catalog/parameter/insert/columns';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Step } from '@/features/main/catalog/parameter/step/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { MaterialForm } from '@/features/main/pricing/tunneling/material/form';
import {
	ExpandMaterialAssignmentCost,
	ExpandMaterialDetail,
	MAIN_PRICING_MATERIAL_COLUMNS,
	MAIN_PRICING_MATERIAL_DETAIL_COLUMNS,
	MAIN_PRICING_MATERIAL_EXPAND_SUMMARY_COLUMNS,
} from '@/features/main/pricing/tunneling/material/columns';
import { MAIN_PRICING_SUPPORT_AND_DRILLING_COLUMNS } from '@/features/main/pricing/tunneling/material/support-and-drilling-columns';
import { SupportAndDrillingForm } from '@/features/main/pricing/tunneling/material/support-and-drilling-form';
import { api } from '@/lib/api';
import { useEffect, useMemo, useState } from 'react';
import { Material, SupportAndDrillingMaterial } from './type';

export function MainPricingMaterialPage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Material>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.MATERIAL.TUNNELING.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.MATERIAL.TUNNELING.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
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
				API.PRICING.MATERIAL.TUNNELING.IMPORT,
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

	const handleDeleteSupportAndDrilling = async ({
		data,
	}: ActionDialogProps<SupportAndDrillingMaterial>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExportSupportAndDrilling = async () => {
		try {
			const filename = await api.export(
				API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.EXPORT,
			);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImportSupportAndDrilling = async (
		file: File,
		data?: ActionDialogProps<SupportAndDrillingMaterial>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.IMPORT,
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
		<Tabs defaultValue='material' className='w-full'>
			<TabsList className='mb-4'>
				<TabsTrigger value='material'>
					Đơn giá và định mức vật liệu đào lò
				</TabsTrigger>
				<TabsTrigger value='support-and-drilling'>
					Đơn giá và định mức lò neo bê tông phun
				</TabsTrigger>
			</TabsList>

			<TabsContent value='material' className='mt-0'>
				<DataTable
					url={API.PRICING.MATERIAL.TUNNELING.LIST}
					columns={MAIN_PRICING_MATERIAL_COLUMNS}
					filters={[
						{ key: 'code', label: 'Mã định mức vật liệu' },
						{ key: 'processName', label: 'Công đoạn sản xuất' },
						{ key: 'materialDetail', label: 'Thông số' },
					]}
					onCreate={(props) => <MaterialForm {...props} />}
					onDuplicate={(props) => <MaterialForm {...props} isDuplicate />}
					onUpdate={(props) => <MaterialForm {...props} />}
					onDelete={handleDelete}
					onExport={handleExport}
					onImport={handleImport}
					onExpand={(props) => <MaterialDetailExpand {...props} />}
				/>
			</TabsContent>

			<TabsContent value='support-and-drilling' className='mt-0'>
				<DataTable
					url={API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.LIST}
					columns={MAIN_PRICING_SUPPORT_AND_DRILLING_COLUMNS}
					filters={[
						{ key: 'code', label: 'Mã đơn giá' },
						{ key: 'processName', label: 'Công đoạn sản xuất' },
						{ key: 'technologyName', label: 'Công nghệ' },
						{ key: 'passportName', label: 'Hộ chiếu' },
						{ key: 'hardnessName', label: 'Độ kiên cố than đá' },
					]}
					onCreate={(props) => <SupportAndDrillingForm {...props} />}
					onDuplicate={(props) => (
						<SupportAndDrillingForm {...props} isDuplicate />
					)}
					onUpdate={(props) => <SupportAndDrillingForm {...props} />}
					onDelete={handleDeleteSupportAndDrilling}
					onExport={handleExportSupportAndDrilling}
					onImport={handleImportSupportAndDrilling}
					onExpand={(props) => <SupportAndDrillingDetailExpand {...props} />}
				/>
			</TabsContent>
		</Tabs>
	);
}

function MaterialDetailExpand({ row }: ActionDialogProps<Material>) {
	const [detail, setDetail] = useState<ExpandMaterialDetail>();
	const [costs, setCosts] = useState<ExpandMaterialAssignmentCost[]>([]);

	useEffect(() => {
		if (!row) return;

		const promises = Promise.all([
			api.get<Passport>(API.CATALOG.PARAMETER.PASSPORT.DETAIL(row.passportId)),
			api.get<Strength>(API.CATALOG.PARAMETER.STRENGTH.DETAIL(row.hardnessId)),
			api.get<Insert>(API.CATALOG.PARAMETER.INSERT.DETAIL(row.insertItemId)),
			api.get<Step>(API.CATALOG.PARAMETER.STEP.DETAIL(row.supportStepId)),
			api.get<{
				costs: Array<{ assignmentCodeId: string; totalPrice: number }>;
			}>(API.PRICING.MATERIAL.TUNNELING.DETAIL(row.id)),
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST),
		]);

		promises.then(
			([passport, strength, insert, step, materialDetail, contractCodes]) => {
				setDetail({
					passport: passport.result,
					strength: strength.result,
					insert: insert.result,
					step: step.result,
				});

				const contractMap = new Map(
					contractCodes.result.data.map((item) => [item.id, item]),
				);

				setCosts(
					(materialDetail.result.costs ?? []).map((item) => {
						const contract = contractMap.get(item.assignmentCodeId);
						return {
							assignmentCodeId: item.assignmentCodeId,
							assignmentCode: contract?.code ?? '',
							assignmentCodeName: contract?.name ?? '',
							totalPrice: item.totalPrice,
						};
					}),
				);
			},
		);
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
				columns={MAIN_PRICING_MATERIAL_EXPAND_SUMMARY_COLUMNS}
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

function SupportAndDrillingDetailExpand({
	row,
}: ActionDialogProps<SupportAndDrillingMaterial>) {
	const [detail, setDetail] = useState<ExpandMaterialDetail>();
	const [costs, setCosts] = useState<ExpandMaterialAssignmentCost[]>([]);

	useEffect(() => {
		if (!row) return;

		const promises = Promise.all([
			api.get<Passport>(API.CATALOG.PARAMETER.PASSPORT.DETAIL(row.passportId)),
			api.get<Strength>(API.CATALOG.PARAMETER.STRENGTH.DETAIL(row.hardnessId)),
			api.get<{
				costs: Array<{ assignmentCodeId: string; totalPrice: number }>;
			}>(API.PRICING.MATERIAL.SUPPORT_AND_DRILLING.DETAIL(row.id)),
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST),
		]);

		promises.then(([passport, strength, materialDetail, contractCodes]) => {
			setDetail({
				passport: passport.result,
				strength: strength.result,
			});

			const contractMap = new Map(
				contractCodes.result.data.map((item) => [item.id, item]),
			);

			setCosts(
				(materialDetail.result.costs ?? []).map((item) => {
					const contract = contractMap.get(item.assignmentCodeId);
					return {
						assignmentCodeId: item.assignmentCodeId,
						assignmentCode: contract?.code ?? '',
						assignmentCodeName: contract?.name ?? '',
						totalPrice: item.totalPrice,
					};
				}),
			);
		});
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
				columns={MAIN_PRICING_MATERIAL_EXPAND_SUMMARY_COLUMNS}
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
