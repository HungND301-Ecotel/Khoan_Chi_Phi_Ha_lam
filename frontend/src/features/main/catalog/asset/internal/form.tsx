import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormArray } from '@/components/form/form-array';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormInput } from '@/components/form/form-input';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import {
	ASSET_INTERNAL_FORM_DEFAULT,
	AssetInternalFormSchema,
	assetInternalFormSchema,
} from '@/features/main/catalog/asset/internal/schema';
import { Asset } from '@/features/main/catalog/asset/types';
import { ContractCode } from '@/features/main/catalog/contract-code/columns';
import { Unit } from '@/features/main/catalog/unit/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';

export type AssetInternalDetail = {
	id: string;
	code: string;
	name: string;
	assigmentCodeId: string;
	assignmentCode: string;
	unitOfMeasureId: string;
	unitOfMeasureName: string;
	usageTime: number;
	materialType: 1;
	costs: Array<{
		startMonth: string;
		endMonth: string;
		costType: number;
		amount: number;
		actualAmount: number;
	}>;
};

export function AssetInternalForm({ data, row }: ActionDialogProps<Asset>) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [contracts, setContracts] = useState<ContractCode[]>([]);
	const [units, setUnits] = useState<Unit[]>([]);

	const form = useForm<AssetInternalFormSchema>({
		resolver: zodResolver(assetInternalFormSchema),
		mode: 'onSubmit',
		defaultValues: ASSET_INTERNAL_FORM_DEFAULT,
	});

	useEffect(() => {
		const fetchData = async () => {
			try {
				const contracts = await api.pagging<ContractCode>(
					API.CATALOG.CONTRACT_CODE.LIST,
				);
				const units = await api.pagging<Unit>(API.CATALOG.UNIT.LIST);

				setContracts(contracts.result.data);
				setUnits(units.result.data);

				if (row) {
					const res = await api.get<AssetInternalDetail>(
						API.CATALOG.ASSET.DETAIL(row.id),
					);
					const { costs, ...data } = res.result;

					form.reset({
						...data,
						costs: costs?.length
							? costs.map((cost) => ({
									startMonth: cost.startMonth.substring(0, 10),
									endMonth: cost.endMonth.substring(0, 10),
									amount: cost.amount,
									actualAmount: cost.actualAmount,
								}))
							: ASSET_INTERNAL_FORM_DEFAULT.costs,
					});
				}
			} catch (error) {
				console.error('Error fetching data:', error);
			}
		};

		fetchData();
	}, [row]);

	const handleSubmit = async (values: AssetInternalFormSchema) => {
		try {
			const processedValues = {
				...values,
			};
			if (row?.id) {
				await api.put(API.CATALOG.ASSET.UPDATE, {
					id: row.id,
					...processedValues,
				});
			} else {
				await api.post(API.CATALOG.ASSET.CREATE, {
					...processedValues,
					isOtherMaterial: false,
				});
			}
			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormComboBox
				control={form.control}
				name='assigmentCodeId'
				label='Mã giao khoán'
				placeholder='Chọn mã giao khoán'
				options={contracts.map((contract) => ({
					value: contract.id,
					label: contract.code,
				}))}
			/>

			<FormInput
				control={form.control}
				name='code'
				label='Mã vật tư, tài sản'
				placeholder='Nhập mã vật tư, tài sản'
			/>

			<FormInput
				control={form.control}
				name='name'
				label='Tên vật tư, tài sản'
				placeholder='Nhập tên vật tư, tài sản'
			/>

			<div className='flex-1'>
				<FormNumber
					control={form.control}
					name={`usageTime`}
					label='Thời gian sử dụng (tháng)'
					placeholder='Nhập thời gian sử dụng (tháng)'
				/>
			</div>

			<FormComboBox
				control={form.control}
				name='unitOfMeasureId'
				label='Đơn vị tính'
				placeholder='Chọn đơn vị tính'
				options={units.map((unit) => ({
					value: unit.id,
					label: unit.name,
				}))}
			/>

			<FormArray control={form.control} name='costs' label='Đơn giá'>
				{(index) => (
					<div className='flex w-full gap-4'>
						<FormMonthYear
							control={form.control}
							name={`costs.${index}.startMonth`}
							label='Thời gian bắt đầu'
							className='flex-1'
						/>
						<FormMonthYear
							control={form.control}
							name={`costs.${index}.endMonth`}
							label='Thời gian kết thúc'
							className='flex-1'
						/>
						<div className='flex-1'>
							<FormNumber
								control={form.control}
								name={`costs.${index}.amount`}
								label='Đơn giá kế hoạch (đ)'
								placeholder='Nhập đơn giá kế hoạch (đ)'
							/>
						</div>
						<div className='flex-1'>
							<FormNumber
								control={form.control}
								name={`costs.${index}.actualAmount`}
								label='Đơn giá thực tế(đ)'
								placeholder='Nhập đơn giá thực tế (đ)'
							/>
						</div>
					</div>
				)}
			</FormArray>

			<DataTableEditConfirm isEdit={!!row} />
		</FormProvider>
	);
}
