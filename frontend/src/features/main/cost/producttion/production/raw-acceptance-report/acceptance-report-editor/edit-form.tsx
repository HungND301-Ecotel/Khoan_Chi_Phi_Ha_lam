import { FormProvider } from '@/components/form/form-provider';
import { popup as globalPopup, usePopup } from '@/components/popup';
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
import { FieldErrors, Resolver, useForm } from 'react-hook-form';
import { AcceptanceReportEditor } from './editor';
import {
	acceptanceReportEditorFormSchema,
	type AcceptanceReportEditorFormInput,
} from './schema';
import type {
	AssignmentCodeOption,
	ImportedItemMeta,
	MaterialLookupOption,
	ProcessGroupOption,
	ProductionOrderOption,
} from './types';
import { MaterialType } from './types';
import {
	NONE_PRODUCTION_ORDER_ID,
	toAssignmentCodeOptionValue,
	toProductionOrderOptionValue,
} from './helpers';
import {
	buildAcceptanceReportRequest,
	extractImportedItems,
	mapAcceptanceReportDetailToEditorForm,
} from './mappers';

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

const NONE_PRODUCTION_ORDER_OPTION: ProductionOrderOption = {
	value: toProductionOrderOptionValue(NONE_PRODUCTION_ORDER_ID),
	label: '[Lệnh sản xuất] Không theo lệnh sản xuất',
};

function collectFormErrorMessages(
	errors: FieldErrors<AcceptanceReportEditorFormInput>,
	materials: AcceptanceReportEditorFormInput['materials'],
): string[] {
	const result = new Set<string>();

	const getMaterialPrefix = (path: Array<string | number>) => {
		const materialsIndex = path.findIndex((segment) => segment === 'materials');
		if (materialsIndex < 0) return '';

		const rowIndex = path[materialsIndex + 1];
		if (typeof rowIndex !== 'number') return '';

		const materialCode = materials[rowIndex]?.materialCode?.trim();
		return materialCode ? `[${materialCode}] ` : '';
	};

	const walk = (value: unknown, path: Array<string | number> = []) => {
		if (!value) return;
		if (Array.isArray(value)) {
			for (const [index, item] of value.entries()) {
				walk(item, [...path, index]);
			}
			return;
		}
		if (typeof value !== 'object') return;

		const maybeError = value as { message?: unknown; types?: unknown };
		const prefix = getMaterialPrefix(path);
		if (typeof maybeError.message === 'string' && maybeError.message.trim()) {
			result.add(`${prefix}${maybeError.message.trim()}`);
		}
		if (maybeError.types && typeof maybeError.types === 'object') {
			for (const message of Object.values(maybeError.types)) {
				if (typeof message === 'string' && message.trim()) {
					result.add(`${prefix}${message.trim()}`);
				}
			}
		}
		for (const [key, nested] of Object.entries(
			value as Record<string, unknown>,
		)) {
			if (key === 'message' || key === 'types') continue;
			walk(nested, [...path, key]);
		}
	};

	walk(errors);
	return [...result];
}

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
	const [assignmentCodeOptions, setAssignmentCodeOptions] = useState<
		ProductionOrderOption[]
	>([]);
	const [importedItems, setImportedItems] = useState<ImportedItemMeta[]>([]);
	const [
		orderOrAssignmentCodeOptionsByItemId,
		setOrderOrAssignmentCodeOptionsByItemId,
	] = useState<Record<string, ProductionOrderOption[]>>({});
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

		const fetchAssignmentCodes = async () => {
			try {
				const response = await api.pagging<AssignmentCodeOption>(
					API.CATALOG.CONTRACT_CODE.LIST,
					{
						ignorePagination: true,
					},
				);

				if (!isMounted) return;

				const options = (response.result.data ?? [])
					.sort((a, b) => a.code.localeCompare(b.code))
					.map((item) => ({
						value: toAssignmentCodeOptionValue(item.id),
						label: `[Nhóm vật tư, tài sản] ${item.code} - ${item.name}`,
					}));

				setAssignmentCodeOptions(options);
			} catch (err) {
				if (!isMounted) return;
				setAssignmentCodeOptions([]);
				console.error('Failed to fetch assignment codes:', err);
			}
		};

		fetchAssignmentCodes();

		return () => {
			isMounted = false;
		};
	}, []);

	useEffect(() => {
		let isMounted = true;

		const fetchMaterialLookupOptions = async () => {
			try {
				const materialsRes = await api.pagging<MaterialLookupItem>(
					API.CATALOG.ASSET.LIST,
					{
						ignorePagination: true,
					},
				);

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
				setMaterialLookupOptions(
					materialOptions.sort((a, b) => a.label.localeCompare(b.label)),
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
			nextOptionsByItemId[item.materialOrPartId] = [...productionOrderOptions];
		}

		setOrderOrAssignmentCodeOptionsByItemId(nextOptionsByItemId);
	}, [importedItems, productionOrderOptions]);

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

				const nextFormValues = mapAcceptanceReportDetailToEditorForm(
					response.result,
				);
				const nextImportedItems = extractImportedItems(
					nextFormValues.materials,
				);
				setImportedItems(nextImportedItems);
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

	const handleInvalidSubmit = (
		errors: FieldErrors<AcceptanceReportEditorFormInput>,
	) => {
		const messages = collectFormErrorMessages(
			errors,
			form.getValues('materials'),
		);
		if (messages.length === 0) {
			globalPopup.error('Dữ liệu chưa hợp lệ. Vui lòng kiểm tra lại.');
			return;
		}

		globalPopup.error(messages[0], {
			errorList: messages.slice(1),
		});
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
	};

	return (
		<FormProvider
			context={form}
			onSubmit={handleSubmit}
			onInvalid={handleInvalidSubmit}
		>
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
					assignmentCodeOptions={assignmentCodeOptions}
					orderOrAssignmentCodeOptionsByItemId={
						orderOrAssignmentCodeOptionsByItemId
					}
					materialLookupOptions={materialLookupOptions}
					onMaterialAdded={handleMaterialAdded}
					unresolvedCount={0}
				/>
			)}
		</FormProvider>
	);
}
