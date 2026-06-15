import { DataTableImport } from '@/components/datatable/import';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import type { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useState } from 'react';
import { Resolver, useForm } from 'react-hook-form';
import { UnresolvedCatalogCreateDialog } from '../unresolved/unresolved-catalog-create-dialog';
import type { UnresolvedCatalogCreateSelection } from '../unresolved/unresolved-catalog-create-dialog';
import { UnresolvedCreateDialog } from '../unresolved/unresolved-create-dialog';
import type { UnresolvedCreateSchema } from '../unresolved/unresolved-create-schema';
import type { AcceptanceReportDetail } from '../types';
import { AcceptanceReportEditor } from './editor';
import {
	acceptanceReportEditorFormSchema,
	type AcceptanceReportEditorFormInput,
	type AcceptanceReportEditorRow,
} from './schema';
import {
	type AssignmentCodeOption,
	type ImportedItemMeta,
	type MaterialLookupOption,
	type ProcessGroupOption,
	type ProductionOrder,
	type ProductionOrderOption,
	type UploadAcceptanceReportResponseDto,
	type CreateAcceptanceReportRequest,
	ImportResolutionStatus,
	MaterialType,
} from './types';
import {
	NONE_PRODUCTION_ORDER_ID,
	normalizeCode,
	toAssignmentCodeOptionValue,
	toProductionOrderOptionValue,
} from './helpers';
import {
	buildAcceptanceReportRequest,
	extractImportedItems,
	mapAcceptanceReportDetailToEditorForm,
	mapResolvedImportItem,
	mapUnresolvedImportItem,
} from './mappers';

type MaterialImportDialogProps = {
	onSave: (data: AcceptanceReportEditorRow[]) => void;
	productionOutputId?: string;
	acceptanceReportId?: string | null;
	currentAcceptanceReportDetail?: AcceptanceReportDetail | null;
};

type MaterialLookupItem = {
	id: string;
	code: string;
	name: string;
	unitOfMeasureName: string;
	materialType: number;
};

type ProductionOutputScopeResponse = {
	processGroups?: {
		processGroupId: string;
	}[];
};

const NONE_PRODUCTION_ORDER_OPTION: ProductionOrderOption = {
	value: toProductionOrderOptionValue(NONE_PRODUCTION_ORDER_ID),
	label: '[Lệnh sản xuất] Không theo lệnh sản xuất',
};

