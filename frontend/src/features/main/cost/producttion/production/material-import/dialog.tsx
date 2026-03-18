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
	MaterialType,
	MaterialsIncludedInContractRevenue,
	QuotaBasedMaterial,
	QuotaBasedMaterialType,
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
};

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

			// Transform API response to form schema
			const formattedData: MaterialFormSchema[] =
				response.result.acceptanceReports.map((item) => {
					const defaultCategory = getDefaultCategoryByMaterialType(item.type);
					const defaultAdditionalCost = getDefaultAdditionalCostByMaterialType(
						item.type,
					);

					return {
						...MATERIAL_FORM_DEFAULT,
						id: item.materialOrPartId,
						acceptanceReportItemId: item.reportItemId || undefined,
						materialOrPartId: item.materialOrPartId,
						materialCode: item.materialCode,
						unitOfMeasureName: item.unitOfMeasureName,
						type: item.type,
						quantityReceived: item.issuedQuantity,
						quantityExported: item.shippedQuantity,
						quantity: item.issuedQuantity + item.shippedQuantity,
						category: defaultCategory,
						additionalCostCategory: defaultAdditionalCost,
					};
				});

			form.setValue('materials', formattedData);
			setShowForm(true);
		} catch (error) {
			const message =
				error instanceof Error ? error.message : 'Lỗi khi xử lý file Excel';
			popup.error(message);
			setShowForm(false);
		} finally {
			setIsLoading(false);
		}
	};

	const handleSave = async (data: MaterialFormSchema[]) => {
		try {
			if (!productionOutputId || !filePath) {
				popup.error('Thiếu thông tin cần thiết');
				return;
			}

			// Transform form data to API request
			const requestData: CreateAcceptanceReportRequest = {
				productionOutputId,
				filePath,
				items: data.map((item) => {
					// Map category to enum
					let materialsIncludedInContractRevenue: number =
						MaterialsIncludedInContractRevenue.None;
					if (item.showCategoryDropdown && item.category) {
						materialsIncludedInContractRevenue = item.category;
					}

					const processGroupId =
						item.showCategoryDropdown && item.category
							? item.categoryProcessGroup || null
							: null;

					// Map additional cost to enum
					let additionalCost: number = AdditionalCost.None;
					if (item.showAdditionalCostDropdown && item.additionalCostCategory) {
						additionalCost = item.additionalCostCategory;
					}

					// Map quota based material to enum
					let quotaBasedMaterial: number = QuotaBasedMaterial.None;
					let quotaBasedMaterialType: number = QuotaBasedMaterialType.New;
					if (item.showContractLimitDropdown && item.contractLimitCategory) {
						quotaBasedMaterial = item.contractLimitCategory;
						// Only set type if category requires it (MineSupport or SupportAccessories)
						if (
							(quotaBasedMaterial === QuotaBasedMaterial.MineSupport ||
								quotaBasedMaterial === QuotaBasedMaterial.SupportAccessories) &&
							item.contractLimitSubCategory
						) {
							quotaBasedMaterialType = item.contractLimitSubCategory;
						}
					}

					// Map asset - both asset and assetMaterialQuantity are required
					const asset: number = item.showAssetDropdown
						? Asset.True
						: Asset.None;
					const assetMaterialQuantity: number = item.assetQuantity || 0;

					return {
						acceptanceReportItemId: item.acceptanceReportItemId || null,
						materialOrPartId: item.materialOrPartId || '',
						type: item.type || 0,
						issuedQuantity: item.quantityReceived || 0,
						shippedQuantity: item.quantityExported || 0,
						materialsIncludedInContractRevenue,
						processGroupId,
						materialsIncludedInContractRevenueQuantity:
							item.categoryQuantity || 0,
						additionalCost,
						additionalCostQuantity: item.additionalCostQuantity || 0,
						quotaBasedMaterial,
						quotaBasedMaterialType,
						quotaBasedMaterialQuantity: item.contractLimitQuantity || 0,
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
			setOpen(false);
			popup.success('Dữ liệu được lưu thành công');
		} catch (error) {
			const message =
				error instanceof Error ? error.message : 'Lỗi khi lưu dữ liệu';
			popup.error(message);
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
					<DataTableImport
						onImport={handleImport}
						isLoading={isLoading}
					/>
				</div>
			) : (
				<FormProvider context={form} onSubmit={handleFormSubmit}>
					<MaterialImportForm
						onCancel={() => setShowForm(false)}
						processGroupOptions={processGroupOptions}
					/>
				</FormProvider>
			)}
		</>
	);
}
