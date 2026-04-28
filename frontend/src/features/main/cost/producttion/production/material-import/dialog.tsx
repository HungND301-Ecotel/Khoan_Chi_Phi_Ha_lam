import { DataTableImport } from '@/components/datatable/import';
import { FormProvider } from '@/components/form/form-provider';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import type { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { api } from '@/lib/api';
import { zodResolver } from '@hookform/resolvers/zod';
import { ReactNode, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { MaterialImportForm } from './form';
import {
	MATERIAL_FORM_DEFAULT,
	MaterialFormSchema,
	materialsFormSchema,
} from './schema';
import {
	AdditionalCost,
	Asset,
	CreateAcceptanceReportRequest,
	Equipment,
	ItemType,
	MaterialType,
	MaterialsIncludedInContractRevenue,
	OtherMaterialDetail,
	ProductionOrder,
	QuantityDetail,
	QuotaBasedMaterial,
	QuotaBasedMaterialType,
	ISSUED_DETAIL_TYPE_BY_KEY,
	SHIPPED_DETAIL_TYPE_BY_KEY,
	UploadAcceptanceReportResponseDto,
} from './types';

type MaterialImportDialogProps = {
	trigger: ReactNode;
	onSave: (data: MaterialFormSchema[]) => void;
	productionOutputId?: string;
};

type ProcessGroupOption = {
	value: string;
	label: string;
	type: number;
};

type ProductionOrderOption = {
	value: string;
	label: string;
};

type ImportedItemMeta = {
	materialOrPartId: string;
	type: number;
};

type PartEquipmentMapping = {
	partId: string;
	equipments: Equipment[];
};

const PRODUCTION_ORDER_OPTION_PREFIX = 'production-order:';
const EQUIPMENT_OPTION_PREFIX = 'equipment:';

const NONE_PRODUCTION_ORDER_OPTION: ProductionOrderOption = {
	value: toProductionOrderOptionValue(''),
	label: '[Lệnh sản xuất] Không theo lệnh sản xuất',
};

function toProductionOrderOptionValue(id: string): string {
	return `${PRODUCTION_ORDER_OPTION_PREFIX}${id}`;
}

function toEquipmentOptionValue(id: string): string {
	return `${EQUIPMENT_OPTION_PREFIX}${id}`;
}

function resolveSelectionIds(value?: string | null): {
	productionOrderId: string | null;
	equipmentId: string | null;
} {
	if (!value) {
		return {
			productionOrderId: null,
			equipmentId: null,
		};
	}

	if (value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX)) {
		const parsedId = normalizeProductionOrderId(
			value.slice(PRODUCTION_ORDER_OPTION_PREFIX.length),
		);
		return {
			productionOrderId: parsedId,
			equipmentId: null,
		};
	}

	if (value.startsWith(EQUIPMENT_OPTION_PREFIX)) {
		const equipmentId = value.slice(EQUIPMENT_OPTION_PREFIX.length).trim();
		return {
			productionOrderId: null,
			equipmentId: equipmentId.length > 0 ? equipmentId : null,
		};
	}

	return {
		productionOrderId: normalizeProductionOrderId(value),
		equipmentId: null,
	};
}

type ProductionOutputScopeResponse = {
	processGroups?: {
		processGroupId: string;
	}[];
};

function getDefaultCategoryByMaterialType(type?: number | null): number | null {
	if (type === MaterialType.Material) {
		return MaterialsIncludedInContractRevenue.Material;
	}

	if (type === MaterialType.SparePart) {
		return MaterialsIncludedInContractRevenue.Maintain;
	}

	return null;
}

function getDefaultAdditionalCostByMaterialType(
	type?: number | null,
): number | null {
	if (type === MaterialType.Material) {
		return AdditionalCost.Material;
	}

	if (type === MaterialType.SparePart) {
		return AdditionalCost.Maintain;
	}

	return null;
}

function normalizeProductionOrderId(value?: string | null): string | null {
	if (!value) return null;
	const trimmedValue = value.trim();
	return trimmedValue.length > 0 ? trimmedValue : null;
}

function parseQuantity(value: number | string | null | undefined): number {
	if (value == null || value === '') return 0;
	const normalized = Number(value);
	return Number.isFinite(normalized) ? normalized : 0;
}

export function MaterialImportDialog({
	onSave,
	productionOutputId,
}: MaterialImportDialogProps) {
	const [showForm, setShowForm] = useState(false);
	const [filePath, setFilePath] = useState<string>('');
	const [isLoading, setIsLoading] = useState(false);
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
	const popup = usePopup();
	const { setOpen } = useDialog();

	const form = useForm<{ materials: MaterialFormSchema[] }>({
		resolver: zodResolver(materialsFormSchema),
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
	}, [importedItems, productionOrderOptions, equipmentOptionsByPartId]);

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
						type: group.type,
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

	const handleImport = async (file: File) => {
		try {
			setIsLoading(true);
			if (!productionOutputId) {
				popup.error('Không tìm thấy mã sản xuất');
				return;
			}

			// Call API to upload file
			const response = await api.uploadFile<UploadAcceptanceReportResponseDto>(
				API.PRODUCTION.ACCEPTANCE_REPORT.UPLOAD_FILE(productionOutputId),
				file,
			);

			// Store filePath for later use
			setFilePath(response.result.filePath);

			const importedItemMetas: ImportedItemMeta[] =
				response.result.acceptanceReports.map((item) => ({
					materialOrPartId: item.partId ?? item.materialId ?? '',
					type: item.type,
				}));
			setImportedItems(importedItemMetas);

			const partIds = Array.from(
				new Set(
					response.result.acceptanceReports
						.filter((item) => item.type === MaterialType.SparePart)
						.map((item) => item.partId)
						.filter((id): id is string => Boolean(id)),
				),
			);

			const fetchedEquipmentOptionsByPartId: Record<
				string,
				ProductionOrderOption[]
			> = {};

			if (partIds.length > 0) {
				const equipmentMappingsRes = await api.post<
					PartEquipmentMapping[],
					string[]
				>(API.PRICING.MAINTENANCE.EQUIPMENTS_BY_PART_IDS, partIds);

				for (const mapping of equipmentMappingsRes.result ?? []) {
					const sortedEquipments = [...(mapping.equipments ?? [])].sort(
						(a, b) => a.code.localeCompare(b.code),
					);
					fetchedEquipmentOptionsByPartId[mapping.partId] =
						sortedEquipments.map((equipment) => ({
							value: toEquipmentOptionValue(equipment.id),
							label: `[Thiết bị] ${equipment.code} - ${equipment.name}`,
						}));
				}
			}

			for (const partId of partIds) {
				if (!fetchedEquipmentOptionsByPartId[partId]) {
					fetchedEquipmentOptionsByPartId[partId] = [];
				}
			}

			setEquipmentOptionsByPartId(fetchedEquipmentOptionsByPartId);

			// Transform API response to form schema
			const formattedData: MaterialFormSchema[] =
				response.result.acceptanceReports.map((item) => {
					const isSafetyAndWelfareMaterial =
						item.type === MaterialType.Material &&
						item.itemType === ItemType.SafetyAndWelfare;
					const isAssetMaterial =
						item.type === MaterialType.Material &&
						item.itemType === ItemType.Resource;
					const isQuotaMaterial =
						item.type === MaterialType.Material &&
						item.itemType === ItemType.QuotaMaterials;
					const defaultCategory = getDefaultCategoryByMaterialType(item.type);
					const defaultAdditionalCost = isSafetyAndWelfareMaterial
						? AdditionalCost.OtherMaterial
						: getDefaultAdditionalCostByMaterialType(item.type);

					return {
						...MATERIAL_FORM_DEFAULT,
						id: item.partId ?? item.materialId ?? '',
						acceptanceReportItemId: item.reportItemId || undefined,
						materialOrPartId: item.partId ?? item.materialId ?? '',
						partType: item.partType ?? null,
						materialCode: item.materialCode,
						unitOfMeasureName: item.unitOfMeasureName,
						type: item.type,
						itemType: item.itemType,
						quantityReceived: item.issuedQuantity,
						quantityExported: item.shippedQuantity,
						quantity: item.issuedQuantity + item.shippedQuantity,
						category: defaultCategory,
						additionalCostCategory: defaultAdditionalCost,
						showAdditionalCostDropdown: isSafetyAndWelfareMaterial,
						showAssetDropdown: isAssetMaterial,
						showContractLimitDropdown: isQuotaMaterial,
					};
				});

			form.setValue('materials', formattedData);
			setShowForm(true);
		} catch (error) {
			popup.error(error);
			setShowForm(false);
			form.setValue('materials', []);
			setFilePath('');
			setImportedItems([]);
			setEquipmentOptionsByPartId({});
			setOrderOrEquipmentOptionsByItemId({});
		} finally {
			setIsLoading(false);
		}
	};

	const handleSave = async (data: MaterialFormSchema[]) => {
		try {
			if (!productionOutputId) {
				popup.error('Không tìm thấy mã sản xuất');
				return;
			}

			// Transform form data to API request
			const requestData: CreateAcceptanceReportRequest = {
				productionOutputId,
				filePath: filePath ?? '',
				items: data.map((item) => {
					const resolvedCategory =
						item.category ?? getDefaultCategoryByMaterialType(item.type);

					// Map category to enum
					let materialsIncludedInContractRevenue: number =
						MaterialsIncludedInContractRevenue.None;
					if (item.showCategoryDropdown && resolvedCategory) {
						materialsIncludedInContractRevenue = resolvedCategory;
					}

					const processGroupId =
						item.showCategoryDropdown && resolvedCategory
							? item.categoryProcessGroup || null
							: null;

					const isSafetyAndWelfareMaterial =
						item.type === MaterialType.Material &&
						item.itemType === ItemType.SafetyAndWelfare;
					const resolvedAdditionalCostCategory =
						item.showAdditionalCostDropdown && isSafetyAndWelfareMaterial
							? AdditionalCost.OtherMaterial
							: item.additionalCostCategory;

					// Map additional cost to enum
					let additionalCost: number = AdditionalCost.None;
					if (
						item.showAdditionalCostDropdown &&
						resolvedAdditionalCostCategory
					) {
						additionalCost = resolvedAdditionalCostCategory;
					}
					const otherMaterialDetail =
						item.showAdditionalCostDropdown &&
						resolvedAdditionalCostCategory === AdditionalCost.OtherMaterial
							? (item.otherMaterialDetail ?? OtherMaterialDetail.None)
							: OtherMaterialDetail.None;

					const categoryProductionOrderId =
						item.showCategoryDropdown &&
						resolvedCategory === MaterialsIncludedInContractRevenue.Maintain
							? normalizeProductionOrderId(item.categoryProductionOrderId)
							: null;
					const categoryEquipmentId =
						item.showCategoryDropdown &&
						resolvedCategory === MaterialsIncludedInContractRevenue.Maintain
							? (item.categoryEquipmentId ?? null)
							: null;

					const additionalSelection =
						item.showAdditionalCostDropdown &&
						(resolvedAdditionalCostCategory === AdditionalCost.Material ||
							resolvedAdditionalCostCategory === AdditionalCost.Maintain)
							? resolveSelectionIds(item.additionalCostProductionOrderId)
							: {
									productionOrderId: null,
									equipmentId: null,
								};

					// Map quota based material to enum
					let quotaBasedMaterial: number = QuotaBasedMaterial.None;
					let quotaBasedMaterialType: number = QuotaBasedMaterialType.New;
					let quotaBasedMaterialQuantities:
						| {
								type: number;
								quantity: number;
						  }[]
						| null = null;
					if (item.showContractLimitDropdown && item.contractLimitCategory) {
						quotaBasedMaterial = item.contractLimitCategory;
						const selectedSubCategories =
							item.contractLimitSubCategories &&
							item.contractLimitSubCategories.length > 0
								? item.contractLimitSubCategories.map((type) => Number(type))
								: item.contractLimitSubCategory != null
									? [item.contractLimitSubCategory]
									: [];

						quotaBasedMaterialType =
							selectedSubCategories[0] ?? QuotaBasedMaterialType.New;
						if (selectedSubCategories.length > 0) {
							quotaBasedMaterialQuantities = selectedSubCategories.map(
								(type) => ({
									type,
									quantity: parseQuantity(
										item.contractLimitBreakdown?.[String(type)],
									),
								}),
							);
						} else if (item.contractLimitQuantity != null) {
							quotaBasedMaterialQuantities = [
								{
									type: quotaBasedMaterialType,
									quantity: parseQuantity(item.contractLimitQuantity),
								},
							];
						}
					}

					// Map asset - both asset and assetMaterialQuantity are required
					const asset: number = item.showAssetDropdown
						? Asset.True
						: Asset.None;
					const assetMaterialQuantity: number = item.assetQuantity || 0;

					const receivedTypes =
						item.receivedTypes && item.receivedTypes.length > 0
							? item.receivedTypes
							: ['receipt_voucher'];
					const exportedTypes =
						item.exportedTypes && item.exportedTypes.length > 0
							? item.exportedTypes
							: ['production'];

					const issuedDetails: QuantityDetail[] = [];
					for (const key of receivedTypes) {
						const detailType =
							ISSUED_DETAIL_TYPE_BY_KEY[
								key as keyof typeof ISSUED_DETAIL_TYPE_BY_KEY
							];
						if (!detailType) continue;

						const quantity =
							receivedTypes.length > 1
								? parseQuantity(item.receivedBreakdown?.[key])
								: parseQuantity(item.quantityReceived);

						issuedDetails.push({
							type: detailType,
							quantity,
						});
					}

					const shippedDetails: QuantityDetail[] = [];
					for (const key of exportedTypes) {
						const detailType =
							SHIPPED_DETAIL_TYPE_BY_KEY[
								key as keyof typeof SHIPPED_DETAIL_TYPE_BY_KEY
							];
						if (!detailType) continue;

						const quantity =
							exportedTypes.length > 1
								? parseQuantity(item.exportedBreakdown?.[key])
								: parseQuantity(item.quantityExported);

						shippedDetails.push({
							type: detailType,
							quantity,
						});
					}

					return {
						acceptanceReportItemId: item.acceptanceReportItemId || null,
						materialId:
							item.type === MaterialType.Material
								? item.materialOrPartId || null
								: null,
						partId:
							item.type === MaterialType.SparePart
								? item.materialOrPartId || null
								: null,
						usageTime: 0,
						type: item.type || 1,
						itemType: item.itemType || 1,
						categoryProductionOrderId,
						categoryEquipmentId,
						additionalCostProductionOrderId:
							additionalSelection.productionOrderId,
						additionalCostEquipmentId: additionalSelection.equipmentId,
						issuedDetails,
						shippedDetails,
						materialsIncludedInContractRevenue,
						processGroupId,
						materialsIncludedInContractRevenueQuantity:
							item.categoryQuantity || 0,
						additionalCost,
						otherMaterialDetail,
						additionalCostQuantity: item.additionalCostQuantity || 0,
						quotaBasedMaterial,
						quotaBasedMaterialType,
						quotaBasedMaterialQuantity: item.contractLimitQuantity || 0,
						quotaBasedMaterialQuantities,
						asset,
						assetMaterialQuantity,
					};
				}),
			};

			// Call API to create acceptance report
			await api.post(API.PRODUCTION.ACCEPTANCE_REPORT.CREATE, requestData);

			onSave(data);
			setShowForm(false);
			form.reset();
			setFilePath('');
			setImportedItems([]);
			setEquipmentOptionsByPartId({});
			setOrderOrEquipmentOptionsByItemId({});
			setOpen(false);
			popup.success('Dữ liệu được lưu thành công');
		} catch (error) {
			popup.error(error);
		}
	};

	const handleFormSubmit = async (formData: {
		materials: MaterialFormSchema[];
	}) => {
		await handleSave(formData.materials);
	};

	return (
		<>
			{!showForm ? (
				<div className='flex flex-col gap-4'>
					<DataTableImport onImport={handleImport} isLoading={isLoading} />
				</div>
			) : (
				<FormProvider context={form} onSubmit={handleFormSubmit}>
					<MaterialImportForm
						onCancel={() => setShowForm(false)}
						processGroupOptions={processGroupOptions}
						productionOrderOptions={productionOrderOptions}
						orderOrEquipmentOptionsByItemId={orderOrEquipmentOptionsByItemId}
					/>
				</FormProvider>
			)}
		</>
	);
}
