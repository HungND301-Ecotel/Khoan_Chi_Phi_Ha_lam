import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { Longwallparameters } from '@/features/main/catalog/parameter/longwallparameters/columns';
import type { Seamface } from '@/features/main/catalog/parameter/seamface/columns';
import type { Cuttingthickness } from '@/features/main/catalog/parameter/cuttingthickness/columns';
import type { LongwallMaterial } from '@/features/main/pricing/longwall-panel/material/columns';
import {
	LONGWALL_MATERIAL_FORM_DEFAULT,
	longwallMaterialFormSchema,
	type LongwallMaterialFormSchema,
} from '@/features/main/pricing/longwall-panel/material/schema';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { api } from '@/lib/api';
import { ProcessStep } from '@/features/main/catalog/process/step/columns';

interface Technology {
	id: string;
	value: string;
}

interface LongwallMaterialFormProps
	extends ActionDialogProps<LongwallMaterial> {}

interface LongwallMaterialDetail {
	id: string;
	code: string;
	longwallParameters: { id: string; llc: string; lkc: number; mk: number };
	cuttingThickness: { id: string; from: string; to: string };
	seamFaceId?: string;
	technologyId?: string;
	processId: string;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
}

export function LongwallMaterialForm({ data, row }: LongwallMaterialFormProps) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [longwallParameters, setLongwallParameters] = useState<
		Longwallparameters[]
	>([]);
	const [technologies, setTechnologies] = useState<Technology[]>([]);
	const [seamfaces, setSeamfaces] = useState<Seamface[]>([]);
	const [cuttingthicknesses, setCuttingthicknesses] = useState<
		Cuttingthickness[]
	>([]);
	const [processes, setProcesses] = useState<ProcessStep[]>([]);
	const form = useForm<LongwallMaterialFormSchema>({
		resolver: zodResolver(longwallMaterialFormSchema),
		mode: 'onSubmit',
		defaultValues: {
			...LONGWALL_MATERIAL_FORM_DEFAULT,
			startMonth: new Date().toISOString().substring(0, 10),
			endMonth: new Date().toISOString().substring(0, 10),
		},
	});

	useEffect(() => {
		const loadData = async () => {
			try {
				// Fetch all parameter data from APIs
				const [techRes, longwallRes, cuttingRes, seamRes, processRes] =
					await Promise.all([
						api.pagging<Technology>(API.CATALOG.PARAMETER.TECHNOLOGY.LIST),
						api.pagging<Longwallparameters>(
							API.CATALOG.PARAMETER.LONGWALLPARAMETERS.LIST,
						),
						api.pagging<Cuttingthickness>(
							API.CATALOG.PARAMETER.CUTTINGTHICKNESS.LIST,
						),
						api.pagging<Seamface>(API.CATALOG.PARAMETER.SEAMFACE.LIST),
						api.pagging<ProcessStep>(API.CATALOG.PROCESS.STEP.LIST),
					]);

				setTechnologies(techRes.result.data);
				setLongwallParameters(longwallRes.result.data);
				setCuttingthicknesses(cuttingRes.result.data);
				setSeamfaces(seamRes.result.data);
				setProcesses(processRes.result.data);

				if (row?.id) {
					// Fetch detail to get nested objects
					try {
						const detailRes = await api.get<LongwallMaterialDetail>(
							API.PRICING.MATERIAL.LONGWALL_PANEL.DETAIL(row.id),
						);
						const detail = detailRes.result;

						form.reset({
							id: detail.id,
							code: detail.code,
							longwallParametersId: detail.longwallParameters?.id || '',
							cuttingThicknessId: detail.cuttingThickness?.id || '',
							seamFaceId: detail.seamFaceId || row.seamFaceId || '',
							technologyId: detail.technologyId || row.technologyId || '',
							processId: detail.processId || row.processId || '',
							startMonth: detail.startMonth.substring(0, 10),
							endMonth: detail.endMonth.substring(0, 10),

							totalPrice: detail.totalPrice,
						});
					} catch (error) {
						// Fallback to row data if DETAIL fails
						form.reset({
							id: row.id,
							code: row.code,
							longwallParametersId:
								row.longwallParametersId || row.passportId || '',
							cuttingThicknessId:
								row.cuttingThicknessId || row.cuttingthicknessId || '',
							seamFaceId: row.seamFaceId || row.mValue || '',
							technologyId: row.technologyId || '',
							processId: row.processId || '',
							startMonth: row.startMonth.substring(0, 10),
							endMonth: row.endMonth.substring(0, 10),

							totalPrice: row.totalPrice,
						});
					}
				}
			} catch (error) {
				popup.error(error);
			}
		};

		loadData();
	}, [row?.id]);

	const handleSubmit = async (values: LongwallMaterialFormSchema) => {
		try {
			const processedValues = {
				...values,
			};

			const payload = {
				code: processedValues.code,
				longwallParametersId: processedValues.longwallParametersId,
				cuttingThicknessId: processedValues.cuttingThicknessId,
				seamFaceId: processedValues.seamFaceId,
				technologyId: processedValues.technologyId,
				processId: processedValues.processId,
				startMonth: processedValues.startMonth,
				endMonth: processedValues.endMonth,
				totalPrice: processedValues.totalPrice || 0,
			};

			if (row?.id) {
				await api.put(API.PRICING.MATERIAL.LONGWALL_PANEL.UPDATE, {
					id: row.id,
					...payload,
				});
				popup.success(`${breadcrumb} đã được cập nhật thành công.`);
			} else {
				await api.post(API.PRICING.MATERIAL.LONGWALL_PANEL.CREATE, payload);
				popup.success(`${breadcrumb} đã được tạo mới thành công.`);
			}

			setOpen(false);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormRow>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Thời gian bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control}
					name='endMonth'
					label='Thời gian kết thúc'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

			<FormInput
				control={form.control}
				name='code'
				label='Mã định mức vật liệu'
				placeholder='Nhập mã định mức vật liệu'
			/>

			<FormComboBox
				control={form.control}
				name='processId'
				label='Công đoạn sản xuất'
				placeholder='Chọn công đoạn sản xuất'
				options={processes.map((process) => ({
					label: process.name,
					value: process.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='technologyId'
				label='Công nghệ khai thác'
				placeholder='Chọn công nghệ khai thác'
				options={technologies.map((tech) => ({
					label: tech.value,
					value: tech.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='longwallParametersId'
				label='Thông số lò chợ'
				placeholder='Chọn thông số lò chợ'
				options={longwallParameters.map((parameter) => ({
					label: `${parameter.llc}; ${parameter.lkc}; ${parameter.mk}`,
					value: parameter.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='cuttingThicknessId'
				label='Chiều dày lớp khấu (m)'
				placeholder='Chọn chiều dày lớp khấu (m)'
				options={cuttingthicknesses.map((ct) => ({
					label: ct.value,
					value: ct.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='seamFaceId'
				label='Mặt vỉa (m)'
				placeholder='Chọn mặt vỉa (m)'
				options={seamfaces.map((seamface) => ({
					label: seamface.value,
					value: seamface.id,
				}))}
			/>

			<FormNumber
				control={form.control}
				name='totalPrice'
				label='Đơn giá vật liệu (đ/tấn)'
				placeholder='Nhập đơn giá vật liệu'
			/>

			<DataTableEditConfirm isEdit={!!row?.id} />
		</FormProvider>
	);
}
