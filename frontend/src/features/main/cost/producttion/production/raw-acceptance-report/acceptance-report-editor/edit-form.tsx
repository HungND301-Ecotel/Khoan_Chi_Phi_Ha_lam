import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import type { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import type { ProductCostFormProps } from '@/features/main/cost/plan/types';
import type {
	AcceptanceReportDetail,
	ProductionOrder,
} from '@/features/main/cost/producttion/production/raw-acceptance-report/types';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { Resolver, useForm } from 'react-hook-form';
import { AcceptanceReportEditor } from './editor';
import { acceptanceReportEditorFormSchema, type AcceptanceReportEditorFormInput } from './schema';
import type {
	Equipment,
	ImportedItemMeta,
	MaterialLookupOption,
	ProcessGroupOption,
	ProductionOrderOption,
} from './types';
import { MaterialType } from './types';
import {
	NONE_PRODUCTION_ORDER_ID,
	toEquipmentOptionValue,
	toProductionOrderOptionValue,
} from './helpers';
import {
	buildAcceptanceReportRequest,
	extractImportedItems,
	mapAcceptanceReportDetailToEditorForm,
} from './mappers';

type MaintainEquipmentMapping = {
	partId: string;
	equipments: Equipment[];
};

type ProductionOutputScopeResponse = {
	processGroups?: {
		processGroupId: string;
	}[];
};

type MaterialLookupItem = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureName: string;
	materialType: number;
};

type PartLookupItem = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureName: string;
	partType: number;
};

const NONE_PRODUCTION_ORDER_OPTION: ProductionOrderOption = {
	value: toProductionOrderOptionValue(NONE_PRODUCTION_ORDER_ID),
	label: '[Lệnh sản xuất] Không theo lệnh sản xuất',
};

