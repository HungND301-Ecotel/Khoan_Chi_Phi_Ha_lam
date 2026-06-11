import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { Passport } from '@/features/main/catalog/parameter/passport/columns';
import { Strength } from '@/features/main/catalog/parameter/strength/columns';
import {
	ExpandSlideCostRow,
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

const deriveNormFromAmount = (amount: number, unitPrice: number) => {
	if (unitPrice > 0) {
		return amount / unitPrice;
	}

	return amount === 0 ? 0 : amount;
};

function buildGroupedExpandRows(
	materialCosts: SlideDetail['materialCost'],
): ExpandSlideCostRow[] {
	return materialCosts.flatMap((group) => {
		const groupItems = group.costs.map((cost) => ({
			rowType: 'material-item' as const,
			assignmentCodeId: group.assignmentCodeId,
			assignmentCode: group.assignmentCode,
			assignmentCodeName: group.assignmentCodeName,
			materialId: cost.materialId,
			materialCode: cost.materialCode,
			materialName: cost.materialName,
			unitOfMeasureName: cost.unitOfMeasureName,
			unitPrice: cost.cost,
			norm: deriveNormFromAmount(cost.amount, cost.cost),
			totalPrice: cost.amount,
		}));
		const groupTotal = groupItems.reduce(
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
				unitOfMeasureName: '',
				unitPrice: null,
				norm: '',
				totalPrice: groupTotal,
			},
			...groupItems,
		];
	});
}

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
				{ key: 'materialDetail', label: 'Thông số' },
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
	const [passport, setPassport] = useState<Passport>();
	const [strength, setStrength] = useState<Strength>();
	const [costs, setCosts] = useState<ExpandSlideCostRow[]>([]);

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
			setCosts(buildGroupedExpandRows(slide.result.materialCost ?? []));
		});
	}, [row]);

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
