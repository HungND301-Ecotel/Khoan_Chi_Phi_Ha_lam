import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { MaterialForm } from '@/features/main/pricing/tunneling/material/form';
import { MAIN_PRICING_MATERIAL_COLUMNS } from '@/features/main/pricing/tunneling/material/columns';
import { MAIN_PRICING_SUPPORT_AND_DRILLING_COLUMNS } from '@/features/main/pricing/tunneling/material/support-and-drilling-columns';
import { SupportAndDrillingForm } from '@/features/main/pricing/tunneling/material/support-and-drilling-form';
import { api } from '@/lib/api';
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
					Đơn giá và định mức vật liệu đào/xén lò
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
					]}
					onCreate={(props) => <MaterialForm {...props} />}
					onUpdate={(props) => <MaterialForm {...props} />}
					onDelete={handleDelete}
					onExport={handleExport}
					onImport={handleImport}
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
					]}
					onCreate={(props) => <SupportAndDrillingForm {...props} />}
					onUpdate={(props) => <SupportAndDrillingForm {...props} />}
					onDelete={handleDeleteSupportAndDrilling}
					onExport={handleExportSupportAndDrilling}
					onImport={handleImportSupportAndDrilling}
				/>
			</TabsContent>
		</Tabs>
	);
}