export function AcceptanceReportEditForm({
	id,
	output,
	callback,
}: ProductCostFormProps) {
	const { setOpen } = useDialog();
	const { success, error } = usePopup();
	const { breadcrumb } = useMeta();
	const [loading, setLoading] = useState(false);
	const [acceptanceReportId, setAcceptanceReportId] = useState('');
	const [filePath, setFilePath] = useState('');
	const [processGroupOptions, setProcessGroupOptions] = useState<
		ProcessGroupOption[]
	>([]);
	const [productionOrderOptions, setProductionOrderOptions] = useState<
		ProductionOrderOption[]
	>([]);
	const [importedItems, setImportedItems] = useState<ImportedItemMeta[]>([]);
	const [equipmentOptionsByPartId, setEquipmentOptionsByPartId] = useState<
		Record<string, ProductionOrderOption[]>
	>({});
	const [orderOrEquipmentOptionsByItemId, setOrderOrEquipmentOptionsByItemId] =
		useState<Record<string, ProductionOrderOption[]>>({});
	const [materialLookupOptions, setMaterialLookupOptions] = useState<
		MaterialLookupOption[]
	>([]);

	const form = useForm<AcceptanceReportEditorFormInput>({
		resolver: zodResolver(
			acceptanceReportEditorFormSchema,
		) as Resolver<AcceptanceReportEditorFormInput>,
		defaultValues: {
			materials: [],
		},
		mode: 'onSubmit',
	});

	useEffect(() => {
		let isMounted = true;

		const fetchProductionOrders = async () => {
			try {
				const response = await api.pagging<ProductionOrder>(
					API.CATALOG.PARAMETER.PRODUCTION_ORDER.LIST,
					{
						ignorePagination: true,
					},
				);

				if (!isMounted) return;

				const options = response.result.data
					.sort((a, b) => a.code.localeCompare(b.code))
					.map((item) => ({
						value: toProductionOrderOptionValue(item.id),
						label: `[Lệnh sản xuất] ${item.code} - ${item.name}`,
					}));

				setProductionOrderOptions([NONE_PRODUCTION_ORDER_OPTION, ...options]);
			} catch (err) {
				if (!isMounted) return;
				setProductionOrderOptions([]);
				console.error('Failed to fetch production orders:', err);
			}
		};

		fetchProductionOrders();

		return () => {
			isMounted = false;
		};
	}, []);

	useEffect(() => {
		let isMounted = true;

		const fetchMaterialLookupOptions = async () => {
			try {
				const [materialsRes, partsRes] = await Promise.all([
					api.pagging<MaterialLookupItem>(API.CATALOG.ASSET.LIST, {
						ignorePagination: true,
					}),
					api.pagging<PartLookupItem>(API.CATALOG.PART.LIST, {
						ignorePagination: true,
					}),
				]);

				if (!isMounted) return;

				const materialOptions: MaterialLookupOption[] = (
					materialsRes.result.data ?? []
				).map((item) => ({
					value: `material:${item.id}`,
					label: `${item.code} - ${item.name}`,
					materialOrPartId: item.id,
					type: MaterialType.Material,
					itemType: item.materialType,
					materialCode: item.code,
					materialName: item.name,
					unitOfMeasureName: item.unitOfMeasureName,
				}));

				const partOptions: MaterialLookupOption[] = (
					partsRes.result.data ?? []
				).map((item) => ({
					value: `part:${item.id}`,
					label: `${item.code} - ${item.name}`,
					materialOrPartId: item.id,
					type: MaterialType.SparePart,
					itemType: item.partType,
					materialCode: item.code,
					materialName: item.name,
					unitOfMeasureName: item.unitOfMeasureName,
				}));

				setMaterialLookupOptions(
					[...materialOptions, ...partOptions].sort((a, b) =>
						a.label.localeCompare(b.label),
					),
				);
			} catch (err) {
				if (!isMounted) return;
				setMaterialLookupOptions([]);
				console.error('Failed to fetch material lookup options:', err);
			}
		};

		fetchMaterialLookupOptions();

		return () => {
			isMounted = false;
		};
	}, []);

	useEffect(() => {
		const nextOptionsByItemId: Record<string, ProductionOrderOption[]> = {};

		for (const item of importedItems) {
			const isPartItem = item.type === MaterialType.SparePart;
			const equipmentOptions = isPartItem
				? (equipmentOptionsByPartId[item.materialOrPartId] ?? [])
				: [];
			nextOptionsByItemId[item.materialOrPartId] = [
				...productionOrderOptions,
				...equipmentOptions,
			];
		}

		setOrderOrEquipmentOptionsByItemId(nextOptionsByItemId);
	}, [equipmentOptionsByPartId, importedItems, productionOrderOptions]);

	useEffect(() => {
		if (!id) {
			setProcessGroupOptions([]);
			return;
		}

		let isMounted = true;

		const fetchScopedProcessGroups = async () => {
			try {
				const [outputRes, processGroupRes] = await Promise.all([
					api.get<ProductionOutputScopeResponse>(
						API.PRODUCTION.PRODUCTION_OUTPUT.RAW_DETAIL(id),
					),
					api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST, {
						ignorePagination: true,
					}),
				]);

				if (!isMounted) return;

				const allowedIds = new Set(
					(outputRes.result.processGroups || []).map(
						(group) => group.processGroupId,
					),
				);

				const options = processGroupRes.result.data
					.filter((group) => allowedIds.has(group.id))
					.sort((a, b) => a.code.localeCompare(b.code))
					.map((group) => ({
						value: group.id,
						label: `${group.code} - ${group.name}`,
					}));

				setProcessGroupOptions(options);
			} catch (err) {
				if (!isMounted) return;
				setProcessGroupOptions([]);
				console.error('Failed to fetch scoped process groups:', err);
			}
		};

		fetchScopedProcessGroups();

		return () => {
			isMounted = false;
		};
	}, [id]);

	useEffect(() => {
		if (output?.acceptanceReportId) {
			setAcceptanceReportId(output.acceptanceReportId);
		}
	}, [output?.acceptanceReportId]);

	useEffect(() => {
		if (!id || !output?.acceptanceReportId) return;

		const fetchAcceptanceReport = async () => {
			setLoading(true);
			try {
				const response = await api.get<AcceptanceReportDetail>(
					API.PRODUCTION.ACCEPTANCE_REPORT.RAW_DETAIL(
						output.acceptanceReportId!,
					),
				);

				if (!response.result) {
					return;
				}

				setAcceptanceReportId(response.result.id);
				setFilePath(response.result.filePath);

				const nextFormValues =
					mapAcceptanceReportDetailToEditorForm(response.result);
				const nextImportedItems = extractImportedItems(nextFormValues.materials);
				setImportedItems(nextImportedItems);

				const partIds = Array.from(
					new Set(
						response.result.items
							.filter((item) => item.type === MaterialType.SparePart)
							.map((item) => item.partId)
							.filter((partId): partId is string => Boolean(partId)),
					),
				);
				const fetchedEquipmentOptionsByPartId: Record<
					string,
					ProductionOrderOption[]
				> = {};

				if (partIds.length > 0) {
					const equipmentMappingsRes = await api.post<
						MaintainEquipmentMapping[],
						string[]
					>(API.PRICING.MAINTENANCE.EQUIPMENTS_BY_PART_IDS, partIds);

					for (const mapping of equipmentMappingsRes.result ?? []) {
						fetchedEquipmentOptionsByPartId[mapping.partId] = (
							mapping.equipments ?? []
						)
							.sort((a, b) => a.code.localeCompare(b.code))
							.map((equipment) => ({
								value: toEquipmentOptionValue(equipment.id),
								label: `[Mã giao khoán] ${equipment.code} - ${equipment.name}`,
							}));
					}
				}

				for (const partId of partIds) {
					if (!fetchedEquipmentOptionsByPartId[partId]) {
						fetchedEquipmentOptionsByPartId[partId] = [];
					}
				}

				setEquipmentOptionsByPartId(fetchedEquipmentOptionsByPartId);
				form.reset(nextFormValues);
			} catch (err) {
				console.error('Failed to fetch acceptance report:', err);
				error(err);
			} finally {
				setLoading(false);
			}
		};

		fetchAcceptanceReport();
	}, [error, form, id, output?.acceptanceReportId]);

	const handleSubmit = async (values: AcceptanceReportEditorFormInput) => {
		try {
			const parsedValues = acceptanceReportEditorFormSchema.parse(values);
			const reportId = acceptanceReportId || output?.acceptanceReportId || '';
			if (!reportId) {
				error('Thiếu thông tin cần thiết');
				return;
			}

			const requestData = buildAcceptanceReportRequest('edit', parsedValues, {
				reportId,
				filePath,
			});

			await api.put(API.PRODUCTION.ACCEPTANCE_REPORT.UPDATE, requestData);

			success(
				`${breadcrumb} đã được ${id ? 'cập nhật' : 'tạo mới'} thành công.`,
			);
			await callback?.();
			setOpen(false);
		} catch (err) {
			error(err);
		}
	};

	const handleMaterialAdded = async (option: MaterialLookupOption) => {
		const nextImportedItems = [
			...importedItems.filter(
				(item) => item.materialOrPartId !== option.materialOrPartId,
			),
			{
				materialOrPartId: option.materialOrPartId,
				type: option.type,
			},
		];
		setImportedItems(nextImportedItems);

		if (
			option.type === MaterialType.SparePart &&
			!equipmentOptionsByPartId[option.materialOrPartId]
		) {
			try {
				const equipmentMappingsRes = await api.post<
					MaintainEquipmentMapping[],
					string[]
				>(API.PRICING.MAINTENANCE.EQUIPMENTS_BY_PART_IDS, [
					option.materialOrPartId,
				]);
				const nextEquipmentOptionsByPartId = { ...equipmentOptionsByPartId };
				for (const mapping of equipmentMappingsRes.result ?? []) {
					nextEquipmentOptionsByPartId[mapping.partId] = (
						mapping.equipments ?? []
					)
						.sort((a, b) => a.code.localeCompare(b.code))
						.map((equipment) => ({
							value: toEquipmentOptionValue(equipment.id),
							label: `[Mã giao khoán] ${equipment.code} - ${equipment.name}`,
						}));
				}
				if (!nextEquipmentOptionsByPartId[option.materialOrPartId]) {
					nextEquipmentOptionsByPartId[option.materialOrPartId] = [];
				}
				setEquipmentOptionsByPartId(nextEquipmentOptionsByPartId);
			} catch (err) {
				console.error('Failed to fetch equipment options for added part:', err);
			}
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			{loading ? (
				<div className='flex h-full items-center justify-center'>
					<div className='size-5 animate-spin rounded-full border-2 border-slate-300 border-t-slate-700' />
				</div>
			) : (
				<AcceptanceReportEditor
					mode='edit'
					onCancel={() => setOpen(false)}
					processGroupOptions={processGroupOptions}
					productionOrderOptions={productionOrderOptions}
					orderOrEquipmentOptionsByItemId={orderOrEquipmentOptionsByItemId}
					materialLookupOptions={materialLookupOptions}
					onMaterialAdded={handleMaterialAdded}
					unresolvedCount={0}
				/>
			)}
		</FormProvider>
	);
}