export function MaterialImportDialog({
	onSave,
	productionOutputId,
	acceptanceReportId,
	currentAcceptanceReportDetail,
}: MaterialImportDialogProps) {
	const [showForm, setShowForm] = useState(false);
	const [filePath, setFilePath] = useState('');
	const [isLoading, setIsLoading] = useState(false);
	const [processGroupOptions, setProcessGroupOptions] = useState<
		ProcessGroupOption[]
	>([]);
	const [productionOrderOptions, setProductionOrderOptions] = useState<
		ProductionOrderOption[]
	>([]);
	const [assignmentCodeOptions, setAssignmentCodeOptions] = useState<
		ProductionOrderOption[]
	>([]);
	const [materialLookupOptions, setMaterialLookupOptions] = useState<
		MaterialLookupOption[]
	>([]);
	const [importedItems, setImportedItems] = useState<ImportedItemMeta[]>([]);
	const [orderOrAssignmentCodeOptionsByItemId, setOrderOrAssignmentCodeOptionsByItemId] =
		useState<Record<string, ProductionOrderOption[]>>({});
	const [unresolvedCreateIndex, setUnresolvedCreateIndex] = useState<
		number | null
	>(null);
	const [unresolvedCreateSelection, setUnresolvedCreateSelection] =
		useState<UnresolvedCatalogCreateSelection | null>(null);
	const [latestAcceptanceReportDetail, setLatestAcceptanceReportDetail] =
		useState<AcceptanceReportDetail | null>(
			currentAcceptanceReportDetail ?? null,
		);
	const popup = usePopup();
	const { setOpen } = useDialog();

	const form = useForm<AcceptanceReportEditorFormInput>({
		resolver: zodResolver(
			acceptanceReportEditorFormSchema,
		) as Resolver<AcceptanceReportEditorFormInput>,
		defaultValues: {
			materials: [],
		},
		mode: 'onChange',
		shouldUnregister: false,
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
			} catch (error) {
				if (!isMounted) return;
				setProductionOrderOptions([]);
				console.error('Failed to fetch production orders:', error);
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
			} catch (error) {
				if (!isMounted) return;
				setMaterialLookupOptions([]);
				console.error('Failed to fetch material lookup options:', error);
			}
		};

		fetchMaterialLookupOptions();

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
			} catch (error) {
				if (!isMounted) return;
				setAssignmentCodeOptions([]);
				console.error('Failed to fetch assignment codes:', error);
			}
		};

		fetchAssignmentCodes();

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
		if (!productionOutputId) {
			setProcessGroupOptions([]);
			return;
		}

		let isMounted = true;

		const fetchScopedProcessGroups = async () => {
			try {
				const [outputRes, processGroupRes] = await Promise.all([
					api.get<ProductionOutputScopeResponse>(
						API.PRODUCTION.PRODUCTION_OUTPUT.RAW_DETAIL(productionOutputId),
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
						type: group.fixedKeyType,
					}));

				setProcessGroupOptions(options);
			} catch (error) {
				if (!isMounted) return;
				setProcessGroupOptions([]);
				console.error('Failed to fetch scoped process groups:', error);
			}
		};

		fetchScopedProcessGroups();

		return () => {
			isMounted = false;
		};
	}, [productionOutputId]);

	useEffect(() => {
		if (currentAcceptanceReportDetail) {
			setLatestAcceptanceReportDetail(currentAcceptanceReportDetail);
		}
	}, [currentAcceptanceReportDetail]);

	useEffect(() => {
		if (!acceptanceReportId) return;

		let isMounted = true;

		const fetchAcceptanceReportDetail = async () => {
			try {
				const response = await api.get<AcceptanceReportDetail>(
					API.PRODUCTION.ACCEPTANCE_REPORT.RAW_DETAIL(acceptanceReportId),
				);

				if (!isMounted || !response.result) return;

				setLatestAcceptanceReportDetail(response.result);
			} catch (error) {
				if (!isMounted) return;
				console.error('Failed to fetch acceptance report detail:', error);
			}
		};

		fetchAcceptanceReportDetail();

		return () => {
			isMounted = false;
		};
	}, [acceptanceReportId]);

	const handleImport = async (file: File) => {
		try {
			setIsLoading(true);
			if (!productionOutputId) {
				popup.error('Không tìm thấy mã sản xuất');
				return;
			}

			const response = await api.uploadFile<UploadAcceptanceReportResponseDto>(
				API.PRODUCTION.ACCEPTANCE_REPORT.UPLOAD_FILE(productionOutputId),
				file,
			);

			setFilePath(response.result.filePath);

			const existingRowsByReportItemId = new Map(
				(
					latestAcceptanceReportDetail
						? mapAcceptanceReportDetailToEditorForm(
								latestAcceptanceReportDetail,
							).materials
						: []
				)
					.filter((row) => row.acceptanceReportItemId)
					.map((row) => [row.acceptanceReportItemId as string, row]),
			);

			const uploadRows: AcceptanceReportEditorRow[] = [
				...response.result.acceptanceReports.map((item) => {
					const uploadedRow = mapResolvedImportItem(item);
					const existingRow =
						item.reportItemId != null
							? existingRowsByReportItemId.get(item.reportItemId)
							: undefined;

					if (!existingRow) {
						return uploadedRow;
					}

					return {
						...existingRow,
						id: existingRow.id || uploadedRow.id,
						acceptanceReportItemId:
							uploadedRow.acceptanceReportItemId ??
							existingRow.acceptanceReportItemId,
						materialOrPartId:
							uploadedRow.materialOrPartId ?? existingRow.materialOrPartId,
						sourceRowNumber: uploadedRow.sourceRowNumber,
						resolutionStatus: ImportResolutionStatus.Resolved,
						partType: uploadedRow.partType ?? existingRow.partType,
						documentNumber: uploadedRow.documentNumber,
						postingDate: uploadedRow.postingDate,
						materialCode: uploadedRow.materialCode,
						materialName: uploadedRow.materialName,
						unitOfMeasureName: uploadedRow.unitOfMeasureName,
						type: uploadedRow.type,
						itemType: uploadedRow.itemType,
						quantityReceived: uploadedRow.quantityReceived,
						quantityExported: uploadedRow.quantityExported,
						receivedTypes: uploadedRow.receivedTypes,
						exportedTypes: uploadedRow.exportedTypes,
						receivedBreakdown: uploadedRow.receivedBreakdown,
						exportedBreakdown: uploadedRow.exportedBreakdown,
						quantity:
							uploadedRow.quantityReceived + uploadedRow.quantityExported,
					};
				}),
				...(response.result.unresolvedAcceptanceReports || []).map((item) =>
					mapUnresolvedImportItem(item),
				),
			].sort(
				(a, b) =>
					(a.sourceRowNumber ?? Number.MAX_SAFE_INTEGER) -
					(b.sourceRowNumber ?? Number.MAX_SAFE_INTEGER),
			);

			setImportedItems(extractImportedItems(uploadRows));
			form.setValue('materials', uploadRows);
			setShowForm(true);
		} catch (error) {
			popup.error(error);
			setShowForm(false);
			form.setValue('materials', []);
			setFilePath('');
			setImportedItems([]);
			setOrderOrAssignmentCodeOptionsByItemId({});
		} finally {
			setIsLoading(false);
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
	};

	const handleSelectUnresolvedType = async (values: UnresolvedCreateSchema) => {
		if (unresolvedCreateIndex == null) return;

		setUnresolvedCreateSelection({
			entityGroup: values.entityGroup,
			specificType: values.specificType ?? 1,
		});
	};

	const handleCreatedCatalogItem = async (values: { code: string }) => {
		const unresolvedIndex = unresolvedCreateIndex;
		const unresolvedSelection = unresolvedCreateSelection;
		if (unresolvedIndex == null || unresolvedSelection == null) {
			return;
		}

		const normalizedCodeValue = normalizeCode(values.code);
		const specificType = unresolvedSelection.specificType;
		if (!normalizedCodeValue) {
			popup.error('Không tìm thấy mã cần tạo mới');
			return;
		}

		const materialsRes = await api.pagging<MaterialLookupItem>(
			API.CATALOG.ASSET.LIST,
			{
				ignorePagination: true,
				search: values.code,
			},
		);

		const createdMaterial = materialsRes.result.data.find(
			(item) => normalizeCode(item.code) === normalizedCodeValue,
		);

		if (!createdMaterial) {
			throw new Error('Không tìm thấy vật tư vừa tạo.');
		}

		form.setValue(
			`materials.${unresolvedIndex}`,
			mapResolvedImportItem({
				reportItemId:
					form.getValues(`materials.${unresolvedIndex}.acceptanceReportItemId`) ??
					null,
				documentNumber:
					form.getValues(`materials.${unresolvedIndex}.documentNumber`) ?? '',
				postingDate:
					form.getValues(`materials.${unresolvedIndex}.postingDate`) ?? null,
				materialId: createdMaterial.id,
				materialCode:
					form.getValues(`materials.${unresolvedIndex}.materialCode`) ??
					createdMaterial.code,
				materialName: createdMaterial.name,
				unitOfMeasureName: createdMaterial.unitOfMeasureName,
				type: MaterialType.Material,
				itemType: specificType,
				issuedQuantity:
					form.getValues(`materials.${unresolvedIndex}.quantityReceived`) ?? 0,
				shippedQuantity:
					form.getValues(`materials.${unresolvedIndex}.quantityExported`) ?? 0,
				rowNumber:
					form.getValues(`materials.${unresolvedIndex}.sourceRowNumber`) ?? 0,
			}),
		);

		setUnresolvedCreateSelection(null);
		setUnresolvedCreateIndex(null);
	};

	const handleSubmit = async (formData: AcceptanceReportEditorFormInput) => {
		try {
			const parsedFormData = acceptanceReportEditorFormSchema.parse(formData);
			const unresolvedItems = parsedFormData.materials.filter(
				(item) => item.resolutionStatus === ImportResolutionStatus.Unresolved,
			);
			if (unresolvedItems.length > 0) {
				popup.error(
					`Còn ${unresolvedItems.length} dòng chưa được tạo mới trong danh mục. Vui lòng xử lý trước khi lưu.`,
				);
				return;
			}

			if (!productionOutputId) {
				popup.error('Không tìm thấy mã sản xuất');
				return;
			}

			const requestData = buildAcceptanceReportRequest(
				'import',
				parsedFormData,
				{
					productionOutputId,
					filePath,
				},
			) as CreateAcceptanceReportRequest;

			await api.post(API.PRODUCTION.ACCEPTANCE_REPORT.CREATE, requestData);

			onSave(parsedFormData.materials);
			setShowForm(false);
			form.reset();
			setFilePath('');
			setImportedItems([]);
			setOrderOrAssignmentCodeOptionsByItemId({});
			setOpen(false);
			popup.success('Dữ liệu được lưu thành công');
		} catch (error) {
			popup.error(error);
		}
	};

	const unresolvedCount = form
		.watch('materials')
		.filter(
			(item) => item.resolutionStatus === ImportResolutionStatus.Unresolved,
		).length;

	return (
		<>
			{!showForm ? (
				<div className='flex flex-col gap-4'>
					<DataTableImport onImport={handleImport} isLoading={isLoading} />
				</div>
			) : (
				<FormProvider context={form} onSubmit={handleSubmit}>
					<AcceptanceReportEditor
						mode='import'
						onCancel={() => setShowForm(false)}
						processGroupOptions={processGroupOptions}
						productionOrderOptions={productionOrderOptions}
						assignmentCodeOptions={assignmentCodeOptions}
						orderOrAssignmentCodeOptionsByItemId={
							orderOrAssignmentCodeOptionsByItemId
						}
						materialLookupOptions={materialLookupOptions}
						onMaterialAdded={handleMaterialAdded}
						unresolvedCount={unresolvedCount}
						onCreateUnresolved={setUnresolvedCreateIndex}
					/>
				</FormProvider>
			)}
			<UnresolvedCreateDialog
				open={
					unresolvedCreateIndex != null && unresolvedCreateSelection == null
				}
				onOpenChange={(open) => {
					if (!open) {
						setUnresolvedCreateSelection(null);
						setUnresolvedCreateIndex(null);
					}
				}}
				defaultCode={
					unresolvedCreateIndex != null
						? (form.getValues(
								`materials.${unresolvedCreateIndex}.materialCode`,
							) ?? '')
						: ''
				}
				onSubmit={handleSelectUnresolvedType}
			/>
			<UnresolvedCatalogCreateDialog
				open={unresolvedCreateSelection != null}
				onOpenChange={(open) => {
					if (!open) {
						setUnresolvedCreateSelection(null);
						setUnresolvedCreateIndex(null);
					}
				}}
				selection={unresolvedCreateSelection}
				defaultCode={
					unresolvedCreateIndex != null
						? (form.getValues(
								`materials.${unresolvedCreateIndex}.materialCode`,
							) ?? '')
						: ''
				}
				onCreated={handleCreatedCatalogItem}
			/>
		</>
	);
}
