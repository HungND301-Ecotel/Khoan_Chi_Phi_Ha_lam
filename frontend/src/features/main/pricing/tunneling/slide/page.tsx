import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import {
	FlatSlideCost,
	MAIN_PRICING_DETAIL_EXPAND_COLUMNS,
	MAIN_PRICING_SLIDE_COLUMNS,
	MAIN_PRICING_SLIDE_EXPAND_COLUMNS,
	Slide,
} from '@/features/main/pricing/tunneling/slide/columns';
import {
	SlideDetail,
	SlideForm,
} from '@/features/main/pricing/tunneling/slide/form';
import { api } from '@/lib/api';
import { useEffect, useState } from 'react';

export function MainPricingSlidePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({ data }: ActionDialogProps<Slide>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original.id);

			await api.delete(API.PRICING.SLIDE.DELETES, rows);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}.`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(API.PRICING.SLIDE.EXPORT);
			popup.success(`Đã xuất file ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<Slide>['data'],
	) => {
		try {
			const result = await api.import(API.PRICING.SLIDE.IMPORT, file);
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
			url={API.PRICING.SLIDE.LIST}
			columns={MAIN_PRICING_SLIDE_COLUMNS}
			getRowId={(row) => row.id}
			filters={[
				{ key: 'code', label: 'Mã định mức máng trượt' },
				{ key: 'processGroupName', label: 'Nhóm công đoạn sản xuất' },
			]}
			onCreate={(props) => <SlideForm {...props} />}
			onDuplicate={(props) => <SlideForm {...props} isDuplicate />}
			onUpdate={(props) => <SlideForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
			onExpand={(props) => <SlideDetailExpand {...props} />}
		/>
	);
}

function SlideDetailExpand({ row }: ActionDialogProps<Slide>) {
	const [slide, setSlide] = useState<SlideDetail>();
	const [passport, setPassport] = useState<Passport>();
	const [strength, setStrength] = useState<Strength>();

	useEffect(() => {
		if (!row) return;
		const promises = Promise.all([
			api.get<Passport>(API.CATALOG.PARAMETER.PASSPORT.DETAIL(row.passportId)),
			api.get<Strength>(API.CATALOG.PARAMETER.STRENGTH.DETAIL(row.hardnessId)),
			api.get<SlideDetail>(API.PRICING.SLIDE.DETAIL(row.id)),
		]);

		promises.then(([passport, strength, slide]) => {
			setPassport(passport.result);
			setStrength(strength.result);
			setSlide(slide.result);
		});
	}, [row]);

	function flattenData(): FlatSlideCost[] {
		const data = slide?.materialCost || [];
		const result: FlatSlideCost[] = [];

		data.forEach((item, index) => {
			const groupTotal = item.costs.reduce((sum, c) => sum + c.amount, 0);
			result.push({
				isGroupRow: true,
				rowIndex: index + 1,
				assignmentCodeId: item.assignmentCodeId,
				assignmentCode: item.assignmentCode,
				assignmentCodeName: item.assignmentCodeName,
				totalPrice: groupTotal,
			});

			item.costs.forEach((cost) => {
				result.push({
					isGroupRow: false,
					rowIndex: index + 1,
					assignmentCodeId: item.assignmentCodeId,
					assignmentCode: item.assignmentCode,
					assignmentCodeName: item.assignmentCodeName,
					materialId: cost.materialId,
					materialCode: cost.materialCode,
					materialName: cost.materialName,
					unitOfMeasureName: cost.unitOfMeasureName,
					cost: cost.cost,
					quantity: cost.amount,
					totalPrice: cost.amount,
				});
			});
		});

		return result;
	}

	return (
		<div className='mx-32 flex flex-col gap-4'>
			<DataTable
				columns={MAIN_PRICING_DETAIL_EXPAND_COLUMNS}
				items={[{ passport, strength }]}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				hasIndex={false}
				compact={true}
			/>

			<div className='bg-border h-0.5' />

			<DataTable
				columns={MAIN_PRICING_SLIDE_EXPAND_COLUMNS}
				items={flattenData()}
				hasActions={false}
				hasPagination={false}
				hasSort={false}
				hasIndex={false}
				compact={true}
			/>
		</div>
	);
}
