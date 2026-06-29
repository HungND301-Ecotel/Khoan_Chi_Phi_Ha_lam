import { ClientPagination } from '@/components/datatable/client-pagination';
import { ActionDialog } from '@/components/datatable/actions';
import { DataTableEditDialog } from '@/components/datatable/edit';
import { FormCheckBox } from '@/components/form/form-check-box';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMultiSelect } from '@/components/form/form-multi-select';
import { FormNumber, FormNumberInput } from '@/components/form/form-number';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
	DialogClose,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from '@/components/ui/dialog';
import {
	InputGroup,
	InputGroupAddon,
	InputGroupInput,
} from '@/components/ui/input-group';
import { Input } from '@/components/ui/input';
import {
	Popover,
	PopoverContent,
	PopoverTrigger,
} from '@/components/ui/popover';
import { Spinner } from '@/components/ui/spinner';
import {
	Table,
	TableBody,
	TableCell,
	TableHeader,
	TableRow,
} from '@/components/ui/table';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import { useDialog } from '@/data/dialog/dialog.hook';
import { cn } from '@/lib/utils';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import FilterListIcon from '@mui/icons-material/FilterList';
import SearchIcon from '@mui/icons-material/Search';
import { memo, useEffect, useMemo, useRef, useState } from 'react';
import { Path, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { MaterialFormSchema } from './schema';
import {
	AdditionalCost,
	ADDITIONAL_COST_OPTIONS,
	type AcceptanceReportEditorMode,
	CATEGORY_OPTIONS,
	CategoryAllocation,
	CONTRACT_LIMIT_OPTIONS,
	CONTRACT_LIMIT_SECONDARY_OPTIONS,
	EXPORTED_TYPE_OPTIONS,
	ItemType,
	type MaterialLookupOption,
	MaterialType,
	OTHER_MATERIAL_DETAIL_OPTIONS,
	type ProcessGroupOption,
	type ProductionOrderOption,
	QuotaBasedMaterial,
	RECEIVED_TYPE_OPTIONS,
} from './types';
import { createManualEditorRow } from './mappers';
import { FormDate } from '@/components/form/form-date';

// ── Form-level types ──────────────────────────────────────────────────────────

type MaterialsForm = { materials: MaterialFormSchema[] };

/** Extended schema that includes the runtime-only breakdown fields. */
type MaterialRowValues = MaterialFormSchema & {
	receivedTypes?: string[];
	exportedTypes?: string[];
	receivedBreakdown?: Record<string, number | string>;
	exportedBreakdown?: Record<string, number | string>;
	contractLimitSubCategories?: string[];
	contractLimitBreakdown?: Record<string, number | string>;
	categoryProcessGroupIds?: string[];
	categoryAssignmentCodeIds?: string[];
	categoryEquipmentIds?: string[];
	categoryAllocations?: CategoryAllocation[];
};

type MaterialsExtendedForm = { materials: MaterialRowValues[] };

type RowPath = Path<MaterialsExtendedForm>;

const PRODUCTION_ORDER_OPTION_PREFIX = 'production-order:';
const ASSIGNMENT_CODE_OPTION_PREFIX = 'assignment-code:';

function parseProductionOrderOptionId(value: string): string {
	return value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX)
		? value.slice(PRODUCTION_ORDER_OPTION_PREFIX.length)
		: value;
}

function parseAssignmentCodeOptionId(value: string): string {
	return value.startsWith(ASSIGNMENT_CODE_OPTION_PREFIX)
		? value.slice(ASSIGNMENT_CODE_OPTION_PREFIX.length)
		: value;
}

type AcceptanceReportEditorProps = {
	mode: AcceptanceReportEditorMode;
	onCancel?: () => void;
	processGroupOptions: ProcessGroupOption[];
	productionOrderOptions: ProductionOrderOption[];
	assignmentCodeOptions: ProductionOrderOption[];
	orderOrAssignmentCodeOptionsByItemId: Record<string, ProductionOrderOption[]>;
	materialLookupOptions?: MaterialLookupOption[];
	onMaterialAdded?: (option: MaterialLookupOption) => Promise<void> | void;
	unresolvedCount: number;
	onCreateUnresolved?: (index: number) => void;
};

type EditFilterKey =
	| 'category'
	| 'additional-cost'
	| 'contract-limit'
	| 'asset';

type EditFilterOption = {
	key: EditFilterKey;
	label: string;
};

const EDIT_FILTER_OPTIONS: EditFilterOption[] = [
	{
		key: 'category',
		label: 'Vật tư tính vào doanh thu khoán',
	},
	{
		key: 'additional-cost',
		label: 'Bổ sung chi phí',
	},
	{
		key: 'contract-limit',
		label: 'Vật tư theo hạn mức',
	},
	{
		key: 'asset',
		label: 'Tài sản',
	},
];

// ── Helpers ───────────────────────────────────────────────────────────────────

function splitAllocationQuantities(total: number, count: number): number[] {
	if (count <= 0) return [];
	const safeTotal = Number.isFinite(total) ? total : 0;
	const base = Number((safeTotal / count).toFixed(2));
	const quantities = Array.from({ length: count }, () => base);
	const allocated = quantities.reduce((sum, value) => sum + value, 0);
	quantities[count - 1] = Number((safeTotal - (allocated - base)).toFixed(2));
	return quantities;
}

function redistributeBreakdownQuantities(
	keys: string[],
	total: number,
	currentValues?: Record<string, number | string>,
): Record<string, number> {
	if (keys.length === 0) return {};

	const safeTotal = Number.isFinite(total) ? total : 0;
	const currentTotal = keys.reduce(
		(sum, key) => sum + (Number(currentValues?.[key]) || 0),
		0,
	);

	const nextValues =
		currentTotal > 0
			? keys.map((key) =>
					Number(
						(
							((Number(currentValues?.[key]) || 0) / currentTotal) *
							safeTotal
						).toFixed(2),
					),
				)
			: splitAllocationQuantities(safeTotal, keys.length);

	const allocatedTotal = nextValues.reduce((sum, value) => sum + value, 0);
	const delta = Number((safeTotal - allocatedTotal).toFixed(2));
	if (Math.abs(delta) >= 0.01) {
		nextValues[nextValues.length - 1] = Number(
			((nextValues[nextValues.length - 1] ?? 0) + delta).toFixed(2),
		);
	}

	return keys.reduce(
		(acc, key, index) => {
			acc[key] = nextValues[index] ?? 0;
			return acc;
		},
		{} as Record<string, number>,
	);
}

type ToolbarFilterOption = {
	key: string;
	label: string;
	className?: string;
};

const CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS =
	CONTRACT_LIMIT_SECONDARY_OPTIONS.map((option) => ({
		value: String(option.value),
		label: option.label,
	}));
const DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE =
	CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS[0]?.value ?? '';
const DEFAULT_CONTRACT_LIMIT_CATEGORY_VALUE =
	CONTRACT_LIMIT_OPTIONS[0]?.value ?? null;
const DEFAULT_OTHER_MATERIAL_DETAIL_VALUE =
	OTHER_MATERIAL_DETAIL_OPTIONS[0]?.value ?? null;
const DATATABLE_ACTION_SHADOW =
	'hover:shadow-[0px_2px_4px_-1px_rgba(0,0,0,0.2),0px_4px_5px_0px_rgba(0,0,0,0.14),0px_1px_10px_0px_rgba(0,0,0,0.12)] shadow-[0px_3px_1px_-2px_rgba(0,0,0,0.2),0px_2px_2px_0px_rgba(0,0,0,0.14),0px_1px_5px_0px_rgba(0,0,0,0.12)]';
const TOOLBAR_BUTTON_CLASS_NAME =
	'hover:bg-muted flex h-10 min-w-24 cursor-pointer bg-white shadow-[0px_3px_1px_-2px_rgba(0,0,0,0.2),0px_2px_2px_0px_rgba(0,0,0,0.14),0px_1px_5px_0px_rgba(0,0,0,0.12)]';

type CreateMaterialDialogContentProps = {
	selectedLookupValue: string;
	onSelectedLookupValueChange: (value: string) => void;
	materialLookupOptions: MaterialLookupOption[];
	newQuantityReceived: number;
	onNewQuantityReceivedChange: (value: number) => void;
	newQuantityExported: number;
	onNewQuantityExportedChange: (value: number) => void;
	isCreatingMaterial: boolean;
	onConfirm: () => Promise<boolean>;
};

function CreateMaterialDialogContent({
	selectedLookupValue,
	onSelectedLookupValueChange,
	materialLookupOptions,
	newQuantityReceived,
	onNewQuantityReceivedChange,
	newQuantityExported,
	onNewQuantityExportedChange,
	isCreatingMaterial,
	onConfirm,
}: CreateMaterialDialogContentProps) {
	const { setOpen } = useDialog();

	return (
		<>
			<div className='grid gap-6'>
				<div className='space-y-2'>
					<label className='text-sm font-medium text-slate-700'>Vật tư</label>
					<FormComboBox
						value={selectedLookupValue}
						onValueChange={onSelectedLookupValueChange}
						options={materialLookupOptions}
						placeholder='Chọn vật tư'
					/>
				</div>
				<div className='grid gap-4 sm:grid-cols-2'>
					<div className='space-y-2'>
						<label className='text-sm font-medium text-slate-700'>
							Số lượng lĩnh
						</label>
						<Input
							type='number'
							min={0}
							step='any'
							value={newQuantityReceived}
							className='h-11'
							onChange={(event) =>
								onNewQuantityReceivedChange(Number(event.target.value) || 0)
							}
						/>
					</div>
					<div className='space-y-2'>
						<label className='text-sm font-medium text-slate-700'>
							Số lượng xuất
						</label>
						<Input
							type='number'
							min={0}
							step='any'
							value={newQuantityExported}
							className='h-11'
							onChange={(event) =>
								onNewQuantityExportedChange(Number(event.target.value) || 0)
							}
						/>
					</div>
				</div>
			</div>
			<DialogFooter className='bg-muted sticky bottom-0 mt-auto py-4'>
				<Button
					type='button'
					variant='outline'
					className='h-8 w-24 bg-[#dfe2ea] shadow-none hover:bg-[#dfe2ea] hover:shadow-sm'
					onClick={() => setOpen(false)}
					disabled={isCreatingMaterial}
				>
					Huỷ
				</Button>
				<Button
					type='button'
					variant='default'
					className='h-8 w-24 shadow-none hover:shadow-none'
					disabled={isCreatingMaterial}
					onClick={async () => {
						const created = await onConfirm();
						if (created) {
							setOpen(false);
						}
					}}
				>
					{isCreatingMaterial ? <Spinner /> : 'Xác nhận'}
				</Button>
			</DialogFooter>
		</>
	);
}

// ── Main form ─────────────────────────────────────────────────────────────────

export function AcceptanceReportEditor({
	mode,
	onCancel,
	processGroupOptions,
	productionOrderOptions,
	assignmentCodeOptions,
	orderOrAssignmentCodeOptionsByItemId,
	materialLookupOptions = [],
	onMaterialAdded,
	unresolvedCount,
	onCreateUnresolved,
}: AcceptanceReportEditorProps) {
	const form = useFormContext<MaterialsForm>();
	const { fields, append, remove } = useFieldArray({
		control: form.control,
		name: 'materials',
	});
	const watchedMaterials = useWatch({
		control: form.control,
		name: 'materials',
	}) as MaterialRowValues[] | undefined;
	const [selectedEditFilterKeys, setSelectedEditFilterKeys] = useState<
		EditFilterKey[]
	>(EDIT_FILTER_OPTIONS.map((option) => option.key));
	const [searchKeyword, setSearchKeyword] = useState('');
	const [pageIndex, setPageIndex] = useState(0);
	const [pageSize, setPageSize] = useState(10);
	const [selectedRowFieldIds, setSelectedRowFieldIds] = useState<string[]>([]);
	const [selectedLookupValue, setSelectedLookupValue] = useState('');
	const [newQuantityReceived, setNewQuantityReceived] = useState(0);
	const [newQuantityExported, setNewQuantityExported] = useState(0);
	const [isCreatingMaterial, setIsCreatingMaterial] = useState(false);
	const popup = usePopup();
	const showRowSelection = true;
	const showMaterialToolbarActions = Boolean(
		materialLookupOptions.length > 0 && onMaterialAdded,
	);
	const toolbarFilterOptions = useMemo<ToolbarFilterOption[]>(
		() =>
			mode === 'import'
				? []
				: EDIT_FILTER_OPTIONS.map((option) => ({
						key: option.key,
						label: option.label,
					})),
		[mode],
	);
	const selectedToolbarFilterKeys = selectedEditFilterKeys;
	const visibleMaterialIndexes = useMemo(() => {
		const normalizedSearchKeyword = searchKeyword.trim().toLowerCase();
		return fields
			.map((_, index) => index)
			.filter((index) => {
				const item = watchedMaterials?.[index] ?? fields[index];
				if (!item) {
					return true;
				}

				const materialCode = item.materialCode?.toLowerCase() ?? '';
				const materialName = item.materialName?.toLowerCase() ?? '';
				const matchesSearch =
					!normalizedSearchKeyword ||
					materialCode.includes(normalizedSearchKeyword) ||
					materialName.includes(normalizedSearchKeyword);

				if (!matchesSearch) {
					return false;
				}
				if (mode === 'import') {
					return true;
				}
				return (
					selectedEditFilterKeys.length === 0 ||
					(selectedEditFilterKeys.includes('category') &&
						Boolean(item.showCategoryDropdown)) ||
					(selectedEditFilterKeys.includes('additional-cost') &&
						Boolean(item.showAdditionalCostDropdown)) ||
					(selectedEditFilterKeys.includes('contract-limit') &&
						Boolean(item.showContractLimitDropdown)) ||
					(selectedEditFilterKeys.includes('asset') &&
						Boolean(item.showAssetDropdown))
				);
			});
	}, [fields, mode, searchKeyword, selectedEditFilterKeys, watchedMaterials]);
	const pageCount = Math.ceil(visibleMaterialIndexes.length / pageSize);
	const safePageIndex =
		pageCount === 0 ? 0 : Math.min(pageIndex, Math.max(pageCount - 1, 0));
	const paginatedMaterialIndexes = useMemo(() => {
		const start = safePageIndex * pageSize;
		return visibleMaterialIndexes.slice(start, start + pageSize);
	}, [pageSize, safePageIndex, visibleMaterialIndexes]);

	useEffect(() => {
		if (Object.keys(form.formState.errors).length > 0) {
			console.log('Form validation errors:', form.formState.errors);
		}
	}, [form.formState.errors]);

	const toggleToolbarFilter = (key: string, checked: boolean) => {
		setSelectedEditFilterKeys((prev) => {
			const typedKey = key as EditFilterKey;
			if (checked) {
				return prev.includes(typedKey) ? prev : [...prev, typedKey];
			}
			return prev.filter((item) => item !== typedKey);
		});
		setPageIndex(0);
	};

	const visibleSelectedCount = visibleMaterialIndexes.reduce((count, index) => {
		const fieldId = fields[index]?.id;
		return fieldId && selectedRowFieldIds.includes(fieldId) ? count + 1 : count;
	}, 0);
	const allVisibleSelected =
		visibleMaterialIndexes.length > 0 &&
		visibleSelectedCount === visibleMaterialIndexes.length;
	const someVisibleSelected =
		visibleSelectedCount > 0 &&
		visibleSelectedCount < visibleMaterialIndexes.length;

	const toggleAllVisibleRows = (checked: boolean) => {
		if (checked) {
			const visibleFieldIds = visibleMaterialIndexes
				.map((index) => fields[index]?.id)
				.filter((fieldId): fieldId is string => Boolean(fieldId));
			setSelectedRowFieldIds((prev) =>
				Array.from(new Set([...prev, ...visibleFieldIds])),
			);
			return;
		}

		const visibleFieldIdSet = new Set(
			visibleMaterialIndexes
				.map((index) => fields[index]?.id)
				.filter((fieldId): fieldId is string => Boolean(fieldId)),
		);
		setSelectedRowFieldIds((prev) =>
			prev.filter((fieldId) => !visibleFieldIdSet.has(fieldId)),
		);
	};

	const handleCreateMaterial = async () => {
		const selectedOption = materialLookupOptions.find(
			(option) => option.value === selectedLookupValue,
		);
		if (!selectedOption) {
			popup.error('Vui lòng chọn vật tư cần tạo mới.');
			return false;
		}

		try {
			setIsCreatingMaterial(true);
			await onMaterialAdded?.(selectedOption);
			append(
				createManualEditorRow(
					selectedOption,
					Number(newQuantityReceived) || 0,
					Number(newQuantityExported) || 0,
				),
			);
			setSelectedLookupValue('');
			setNewQuantityReceived(0);
			setNewQuantityExported(0);
			setSearchKeyword('');
			setPageIndex(Math.floor(fields.length / pageSize));
			popup.success('Đã tạo mới vật tư trong biên bản nghiệm thu.');
			return true;
		} catch (error) {
			popup.error(error);
			return false;
		} finally {
			setIsCreatingMaterial(false);
		}
	};

	const handleDeleteSelectedRows = () => {
		const selectedIndexes = fields
			.map((field, index) =>
				selectedRowFieldIds.includes(field.id) ? index : null,
			)
			.filter((index): index is number => index != null)
			.sort((a, b) => b - a);

		if (selectedIndexes.length === 0) {
			popup.error('Vui lòng chọn ít nhất một dòng để xoá.');
			return;
		}

		try {
			remove(selectedIndexes);
			setSelectedRowFieldIds([]);
			popup.success(`Đã xoá thành công ${selectedIndexes.length} dòng vật tư.`);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<div className='flex h-full flex-col gap-6'>
			{mode === 'import' && unresolvedCount > 0 && (
				<div className='rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800'>
					Có {unresolvedCount} dòng chưa tồn tại trong danh mục. Vui lòng tạo
					mới từng dòng trước khi lưu phiếu nghiệm thu.
				</div>
			)}
			<div className='rounded-lg border border-slate-200 bg-slate-50 p-3'>
				<div className='flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between'>
					<div className='flex flex-1 flex-col gap-3 lg:flex-row lg:items-center'>
						{showMaterialToolbarActions && (
							<div className='flex shrink-0 flex-wrap items-center gap-3'>
								<DialogProvider>
									<DataTableEditDialog
										type='Tạo mới'
										crumb='vật tư'
										trigger={
											<Button
												type='button'
												variant='warning'
												className={cn(DATATABLE_ACTION_SHADOW, 'min-w-24')}
											>
												<span className='font-medium'>Tạo mới</span>
												<AddIcon fontSize='small' />
											</Button>
										}
									>
										<CreateMaterialDialogContent
											selectedLookupValue={selectedLookupValue}
											onSelectedLookupValueChange={setSelectedLookupValue}
											materialLookupOptions={materialLookupOptions}
											newQuantityReceived={newQuantityReceived}
											onNewQuantityReceivedChange={setNewQuantityReceived}
											newQuantityExported={newQuantityExported}
											onNewQuantityExportedChange={setNewQuantityExported}
											isCreatingMaterial={isCreatingMaterial}
											onConfirm={handleCreateMaterial}
										/>
									</DataTableEditDialog>
								</DialogProvider>
								<DialogProvider>
									<ActionDialog
										className='min-h-auto sm:max-w-md'
										trigger={
											<Button
												type='button'
												variant='destructive'
												className={cn(DATATABLE_ACTION_SHADOW, 'min-w-24')}
												disabled={selectedRowFieldIds.length === 0}
											>
												<span className='font-medium'>
													Xoá ({selectedRowFieldIds.length})
												</span>
												<DeleteIcon fontSize='small' />
											</Button>
										}
									>
										<DialogHeader>
											<DialogTitle className='text-center uppercase'>
												Xác nhận xóa
											</DialogTitle>
											<DialogDescription className='text-center'>
												Bạn có chắc chắn muốn xóa {selectedRowFieldIds.length}{' '}
												mục không?
											</DialogDescription>
										</DialogHeader>
										<DialogFooter className='flex w-full items-center sm:justify-center'>
											<DialogClose asChild>
												<Button variant='secondary' className='w-24'>
													Huỷ
												</Button>
											</DialogClose>
											<DialogClose asChild>
												<Button
													variant='destructive'
													onClick={handleDeleteSelectedRows}
													className='w-24'
												>
													Xoá
												</Button>
											</DialogClose>
										</DialogFooter>
									</ActionDialog>
								</DialogProvider>
							</div>
						)}
						<InputGroup className='w-full flex-1 rounded-sm border-[#d4d5d7] shadow-none hover:border-black'>
							<InputGroupInput
								placeholder='Tìm theo mã vật tư hoặc tên vật tư'
								value={searchKeyword}
								onChange={(event) => {
									setSearchKeyword(event.target.value);
									setPageIndex(0);
								}}
								className='peer bg-white'
							/>
							<InputGroupAddon align='inline-end'>
								<SearchIcon className='size-4' />
							</InputGroupAddon>
						</InputGroup>
					</div>
					{toolbarFilterOptions.length > 0 && (
						<Popover>
							<PopoverTrigger asChild>
								<Button variant='ghost' className={TOOLBAR_BUTTON_CLASS_NAME}>
									<FilterListIcon fontSize='small' />
									<span className='font-medium'>Lọc</span>
								</Button>
							</PopoverTrigger>
							<PopoverContent align='end' className='w-80 p-3'>
								<div className='space-y-3'>
									<div className='flex items-center justify-between gap-3'>
										<p className='text-sm font-medium text-slate-900'>Bộ lọc</p>
										{selectedToolbarFilterKeys.length > 0 && (
											<Button
												type='button'
												variant='ghost'
												size='sm'
												className='h-auto px-2 py-1 text-xs'
												onClick={() => {
													setSelectedEditFilterKeys([]);
													setPageIndex(0);
												}}
											>
												Xoá lọc
											</Button>
										)}
									</div>
									<div className='space-y-2'>
										{toolbarFilterOptions.map((option) => {
											const checked = selectedToolbarFilterKeys.includes(
												option.key as EditFilterKey,
											);
											return (
												<label
													key={option.key}
													className='flex cursor-pointer items-center gap-3 rounded-md px-2 py-1.5 hover:bg-slate-50'
												>
													<input
														type='checkbox'
														className='h-4 w-4 rounded border-slate-300'
														checked={checked}
														onChange={(event) =>
															toggleToolbarFilter(
																option.key,
																event.target.checked,
															)
														}
													/>
													{option.className ? (
														<span className={option.className}>
															{option.label}
														</span>
													) : (
														<span className='text-sm text-slate-700'>
															{option.label}
														</span>
													)}
												</label>
											);
										})}
									</div>
								</div>
							</PopoverContent>
						</Popover>
					)}
				</div>
			</div>
			<div className='min-h-0 flex-1 overflow-x-auto overflow-y-auto'>
				<div className='rounded-lg border shadow-sm'>
					<Table className='w-full'>
						<TableHeader className='bg-linear-to-r from-slate-50 to-slate-100'>
							<TableRow className='bg-linear-to-r from-slate-50 to-slate-100'>
								{showRowSelection && (
									<TableCell className='w-12 min-w-12 border-b-2 border-slate-200 px-3 py-4 text-center'>
										<Checkbox
											checked={
												allVisibleSelected ||
												(someVisibleSelected ? 'indeterminate' : false)
											}
											onCheckedChange={(checked) =>
												toggleAllVisibleRows(Boolean(checked))
											}
											className='[&_.lucide-check]:text-white'
										/>
									</TableCell>
								)}
								<TableCell className='sticky left-0 z-20 w-[5%] min-w-16 border-b-2 border-slate-200 bg-slate-100 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									STT
								</TableCell>
								<TableCell className='sticky left-16 z-20 w-[8%] min-w-32 border-b-2 border-slate-200 bg-slate-100 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
									Mã vật tư
								</TableCell>
								<TableCell className='w-[20%] min-w-60 border-b-2 border-slate-200 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
									Tên vật tư
								</TableCell>
								<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
									Đơn vị tính
								</TableCell>
								<TableCell className='w-[12%] min-w-40 border-b-2 border-slate-200 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
									Số chứng từ
								</TableCell>
								<TableCell className='w-[10%] min-w-36 border-b-2 border-slate-200 px-4 py-4 text-left text-sm font-semibold text-slate-700'>
									Ngày vào sổ
								</TableCell>
								<TableCell className='border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Số lượng lĩnh
								</TableCell>
								<TableCell className='border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Số lượng xuất
								</TableCell>
								<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Phân bổ
								</TableCell>
								<TableCell className='w-[18%] min-w-80 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Vật tư tính vào doanh thu khoán
								</TableCell>
								<TableCell className='w-[13%] min-w-44 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Bổ sung chi phí
								</TableCell>
								<TableCell className='w-[13%] min-w-44 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Vật tư theo hạn mức
								</TableCell>
								<TableCell className='w-[8%] min-w-28 border-b-2 border-slate-200 px-4 py-4 text-center text-sm font-semibold text-slate-700'>
									Tài sản
								</TableCell>
							</TableRow>
						</TableHeader>
						<TableBody>
							{paginatedMaterialIndexes.map((index, displayIndex) => (
								<MaterialImportRow
									key={fields[index].id}
									fieldId={fields[index].id}
									index={index}
									displayIndex={safePageIndex * pageSize + displayIndex + 1}
									mode={mode}
									processGroupOptions={processGroupOptions}
									productionOrderOptions={productionOrderOptions}
									assignmentCodeOptions={assignmentCodeOptions}
									orderOrAssignmentCodeOptionsByItemId={
										orderOrAssignmentCodeOptionsByItemId
									}
									selected={selectedRowFieldIds.includes(fields[index].id)}
									onSelectedChange={(checked) => {
										setSelectedRowFieldIds((prev) =>
											checked
												? prev.includes(fields[index].id)
													? prev
													: [...prev, fields[index].id]
												: prev.filter((item) => item !== fields[index].id),
										);
									}}
									onCreateUnresolved={onCreateUnresolved}
								/>
							))}
							{visibleMaterialIndexes.length === 0 && (
								<TableRow>
									<TableCell
										colSpan={showRowSelection ? 14 : 13}
										className='py-6 text-center text-sm text-slate-500'
									>
										Không có vật tư phù hợp với bộ lọc đã chọn.
									</TableCell>
								</TableRow>
							)}
						</TableBody>
					</Table>
				</div>
			</div>
			{visibleMaterialIndexes.length > 0 && (
				<ClientPagination
					totalItems={visibleMaterialIndexes.length}
					pageIndex={safePageIndex}
					pageSize={pageSize}
					onPageIndexChange={setPageIndex}
					onPageSizeChange={(nextPageSize) => {
						setPageSize(nextPageSize);
						setPageIndex(0);
					}}
				/>
			)}

			<DialogFooter className='bg-muted sticky bottom-0 z-20 mt-auto w-full px-10 py-4'>
				<Button
					type='button'
					variant='outline'
					className='h-8 w-24 bg-[#dfe2ea] shadow-none hover:bg-[#dfe2ea] hover:shadow-sm'
					onClick={() => onCancel?.()}
				>
					Huỷ
				</Button>
				<Button
					type='submit'
					variant='default'
					className='h-8 w-24 shadow-none hover:shadow-none'
					disabled={
						form.formState.isSubmitting ||
						(mode === 'import' && unresolvedCount > 0)
					}
				>
					{form.formState.isSubmitting ? <Spinner /> : 'Lưu'}
				</Button>
			</DialogFooter>
		</div>
	);
}

// ── resetCellFields ───────────────────────────────────────────────────────────

type ResetFieldSpec = {
	checkbox: string;
	dropdown?: string;
	dropdownSecondary?: string;
	dropdownTertiary?: string;
	dropdownQuaternary?: string;
	quantity?: string;
};

function resetCellFields(
	form: ReturnType<typeof useFormContext<MaterialsExtendedForm>>,
	basename: `materials.${number}`,
	fields: ResetFieldSpec[],
) {
	for (const field of fields) {
		form.setValue(`${basename}.${field.checkbox}` as RowPath, false as never);
		if (field.dropdown)
			form.setValue(`${basename}.${field.dropdown}` as RowPath, null as never);
		if (field.dropdownSecondary)
			form.setValue(
				`${basename}.${field.dropdownSecondary}` as RowPath,
				null as never,
			);
		if (field.dropdownTertiary)
			form.setValue(
				`${basename}.${field.dropdownTertiary}` as RowPath,
				null as never,
			);
		if (field.dropdownQuaternary)
			form.setValue(
				`${basename}.${field.dropdownQuaternary}` as RowPath,
				null as never,
			);
		if (field.quantity)
			form.setValue(`${basename}.${field.quantity}` as RowPath, null as never);
	}
}

// ── QuantityBreakdownInputs ───────────────────────────────────────────────────

type QuantityBreakdownInputsProps = {
	selectedKeys: string[];
	allOptions: { value: string; label: string }[];
	values: Record<string, number | string>;
	onChange: (key: string, val: number | string) => void;
	isValid: boolean;
	equalWidth?: boolean;
};

function QuantityBreakdownInputs({
	selectedKeys,
	allOptions,
	values,
	onChange,
	isValid,
	equalWidth = false,
}: QuantityBreakdownInputsProps) {
	return (
		<>
			{selectedKeys.map((key) => {
				const opt = allOptions.find((o) => o.value === key);
				return (
					<div
						key={key}
						className={cn(
							'flex flex-col gap-0.5',
							equalWidth ? 'min-w-0 flex-1' : 'w-24 shrink-0',
						)}
					>
						<label
							className='truncate text-[10px] leading-tight font-medium text-slate-500'
							title={opt?.label}
						>
							{opt?.label}
						</label>
						<Input
							type='number'
							min={0}
							step='any'
							value={values[key] ?? ''}
							onChange={(e) =>
								onChange(
									key,
									e.target.value === '' ? '' : Number(e.target.value),
								)
							}
							placeholder='0'
							className={cn(
								'text-center',
								!isValid && 'border-red-400 focus-visible:ring-red-300',
							)}
						/>
					</div>
				);
			})}
		</>
	);
}

// ── MaterialImportRow ─────────────────────────────────────────────────────────

const MaterialImportRow = memo(function MaterialImportRow({
	fieldId,
	index,
	displayIndex,
	mode,
	processGroupOptions,
	productionOrderOptions,
	assignmentCodeOptions,
	orderOrAssignmentCodeOptionsByItemId,
	selected,
	onSelectedChange,
	onCreateUnresolved,
}: {
	fieldId: string;
	index: number;
	displayIndex?: number;
	mode: AcceptanceReportEditorMode;
	processGroupOptions: ProcessGroupOption[];
	productionOrderOptions: ProductionOrderOption[];
	assignmentCodeOptions: ProductionOrderOption[];
	orderOrAssignmentCodeOptionsByItemId: Record<string, ProductionOrderOption[]>;
	selected: boolean;
	onSelectedChange: (checked: boolean) => void;
	onCreateUnresolved?: (index: number) => void;
}) {
	const form = useFormContext<MaterialsExtendedForm>();
	const basename = `materials.${index}` as const;
	const row = useWatch({
		control: form.control,
		name: basename as RowPath,
	}) as MaterialRowValues | undefined;
	const showCategoryDropdown = row?.showCategoryDropdown ?? false;
	const showAdditionalCostDropdown = row?.showAdditionalCostDropdown ?? false;
	const showContractLimitDropdown = row?.showContractLimitDropdown ?? false;
	const showAssetDropdown = row?.showAssetDropdown ?? false;
	const categoryType = row?.categoryType;
	const categoryProcessGroup = row?.categoryProcessGroup;
	const categoryProductionOrderId = row?.categoryProductionOrderId;
	const categoryAssignmentCodeId = row?.categoryAssignmentCodeId;
	const categoryEquipmentId =
		row?.categoryAssignmentCodeId ?? row?.categoryEquipmentId;
	const additionalCostCategory = row?.additionalCostCategory;
	const additionalCostAssignmentCodeId = row?.additionalCostAssignmentCodeId;
	const additionalCostProductionOrderId = row?.additionalCostProductionOrderId;
	const otherMaterialDetailValue = row?.otherMaterialDetail;
	const contractLimitCategoryValue = row?.contractLimitCategory;
	const quantityExported = row?.quantityExported;
	const quantityReceived = row?.quantityReceived;
	const categoryQuantity = row?.categoryQuantity;
	const additionalCostQuantity = row?.additionalCostQuantity;
	const contractLimitQuantity = row?.contractLimitQuantity;
	const contractLimitSubCategoriesValue = row?.contractLimitSubCategories as
		| string[]
		| undefined;
	const contractLimitBreakdown = row?.contractLimitBreakdown as
		| Record<string, number | string>
		| undefined;
	const assetQuantity = row?.assetQuantity;
	const materialTypeValue = row?.type;
	const itemTypeValue = row?.itemType;
	const materialOrPartId = row?.materialOrPartId;
	const resolutionStatus = row?.resolutionStatus;
	const unresolvedReason = row?.unresolvedReason;
	const documentNumber = row?.documentNumber ?? '';
	const receivedTypes = row?.receivedTypes as string[] | undefined;
	const exportedTypes = row?.exportedTypes as string[] | undefined;
	const receivedBreakdown = row?.receivedBreakdown as
		| Record<string, number | string>
		| undefined;
	const exportedBreakdown = row?.exportedBreakdown as
		| Record<string, number | string>
		| undefined;
	const materialCode = row?.materialCode;
	const materialName = row?.materialName;
	const unitOfMeasureName = row?.unitOfMeasureName;
	const isLongTermTracking = row?.isLongTermTracking ?? false;
	const showRowSelection = true;

	// ── Derived values ───────────────────────────────────────────────────────
	const isSparePartByAssignmentCode =
		materialTypeValue === MaterialType.SparePart &&
		itemTypeValue === ItemType.InContract;
	const orderOrAssignmentCodeOptions =
		(materialOrPartId
			? orderOrAssignmentCodeOptionsByItemId[materialOrPartId]
			: undefined) ?? productionOrderOptions;
	const productionOrderOnlyOptions = orderOrAssignmentCodeOptions.filter(
		(option) => option.value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX),
	);
	const categoryAssignmentCodeOptions = [
		{
			value: '__none__',
			label: '[Nhóm vật tư, tài sản] Không thuộc nhóm vật tư, tài sản',
		},
		...assignmentCodeOptions.map((option) => ({
			value: parseAssignmentCodeOptionId(option.value),
			label: option.label,
		})),
	];
	const categoryProductionOrderOptions = productionOrderOnlyOptions.map(
		(option) => ({
			value: parseProductionOrderOptionId(option.value),
			label: option.label,
		}),
	);
	const additionalCostAssignmentCodeOptions = [
		{
			value: '__none__',
			label: '[Nhóm vật tư, tài sản] Không thuộc nhóm vật tư, tài sản',
		},
		...assignmentCodeOptions.map((option) => ({
			value: parseAssignmentCodeOptionId(option.value),
			label: option.label,
		})),
	];
	const additionalCostProductionOrderOptions = (() => {
		if (additionalCostCategory === AdditionalCost.OtherMaterial) {
			return productionOrderOptions
				.filter((option) =>
					option.value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX),
				)
				.map((option) => ({
					value: parseProductionOrderOptionId(option.value),
					label: option.label,
				}));
		}
		return (
			isSparePartByAssignmentCode
				? productionOrderOnlyOptions
				: orderOrAssignmentCodeOptions
		)
			.filter((option) =>
				option.value.startsWith(PRODUCTION_ORDER_OPTION_PREFIX),
			)
			.map((option) => ({
				value: parseProductionOrderOptionId(option.value),
				label: option.label,
			}));
	})();

	const additionalCostOptionsByType = ADDITIONAL_COST_OPTIONS;

	const needsSecondComboBox =
		contractLimitCategoryValue === QuotaBasedMaterial.MineSupport ||
		contractLimitCategoryValue === QuotaBasedMaterial.SupportAccessories;
	const categoryNeedsProductionOrder = categoryType != null;
	const supportsRowLongTermTracking =
		showCategoryDropdown && categoryType === MaterialType.SparePart;
	const categoryProcessGroupOptions = processGroupOptions;
	const additionalCostNeedsAssignmentCode =
		additionalCostCategory === AdditionalCost.Material ||
		additionalCostCategory === AdditionalCost.Maintain;
	const additionalCostNeedsProductionOrder =
		additionalCostCategory === AdditionalCost.Material ||
		additionalCostCategory === AdditionalCost.Maintain ||
		additionalCostCategory === AdditionalCost.OtherMaterial;
	const additionalCostNeedsOtherMaterialDetail =
		additionalCostCategory === AdditionalCost.OtherMaterial;
	const contractLimitSelectedKeys =
		contractLimitSubCategoriesValue &&
		contractLimitSubCategoriesValue.length > 0
			? contractLimitSubCategoriesValue
			: [];
	const contractLimitSelectedTypes = contractLimitSelectedKeys.map((type) =>
		Number(type),
	);
	const contractLimitBreakdownTotal = contractLimitSelectedKeys.reduce(
		(acc, key) => acc + (Number(contractLimitBreakdown?.[key]) || 0),
		0,
	);

	// ── Helper to set a field value ──────────────────────────────────────────
	const set = <K extends keyof MaterialRowValues>(
		key: K,
		value: MaterialRowValues[K],
	) => form.setValue(`${basename}.${key}` as RowPath, value as never);

	// ── Initialize MultiSelect defaults on mount ─────────────────────────────
	useEffect(() => {
		if (!receivedTypes || receivedTypes.length === 0) {
			set('receivedTypes', [RECEIVED_TYPE_OPTIONS[0].value]);
		}
		if (!exportedTypes || exportedTypes.length === 0) {
			set('exportedTypes', [EXPORTED_TYPE_OPTIONS[0].value]);
		}
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, []);

	// ── Sync breakdown: redistribute total equally when selection changes ────
	useEffect(() => {
		if (!receivedTypes) return;
		const current = receivedBreakdown ?? {};
		if (
			Object.keys(current).sort().join(',') ===
			[...receivedTypes].sort().join(',')
		)
			return;
		const count = receivedTypes.length;
		const divided = count > 0 ? (Number(quantityReceived) || 0) / count : 0;
		const next: Record<string, number | string> = {};
		for (const key of receivedTypes) next[key] = divided;
		set('receivedBreakdown', next);
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [receivedTypes, receivedBreakdown, quantityReceived]);

	useEffect(() => {
		if (!exportedTypes) return;
		const current = exportedBreakdown ?? {};
		if (
			Object.keys(current).sort().join(',') ===
			[...exportedTypes].sort().join(',')
		)
			return;
		const count = exportedTypes.length;
		const divided = count > 0 ? (Number(quantityExported) || 0) / count : 0;
		const next: Record<string, number | string> = {};
		for (const key of exportedTypes) next[key] = divided;
		set('exportedBreakdown', next);
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [exportedTypes, exportedBreakdown, quantityExported]);

	// ── Checkbox toggle effects ──────────────────────────────────────────────
	const prevState = useRef({
		showCategoryDropdown: false,
		showAdditionalCostDropdown: false,
		showContractLimitDropdown: false,
		showAssetDropdown: false,
	});

	const prevDropdownState = useRef({
		categoryType: null as number | null | undefined,
		categoryProductionOrderId: null as string | null | undefined,
		categoryAssignmentCodeId: null as string | null | undefined,
		categoryEquipmentId: null as string | null | undefined,
		additionalCostCategory: null as number | null | undefined,
		additionalCostAssignmentCodeId: null as string | null | undefined,
		additionalCostProductionOrderId: null as string | null | undefined,
		otherMaterialDetail: null as number | null | undefined,
		contractLimitCategory: null as number | null | undefined,
		showCategoryDropdown: false,
		showAdditionalCostDropdown: false,
		showAssetDropdown: false,
	});

	useEffect(() => {
		if (!supportsRowLongTermTracking) {
			if (isLongTermTracking) {
				set('isLongTermTracking', false);
			}
		}
	}, [supportsRowLongTermTracking, isLongTermTracking]);

	useEffect(() => {
		const prev = prevState.current;

		const justEnabledCategory =
			!prev.showCategoryDropdown && showCategoryDropdown;
		const justEnabledAdditional =
			!prev.showAdditionalCostDropdown && showAdditionalCostDropdown;
		const justEnabledContractLimit =
			!prev.showContractLimitDropdown && showContractLimitDropdown;
		const justEnabledAsset = !prev.showAssetDropdown && showAssetDropdown;
		const justDisabledCategory =
			prev.showCategoryDropdown && !showCategoryDropdown;
		const justDisabledAdditional =
			prev.showAdditionalCostDropdown && !showAdditionalCostDropdown;
		const justDisabledContractLimit =
			prev.showContractLimitDropdown && !showContractLimitDropdown;
		const justDisabledAsset = prev.showAssetDropdown && !showAssetDropdown;

		if (justEnabledContractLimit) {
			resetCellFields(form, basename, [
				{
					checkbox: 'showCategoryDropdown',
					dropdown: 'categoryType',
					dropdownSecondary: 'categoryProductionOrderId',
					dropdownTertiary: 'categoryAssignmentCodeId',
					dropdownQuaternary: 'categoryEquipmentId',
					quantity: 'categoryQuantity',
				},
				{
					checkbox: 'showAdditionalCostDropdown',
					dropdown: 'additionalCostCategory',
					dropdownSecondary: 'additionalCostAssignmentCodeId',
					dropdownTertiary: 'additionalCostProductionOrderId',
					dropdownQuaternary: 'otherMaterialDetail',
					quantity: 'additionalCostQuantity',
				},
				{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
			]);
		} else if (justEnabledAsset) {
			resetCellFields(form, basename, [
				{
					checkbox: 'showCategoryDropdown',
					dropdown: 'categoryType',
					dropdownSecondary: 'categoryProductionOrderId',
					dropdownTertiary: 'categoryAssignmentCodeId',
					dropdownQuaternary: 'categoryEquipmentId',
					quantity: 'categoryQuantity',
				},
				{
					checkbox: 'showAdditionalCostDropdown',
					dropdown: 'additionalCostCategory',
					dropdownSecondary: 'additionalCostAssignmentCodeId',
					dropdownTertiary: 'additionalCostProductionOrderId',
					dropdownQuaternary: 'otherMaterialDetail',
					quantity: 'additionalCostQuantity',
				},
				{
					checkbox: 'showContractLimitDropdown',
					dropdown: 'contractLimitCategory',
					quantity: 'contractLimitQuantity',
				},
			]);
		} else if (justEnabledCategory) {
			if (prev.showContractLimitDropdown)
				resetCellFields(form, basename, [
					{
						checkbox: 'showContractLimitDropdown',
						dropdown: 'contractLimitCategory',
						quantity: 'contractLimitQuantity',
					},
				]);
			if (prev.showAssetDropdown)
				resetCellFields(form, basename, [
					{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
				]);
			if (
				categoryType != null &&
				categoryProductionOrderId == null &&
				categoryProductionOrderOptions.length > 0
			)
				set(
					'categoryProductionOrderId',
					categoryProductionOrderOptions[0].value,
				);
			if (categoryType != null && categoryAssignmentCodeId == null) {
				set(
					'categoryAssignmentCodeId',
					categoryAssignmentCodeOptions[0]?.value ?? null,
				);
			}
		} else if (justEnabledAdditional) {
			if (prev.showContractLimitDropdown)
				resetCellFields(form, basename, [
					{
						checkbox: 'showContractLimitDropdown',
						dropdown: 'contractLimitCategory',
						quantity: 'contractLimitQuantity',
					},
				]);
			if (prev.showAssetDropdown)
				resetCellFields(form, basename, [
					{ checkbox: 'showAssetDropdown', quantity: 'assetQuantity' },
				]);
			if (
				additionalCostCategory === AdditionalCost.Material ||
				additionalCostCategory === AdditionalCost.Maintain
			) {
				const hasValidAssignmentSelection =
					additionalCostAssignmentCodeId != null &&
					additionalCostAssignmentCodeOptions.some(
						(option) => option.value === additionalCostAssignmentCodeId,
					);
				if (!hasValidAssignmentSelection) {
					set(
						'additionalCostAssignmentCodeId',
						additionalCostAssignmentCodeOptions[0]?.value ?? null,
					);
				}
				const hasValidSelection =
					additionalCostProductionOrderId != null &&
					additionalCostProductionOrderOptions.some(
						(option) => option.value === additionalCostProductionOrderId,
					);
				if (!hasValidSelection) {
					set(
						'additionalCostProductionOrderId',
						additionalCostProductionOrderOptions[0]?.value ?? null,
					);
				}
			}
			if (
				additionalCostCategory === AdditionalCost.OtherMaterial &&
				otherMaterialDetailValue == null &&
				DEFAULT_OTHER_MATERIAL_DETAIL_VALUE != null
			) {
				set('otherMaterialDetail', DEFAULT_OTHER_MATERIAL_DETAIL_VALUE);
			}
		}

		if (justDisabledCategory) {
			set('categoryType', null);
			set('categoryProcessGroup', null);
			set('categoryProductionOrderId', null);
			set('categoryAssignmentCodeId', null);
			set('categoryEquipmentId', null);
			set('categoryQuantity', null);
		}
		if (justDisabledAdditional) {
			set('additionalCostCategory', null);
			set('additionalCostAssignmentCodeId', null);
			set('additionalCostProductionOrderId', null);
			set('otherMaterialDetail', null);
			set('additionalCostQuantity', null);
		}
		if (justDisabledContractLimit) {
			set('contractLimitCategory', null);
			set('contractLimitQuantity', null);
			set('contractLimitSubCategory', null);
			set('contractLimitSubCategories', []);
			set('contractLimitBreakdown', {});
		}
		if (justDisabledAsset) set('assetQuantity', null);

		prevState.current = {
			showCategoryDropdown,
			showAdditionalCostDropdown,
			showContractLimitDropdown,
			showAssetDropdown,
		};
	}, [
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
		categoryType,
		categoryProductionOrderId,
		categoryAssignmentCodeId,
		categoryEquipmentId,
		additionalCostCategory,
		additionalCostAssignmentCodeId,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		categoryProductionOrderOptions,
		categoryAssignmentCodeOptions,
		additionalCostAssignmentCodeOptions,
		additionalCostProductionOrderOptions,
		form,
		basename,
	]);

	useEffect(() => {
		if (!showCategoryDropdown) return;
		if (categoryType == null) {
			if (categoryProcessGroup != null) set('categoryProcessGroup', null);
			if (categoryAssignmentCodeId != null)
				set('categoryAssignmentCodeId', null);
			if (categoryEquipmentId != null) set('categoryEquipmentId', null);
			if (categoryProductionOrderId != null)
				set('categoryProductionOrderId', null);
			return;
		}
		const hasValidAssignmentCodeSelection =
			categoryAssignmentCodeId != null &&
			categoryAssignmentCodeOptions.some(
				(option) => option.value === categoryAssignmentCodeId,
			);
		if (!hasValidAssignmentCodeSelection) {
			set(
				'categoryAssignmentCodeId',
				categoryAssignmentCodeOptions[0]?.value ?? null,
			);
			return;
		}
		if ((categoryEquipmentId ?? null) !== (categoryAssignmentCodeId ?? null)) {
			set('categoryEquipmentId', categoryAssignmentCodeId ?? null);
		}
		if (
			categoryProductionOrderId == null &&
			categoryProductionOrderOptions.length > 0
		) {
			set('categoryProductionOrderId', categoryProductionOrderOptions[0].value);
		}
	}, [
		showCategoryDropdown,
		categoryType,
		categoryProcessGroup,
		categoryProductionOrderId,
		categoryAssignmentCodeId,
		categoryEquipmentId,
		categoryProductionOrderOptions,
		categoryAssignmentCodeOptions,
	]);

	useEffect(() => {
		if (!showCategoryDropdown || !supportsRowLongTermTracking) {
			if (categoryProcessGroup != null) {
				set('categoryProcessGroup', null);
			}
			return;
		}

		if (!isLongTermTracking) {
			if (categoryProcessGroup != null) {
				set('categoryProcessGroup', null);
			}
			return;
		}

		const hasValidProcessGroupSelection =
			categoryProcessGroup != null &&
			categoryProcessGroupOptions.some(
				(option: ProcessGroupOption) => option.value === categoryProcessGroup,
			);

		if (!hasValidProcessGroupSelection) {
			set(
				'categoryProcessGroup',
				categoryProcessGroupOptions[0]?.value ?? null,
			);
		}
	}, [
		showCategoryDropdown,
		supportsRowLongTermTracking,
		isLongTermTracking,
		categoryProcessGroup,
		categoryProcessGroupOptions,
	]);

	useEffect(() => {
		if (!showAdditionalCostDropdown) return;
		const requiresProductionOrder =
			additionalCostCategory === AdditionalCost.Material ||
			additionalCostCategory === AdditionalCost.Maintain ||
			additionalCostCategory === AdditionalCost.OtherMaterial;
		const requiresAssignmentCode =
			additionalCostCategory === AdditionalCost.Material ||
			additionalCostCategory === AdditionalCost.Maintain;
		const requiresOtherMaterialDetail =
			additionalCostCategory === AdditionalCost.OtherMaterial;
		if (!requiresAssignmentCode) {
			if (additionalCostAssignmentCodeId != null) {
				set('additionalCostAssignmentCodeId', null);
			}
		} else {
			const hasValidAssignmentSelection =
				additionalCostAssignmentCodeId != null &&
				additionalCostAssignmentCodeOptions.some(
					(option) => option.value === additionalCostAssignmentCodeId,
				);
			if (!hasValidAssignmentSelection) {
				set(
					'additionalCostAssignmentCodeId',
					additionalCostAssignmentCodeOptions[0]?.value ?? null,
				);
			}
		}
		if (!requiresProductionOrder) {
			if (additionalCostProductionOrderId != null)
				set('additionalCostProductionOrderId', null);
		} else {
			const hasValidSelection =
				additionalCostProductionOrderId != null &&
				additionalCostProductionOrderOptions.some(
					(option) => option.value === additionalCostProductionOrderId,
				);
			if (!hasValidSelection) {
				set(
					'additionalCostProductionOrderId',
					additionalCostProductionOrderOptions[0]?.value ?? null,
				);
			}
		}

		if (!requiresOtherMaterialDetail) {
			if (otherMaterialDetailValue != null) set('otherMaterialDetail', null);
			return;
		}

		if (
			otherMaterialDetailValue == null &&
			DEFAULT_OTHER_MATERIAL_DETAIL_VALUE != null
		) {
			set('otherMaterialDetail', DEFAULT_OTHER_MATERIAL_DETAIL_VALUE);
		}
	}, [
		showAdditionalCostDropdown,
		additionalCostCategory,
		additionalCostAssignmentCodeId,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		additionalCostAssignmentCodeOptions,
		additionalCostProductionOrderOptions,
	]);

	useEffect(() => {
		const prev = prevDropdownState.current;
		const categoryRequiresProductionOrder = categoryType != null;
		const additionalRequiresProductionOrder =
			additionalCostCategory === AdditionalCost.Material ||
			additionalCostCategory === AdditionalCost.Maintain ||
			additionalCostCategory === AdditionalCost.OtherMaterial;
		const additionalRequiresAssignmentCode =
			additionalCostCategory === AdditionalCost.Material ||
			additionalCostCategory === AdditionalCost.Maintain;
		const additionalRequiresOtherDetail =
			additionalCostCategory === AdditionalCost.OtherMaterial;
		const hasCategoryActiveNow = Boolean(
			showCategoryDropdown &&
			categoryType != null &&
			categoryAssignmentCodeId != null &&
			(!categoryRequiresProductionOrder || categoryProductionOrderId != null),
		);
		const hasAdditionalCostActiveNow = Boolean(
			showAdditionalCostDropdown &&
			additionalCostCategory &&
			(!additionalRequiresAssignmentCode ||
				additionalCostAssignmentCodeId != null) &&
			(!additionalRequiresProductionOrder ||
				additionalCostProductionOrderId != null) &&
			(!additionalRequiresOtherDetail || otherMaterialDetailValue != null),
		);
		const hasCategoryActiveBefore = Boolean(
			prev.showCategoryDropdown &&
			prev.categoryType != null &&
			prev.categoryAssignmentCodeId != null &&
			prev.categoryProductionOrderId != null,
		);
		const prevAdditionalRequiresProductionOrder =
			prev.additionalCostCategory === AdditionalCost.Material ||
			prev.additionalCostCategory === AdditionalCost.Maintain ||
			prev.additionalCostCategory === AdditionalCost.OtherMaterial;

		const prevAdditionalRequiresAssignmentCode =
			prev.additionalCostCategory === AdditionalCost.Material ||
			prev.additionalCostCategory === AdditionalCost.Maintain;

		const prevAdditionalRequiresOtherDetail =
			prev.additionalCostCategory === AdditionalCost.OtherMaterial;

		const hasAdditionalCostActiveBefore = Boolean(
			prev.showAdditionalCostDropdown &&
			prev.additionalCostCategory &&
			(!prevAdditionalRequiresAssignmentCode ||
				prev.additionalCostAssignmentCodeId != null) &&
			(!prevAdditionalRequiresProductionOrder ||
				prev.additionalCostProductionOrderId != null) &&
			(!prevAdditionalRequiresOtherDetail || prev.otherMaterialDetail != null),
		);
		const categoryJustReady = !hasCategoryActiveBefore && hasCategoryActiveNow;
		const additionalCostJustSelected =
			!hasAdditionalCostActiveBefore && hasAdditionalCostActiveNow;
		const contractLimitJustSelected =
			!prev.contractLimitCategory && contractLimitCategoryValue;
		const assetJustEnabled = !prev.showAssetDropdown && showAssetDropdown;
		const exportedQtyNow = Number(quantityExported) || 0;

		if (assetJustEnabled && exportedQtyNow > 0) {
			set('assetQuantity', exportedQtyNow);
		} else if (contractLimitJustSelected && exportedQtyNow > 0) {
			set('contractLimitQuantity', exportedQtyNow);
		} else if (categoryJustReady || additionalCostJustSelected) {
			if (hasCategoryActiveNow && hasAdditionalCostActiveNow) {
				set('categoryQuantity', exportedQtyNow / 2);
				set('additionalCostQuantity', exportedQtyNow / 2);
			} else if (hasCategoryActiveNow && categoryJustReady) {
				set('categoryQuantity', exportedQtyNow);
			} else if (hasAdditionalCostActiveNow && additionalCostJustSelected) {
				set('additionalCostQuantity', exportedQtyNow);
			}
		}

		prevDropdownState.current = {
			categoryType,
			categoryProductionOrderId,
			categoryAssignmentCodeId,
			categoryEquipmentId,
			additionalCostCategory,
			additionalCostAssignmentCodeId,
			additionalCostProductionOrderId,
			otherMaterialDetail: otherMaterialDetailValue,
			contractLimitCategory: contractLimitCategoryValue,
			showCategoryDropdown,
			showAdditionalCostDropdown,
			showAssetDropdown,
		};
	}, [
		categoryType,
		categoryProductionOrderId,
		categoryAssignmentCodeId,
		categoryEquipmentId,
		additionalCostCategory,
		additionalCostAssignmentCodeId,
		additionalCostProductionOrderId,
		otherMaterialDetailValue,
		contractLimitCategoryValue,
		quantityExported,
		showCategoryDropdown,
		showAdditionalCostDropdown,
		showContractLimitDropdown,
		showAssetDropdown,
	]);

	const prevContractLimitCategoryRef = useRef(contractLimitCategoryValue);
	const prevContractLimitSelectionSignatureRef = useRef('');
	useEffect(() => {
		if (prevContractLimitCategoryRef.current !== contractLimitCategoryValue) {
			set('contractLimitSubCategory', null);
			set('contractLimitSubCategories', []);
			set('contractLimitBreakdown', {});
			set('contractLimitQuantity', null);
			prevContractLimitSelectionSignatureRef.current = '';
			prevContractLimitCategoryRef.current = contractLimitCategoryValue;
		}
	}, [contractLimitCategoryValue]);

	useEffect(() => {
		if (!showContractLimitDropdown) return;
		if (contractLimitCategoryValue != null) return;
		if (DEFAULT_CONTRACT_LIMIT_CATEGORY_VALUE == null) return;
		set('contractLimitCategory', DEFAULT_CONTRACT_LIMIT_CATEGORY_VALUE);
	}, [showContractLimitDropdown, contractLimitCategoryValue]);

	useEffect(() => {
		if (!showContractLimitDropdown || !contractLimitCategoryValue) return;
		if (needsSecondComboBox) return;

		const exportedQty = Number(quantityExported) || 0;
		if (Math.abs((Number(contractLimitQuantity) || 0) - exportedQty) < 0.0001) {
			return;
		}

		set('contractLimitQuantity', exportedQty);
	}, [
		showContractLimitDropdown,
		contractLimitCategoryValue,
		needsSecondComboBox,
		quantityExported,
		contractLimitQuantity,
	]);

	useEffect(() => {
		if (!showContractLimitDropdown || !needsSecondComboBox) return;
		const selectedKeys =
			contractLimitSubCategoriesValue &&
			contractLimitSubCategoriesValue.length > 0
				? contractLimitSubCategoriesValue
				: [];

		if (selectedKeys.length === 0) {
			if (!DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE) return;
			set('contractLimitSubCategories', [
				DEFAULT_CONTRACT_LIMIT_SECONDARY_VALUE,
			]);
			return;
		}

		const selectedSignature = [...selectedKeys].sort().join(',');
		if (prevContractLimitSelectionSignatureRef.current === selectedSignature) {
			return;
		}

		const exportedQty = Number(quantityExported) || 0;
		const divided = exportedQty / selectedKeys.length;
		const nextBreakdown: Record<string, number | string> = {};
		for (const key of selectedKeys) {
			nextBreakdown[key] = divided;
		}
		set('contractLimitBreakdown', nextBreakdown);
		prevContractLimitSelectionSignatureRef.current = selectedSignature;
	}, [
		showContractLimitDropdown,
		needsSecondComboBox,
		contractLimitSubCategoriesValue,
		quantityExported,
	]);

	useEffect(() => {
		if (!showContractLimitDropdown || !needsSecondComboBox) return;
		if (
			Math.abs(
				(Number(contractLimitQuantity) || 0) - contractLimitBreakdownTotal,
			) < 0.0001
		) {
			return;
		}
		set('contractLimitQuantity', contractLimitBreakdownTotal);
	}, [
		showContractLimitDropdown,
		needsSecondComboBox,
		contractLimitQuantity,
		contractLimitBreakdownTotal,
	]);

	// ── Total validation ─────────────────────────────────────────────────────
	const totalQuantity = (() => {
		let total = 0;
		if (
			showCategoryDropdown &&
			categoryType != null &&
			categoryAssignmentCodeId != null &&
			(!categoryNeedsProductionOrder || categoryProductionOrderId != null) &&
			categoryQuantity != null
		)
			total += Number(categoryQuantity);
		if (
			showAdditionalCostDropdown &&
			additionalCostCategory &&
			(!additionalCostNeedsProductionOrder ||
				additionalCostProductionOrderId != null) &&
			(!additionalCostNeedsOtherMaterialDetail ||
				otherMaterialDetailValue != null) &&
			additionalCostQuantity != null
		)
			total += Number(additionalCostQuantity);
		if (
			showContractLimitDropdown &&
			contractLimitCategoryValue &&
			(!needsSecondComboBox || contractLimitSelectedTypes.length > 0) &&
			contractLimitQuantity != null
		)
			total += Number(contractLimitQuantity);
		if (showAssetDropdown && assetQuantity != null)
			total += Number(assetQuantity);
		return total;
	})();

	const exportedQty = Number(quantityExported) || 0;
	const receivedQty = Number(quantityReceived) || 0;
	const isValidTotal = Math.abs(totalQuantity - exportedQty) < 0.01;
	const isContractLimitBreakdownValid =
		!needsSecondComboBox ||
		Math.abs(contractLimitBreakdownTotal - exportedQty) < 0.01;

	// ── Breakdown change handlers ────────────────────────────────────────────
	const handleReceivedBreakdownChange = (key: string, val: number | string) =>
		set('receivedBreakdown', { ...(receivedBreakdown ?? {}), [key]: val });

	const handleExportedBreakdownChange = (key: string, val: number | string) =>
		set('exportedBreakdown', { ...(exportedBreakdown ?? {}), [key]: val });
	const handleReceivedQuantityChange = (value?: number) => {
		const nextValue = value ?? 0;
		set('quantityReceived', nextValue);
		set('quantity', nextValue + exportedQty);

		if (showReceivedBreakdown) {
			set(
				'receivedBreakdown',
				redistributeBreakdownQuantities(
					activeReceivedKeys,
					nextValue,
					receivedBreakdown,
				),
			);
		}
	};
	const handleExportedQuantityChange = (value?: number) => {
		const nextValue = value ?? 0;
		set('quantityExported', nextValue);
		set('quantity', receivedQty + nextValue);

		if (showExportedBreakdown) {
			set(
				'exportedBreakdown',
				redistributeBreakdownQuantities(
					activeExportedKeys,
					nextValue,
					exportedBreakdown,
				),
			);
		}
	};
	const handleContractLimitBreakdownChange = (
		key: string,
		val: number | string,
	) =>
		set('contractLimitBreakdown', {
			...(contractLimitBreakdown ?? {}),
			[key]: val,
		});

	const activeReceivedKeys = receivedTypes ?? [RECEIVED_TYPE_OPTIONS[0].value];
	const activeExportedKeys = exportedTypes ?? [EXPORTED_TYPE_OPTIONS[0].value];
	const showReceivedBreakdown = activeReceivedKeys.length > 1;
	const showExportedBreakdown = activeExportedKeys.length > 1;
	const isUnresolved = resolutionStatus === 'unresolved';
	const unresolvedInputClassName =
		'border-red-300 bg-slate-100 text-slate-500 focus-visible:ring-red-200';

	// ── Render ───────────────────────────────────────────────────────────────
	return (
		<>
			{isUnresolved ? (
				<TableRow className='transition-colors hover:bg-slate-50/50'>
					{showRowSelection && (
						<TableCell className='w-12 border-b border-slate-200 px-3 py-4 text-center'>
							<Checkbox
								checked={selected}
								onCheckedChange={(checked) =>
									onSelectedChange(Boolean(checked))
								}
								aria-label={`Select row ${fieldId}`}
								className='[&_.lucide-check]:text-white'
							/>
						</TableCell>
					)}
					<TableCell className='sticky left-0 z-20 w-[5%] min-w-16 border-b border-slate-200 bg-white px-4 py-4 text-center font-medium text-slate-700 shadow-xs hover:bg-slate-50'>
						{displayIndex ?? index + 1}
					</TableCell>
					<TableCell className='sticky left-16 z-20 w-[10%] min-w-32 border-b border-slate-200 bg-white px-4 py-4 shadow-xs hover:bg-slate-50'>
						<div className='flex flex-col gap-2'>
							<div className='flex items-center gap-2'>
								<Input
									readOnly
									value={materialCode || ''}
									className={cn(
										'font-medium text-slate-700',
										unresolvedInputClassName,
									)}
								/>
								{mode === 'import' && onCreateUnresolved && (
									<Button
										type='button'
										variant='outline'
										className='h-9 shrink-0 border-red-300 text-red-700 hover:bg-red-50'
										onClick={() => onCreateUnresolved(index)}
									>
										+
									</Button>
								)}
							</div>
							<span className='rounded bg-red-100 px-1.5 py-0.5 text-center text-[10px] font-medium text-red-700'>
								Chưa có trong danh mục
							</span>
							{unresolvedReason && (
								<p className='text-xs text-red-600'>{unresolvedReason}</p>
							)}
						</div>
					</TableCell>
					<TableCell className='w-[20%] min-w-60 border-b border-slate-200 px-4 py-4'>
						<Input
							readOnly
							value={materialName || ''}
							className={unresolvedInputClassName}
						/>
					</TableCell>
					<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
						<Input
							readOnly
							value={unitOfMeasureName || ''}
							className={unresolvedInputClassName}
						/>
					</TableCell>
					<TableCell className='w-[12%] min-w-40 border-b border-slate-200 px-4 py-4'>
						<Input
							value={documentNumber}
							onChange={(event) => set('documentNumber', event.target.value)}
							className='border-slate-300 bg-white'
						/>
					</TableCell>
					<TableCell className='w-[10%] min-w-36 border-b border-slate-200 px-4 py-4'>
						<FormDate
							control={form.control}
							name={`${basename}.postingDate` as RowPath}
						/>
					</TableCell>
					<TableCell className='border-b border-slate-200 px-4 py-4'>
						<div className='flex flex-col gap-2'>
							<div className='flex flex-col gap-0.5'>
								<label className='text-[10px] font-medium text-slate-500'>
									Tổng
								</label>
								<Input
									readOnly
									value={receivedQty}
									className={cn(
										'pointer-events-none cursor-not-allowed! text-center! text-slate-500!',
										unresolvedInputClassName,
									)}
								/>
							</div>
						</div>
					</TableCell>
					<TableCell className='border-b border-slate-200 px-4 py-4'>
						<div className='flex flex-col gap-2'>
							<div className='flex flex-col gap-0.5'>
								<label className='text-[10px] font-medium text-slate-500'>
									Tổng
								</label>
								<Input
									readOnly
									value={exportedQty}
									className={cn(
										'pointer-events-none cursor-not-allowed! text-center! text-slate-500!',
										unresolvedInputClassName,
									)}
								/>
							</div>
						</div>
					</TableCell>
					<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4 text-center'>
						<span className='text-sm text-slate-400'>-</span>
					</TableCell>
					<TableCell className='w-[18%] min-w-80 border-b border-slate-200 px-4 py-4 text-center'>
						<span className='text-sm text-slate-400'>-</span>
					</TableCell>
					<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4 text-center'>
						<span className='text-sm text-slate-400'>-</span>
					</TableCell>
					<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4 text-center'>
						<span className='text-sm text-slate-400'>-</span>
					</TableCell>
					<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4 text-center'>
						<span className='text-sm text-slate-400'>-</span>
					</TableCell>
				</TableRow>
			) : (
				<TableRow
					className={cn(
						'transition-colors',
						materialTypeValue === MaterialType.Material
							? 'bg-blue-50/40 hover:bg-blue-50/70'
							: materialTypeValue === MaterialType.SparePart
								? 'bg-amber-50/40 hover:bg-amber-50/70'
								: 'hover:bg-slate-50/50',
					)}
				>
					{showRowSelection && (
						<TableCell className='w-12 border-b border-slate-200 px-3 py-4 text-center'>
							<Checkbox
								checked={selected}
								onCheckedChange={(checked) =>
									onSelectedChange(Boolean(checked))
								}
								aria-label={`Select row ${fieldId}`}
								className='[&_.lucide-check]:text-white'
							/>
						</TableCell>
					)}
					{/* STT */}
					<TableCell className='sticky left-0 z-20 w-[5%] min-w-16 border-b border-slate-200 bg-white px-4 py-4 text-center font-medium text-slate-700 shadow-xs hover:bg-slate-50'>
						{displayIndex ?? index + 1}
					</TableCell>

					{/* Mã vật tư */}
					<TableCell className='sticky left-16 z-20 w-[10%] min-w-32 border-b border-slate-200 bg-white px-4 py-4 shadow-xs hover:bg-slate-50'>
						<Input
							readOnly
							value={materialCode || ''}
							className='border-slate-300 bg-slate-100 font-medium text-slate-500'
						/>
					</TableCell>

					{/* Tên vật tư */}
					<TableCell className='w-[20%] min-w-60 border-b border-slate-200 px-4 py-4'>
						<Input
							readOnly
							value={materialName || ''}
							className='border-slate-300 bg-slate-100 text-slate-500'
						/>
					</TableCell>

					{/* Đơn vị tính */}
					<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
						<Input
							readOnly
							value={unitOfMeasureName || ''}
							className='border-slate-300 bg-slate-100 text-slate-500'
						/>
					</TableCell>

					{/* Số chứng từ */}
					<TableCell className='w-[12%] min-w-40 border-b border-slate-200 px-4 py-4'>
						<Input
							value={documentNumber}
							onChange={(event) => set('documentNumber', event.target.value)}
							className='border-slate-300 bg-white'
						/>
					</TableCell>

					{/* Ngày vào sổ */}
					<TableCell className='w-[10%] min-w-36 border-b border-slate-200 px-4 py-4'>
						<FormDate
							control={form.control}
							name={`${basename}.postingDate` as RowPath}
						/>
					</TableCell>

					{/* Số lượng lĩnh */}
					<TableCell className='border-b border-slate-200 px-4 py-4'>
						{(() => {
							const subTotal = activeReceivedKeys.reduce(
								(acc, key) =>
									acc + (Number((receivedBreakdown ?? {})[key]) || 0),
								0,
							);
							const isReceivedValid =
								!showReceivedBreakdown ||
								Math.abs(subTotal - receivedQty) < 0.01;
							return (
								<div className='flex flex-col gap-2'>
									<div className='flex items-end gap-2'>
										{/* Total — always leftmost */}
										<div
											className={cn(
												'flex shrink-0 flex-col gap-0.5',
												showReceivedBreakdown ? 'w-24' : 'w-full',
											)}
										>
											<label className='text-[10px] font-medium text-slate-500'>
												Tổng
											</label>
											<FormNumberInput
												value={receivedQty}
												onValueChange={handleReceivedQuantityChange}
												className='border-slate-300 bg-white'
											/>
										</div>
										{/* Sub-inputs — only when >1 selected */}
										{showReceivedBreakdown && (
											<QuantityBreakdownInputs
												selectedKeys={activeReceivedKeys}
												allOptions={RECEIVED_TYPE_OPTIONS}
												values={receivedBreakdown ?? {}}
												onChange={handleReceivedBreakdownChange}
												isValid={isReceivedValid}
											/>
										)}
									</div>
									<FormMultiSelect
										control={form.control}
										name={`${basename}.receivedTypes` as RowPath}
										options={RECEIVED_TYPE_OPTIONS}
										placeholder='Chọn loại lĩnh'
									/>
									{showReceivedBreakdown && !isReceivedValid && (
										<p className='text-[11px] font-medium text-red-500'>
											Tổng các mục ({subTotal}) phải bằng số lượng lĩnh (
											{receivedQty})
										</p>
									)}
								</div>
							);
						})()}
					</TableCell>

					{/* Số lượng xuất */}
					<TableCell className='border-b border-slate-200 px-4 py-4'>
						{(() => {
							const subTotal = activeExportedKeys.reduce(
								(acc, key) =>
									acc + (Number((exportedBreakdown ?? {})[key]) || 0),
								0,
							);
							const isExportedValid =
								!showExportedBreakdown ||
								Math.abs(subTotal - exportedQty) < 0.01;
							return (
								<div className='flex flex-col gap-2'>
									<div className='flex items-end gap-2'>
										{/* Total — always leftmost */}
										<div
											className={cn(
												'flex shrink-0 flex-col gap-0.5',
												showExportedBreakdown ? 'w-24' : 'w-full',
											)}
										>
											<label className='text-[10px] font-medium text-slate-500'>
												Tổng
											</label>
											<FormNumberInput
												value={exportedQty}
												onValueChange={handleExportedQuantityChange}
												className='border-slate-300 bg-white'
											/>
										</div>
										{/* Sub-inputs — only when >1 selected */}
										{showExportedBreakdown && (
											<QuantityBreakdownInputs
												selectedKeys={activeExportedKeys}
												allOptions={EXPORTED_TYPE_OPTIONS}
												values={exportedBreakdown ?? {}}
												onChange={handleExportedBreakdownChange}
												isValid={isExportedValid}
											/>
										)}
									</div>
									<FormMultiSelect
										control={form.control}
										name={`${basename}.exportedTypes` as RowPath}
										options={EXPORTED_TYPE_OPTIONS}
										placeholder='Chọn loại xuất'
									/>
									{showExportedBreakdown && !isExportedValid && (
										<p className='text-[11px] font-medium text-red-500'>
											Tổng các mục ({subTotal}) phải bằng số lượng xuất (
											{exportedQty})
										</p>
									)}
								</div>
							);
						})()}
					</TableCell>

					{/* Phân bổ */}
					<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
						<div className='flex justify-center'>
							<Checkbox
								checked={supportsRowLongTermTracking && isLongTermTracking}
								disabled={!supportsRowLongTermTracking}
								onCheckedChange={(checked) =>
									set('isLongTermTracking', Boolean(checked))
								}
								className='[&_.lucide-check]:text-white'
							/>
						</div>
					</TableCell>

					{/* Vật tư tính vào doanh thu khoán */}
					<TableCell className='w-[18%] min-w-80 border-b border-slate-200 px-4 py-4'>
						<div className='flex flex-col items-center gap-3'>
							<div className='flex justify-center *:w-auto!'>
								<FormCheckBox
									control={form.control}
									name={`${basename}.showCategoryDropdown` as RowPath}
								/>
							</div>
							{!showCategoryDropdown &&
								!showAdditionalCostDropdown &&
								!showContractLimitDropdown &&
								!showAssetDropdown &&
								form.formState.errors?.materials?.[index]
									?.showCategoryDropdown && (
									<p className='text-xs text-red-600'>
										{
											form.formState.errors.materials[index]
												?.showCategoryDropdown?.message
										}
									</p>
								)}
							{showCategoryDropdown && (
								<>
									<div className='w-full'>
										<FormComboBox
											control={form.control}
											name={`${basename}.categoryType` as RowPath}
											options={CATEGORY_OPTIONS}
											placeholder='Chọn loại vật tư'
										/>
									</div>
									{showCategoryDropdown &&
										supportsRowLongTermTracking &&
										isLongTermTracking && (
											<div className='w-full'>
												<FormComboBox
													control={form.control}
													name={`${basename}.categoryProcessGroup` as RowPath}
													options={categoryProcessGroupOptions}
													placeholder='Chọn nhóm công đoạn sản xuất'
												/>
											</div>
										)}
									{categoryType != null && (
										<>
											<div className='w-full'>
												<FormComboBox
													control={form.control}
													name={
														`${basename}.categoryAssignmentCodeId` as RowPath
													}
													options={categoryAssignmentCodeOptions}
													placeholder='Chọn Nhóm vật tư, tài sản'
												/>
											</div>
											<div className='w-full'>
												<FormComboBox
													control={form.control}
													name={
														`${basename}.categoryProductionOrderId` as RowPath
													}
													options={categoryProductionOrderOptions}
													placeholder='Chọn lệnh sản xuất'
												/>
											</div>
											<div className='w-full'>
												<label className='mb-1.5 block text-xs font-medium text-slate-600'>
													Số lượng vật tư
												</label>
												<FormNumber
													control={form.control}
													name={`${basename}.categoryQuantity` as RowPath}
													placeholder='Nhập số lượng'
												/>
											</div>
											{categoryAssignmentCodeId != null &&
												(!categoryNeedsProductionOrder ||
													categoryProductionOrderId != null) && (
													<div className='w-full'>
														<div
															className={cn(
																'mt-1 text-xs',
																isValidTotal
																	? 'text-green-600'
																	: 'text-red-600',
															)}
														>
															Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
														</div>
													</div>
												)}
										</>
									)}
								</>
							)}
						</div>
					</TableCell>

					{/* Bổ sung chi phí */}
					<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
						<div className='flex flex-col items-center gap-3'>
							<div className='flex justify-center *:w-auto!'>
								<FormCheckBox
									control={form.control}
									name={`${basename}.showAdditionalCostDropdown` as RowPath}
								/>
							</div>
							{showAdditionalCostDropdown && (
								<>
									<div className='w-full'>
										<FormComboBox
											control={form.control}
											name={`${basename}.additionalCostCategory` as RowPath}
											options={additionalCostOptionsByType}
											placeholder='Chọn danh mục'
										/>
									</div>
									{additionalCostCategory && (
										<>
											{additionalCostNeedsAssignmentCode && (
												<div className='w-full'>
													<FormComboBox
														control={form.control}
														name={
															`${basename}.additionalCostAssignmentCodeId` as RowPath
														}
														options={additionalCostAssignmentCodeOptions}
														placeholder='Chọn Nhóm vật tư, tài sản'
													/>
												</div>
											)}
											{additionalCostNeedsOtherMaterialDetail && (
												<div className='w-full'>
													<FormComboBox
														control={form.control}
														name={`${basename}.otherMaterialDetail` as RowPath}
														options={OTHER_MATERIAL_DETAIL_OPTIONS}
														placeholder='Chọn loại vật tư'
													/>
												</div>
											)}
											{additionalCostNeedsProductionOrder && (
												<div className='w-full'>
													<FormComboBox
														control={form.control}
														name={
															`${basename}.additionalCostProductionOrderId` as RowPath
														}
														options={additionalCostProductionOrderOptions}
														placeholder='Chọn lệnh sản xuất'
													/>
												</div>
											)}
										</>
									)}
									{additionalCostCategory &&
										(!additionalCostNeedsAssignmentCode ||
											additionalCostAssignmentCodeId != null) &&
										(!additionalCostNeedsProductionOrder ||
											additionalCostProductionOrderId != null) &&
										(!additionalCostNeedsOtherMaterialDetail ||
											otherMaterialDetailValue != null) && (
											<div className='w-full'>
												<label className='mb-1.5 block text-xs font-medium text-slate-600'>
													Số lượng vật tư
												</label>
												<FormNumber
													control={form.control}
													name={`${basename}.additionalCostQuantity` as RowPath}
													placeholder='Nhập số lượng'
												/>
												<div
													className={cn(
														'mt-1 text-xs',
														isValidTotal ? 'text-green-600' : 'text-red-600',
													)}
												>
													Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
												</div>
											</div>
										)}
								</>
							)}
						</div>
					</TableCell>

					{/* Vật tư theo hạn mức */}
					<TableCell className='w-[13%] min-w-44 border-b border-slate-200 px-4 py-4'>
						<div className='flex flex-col items-center gap-3'>
							<div className='flex justify-center *:w-auto!'>
								<FormCheckBox
									control={form.control}
									name={`${basename}.showContractLimitDropdown` as RowPath}
								/>
							</div>
							{showContractLimitDropdown && (
								<>
									<div className='w-full'>
										<FormComboBox
											control={form.control}
											name={`${basename}.contractLimitCategory` as RowPath}
											options={CONTRACT_LIMIT_OPTIONS}
											placeholder='Chọn danh mục'
										/>
									</div>
									{contractLimitCategoryValue && needsSecondComboBox && (
										<>
											<div className='w-full'>
												<FormMultiSelect
													control={form.control}
													name={
														`${basename}.contractLimitSubCategories` as RowPath
													}
													options={CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS}
													placeholder='Chọn loại (Lĩnh mới/Tái sử dụng)'
												/>
											</div>
											{contractLimitSelectedKeys.length > 0 && (
												<div className='w-full space-y-2'>
													<div className='flex w-full items-end gap-2'>
														<QuantityBreakdownInputs
															selectedKeys={contractLimitSelectedKeys}
															allOptions={
																CONTRACT_LIMIT_SECONDARY_MULTI_OPTIONS
															}
															values={contractLimitBreakdown ?? {}}
															onChange={handleContractLimitBreakdownChange}
															isValid={isContractLimitBreakdownValid}
															equalWidth
														/>
													</div>
													{!isContractLimitBreakdownValid && (
														<p className='text-xs text-red-600'>
															Tổng lĩnh mới + lĩnh tái sử dụng phải bằng số
															lượng xuất ({contractLimitBreakdownTotal} /{' '}
															{exportedQty})
														</p>
													)}
												</div>
											)}
										</>
									)}
									{contractLimitCategoryValue &&
										(!needsSecondComboBox ||
											contractLimitSelectedTypes.length > 0) && (
											<div className='w-full'>
												{!needsSecondComboBox && (
													<>
														<label className='mb-1.5 block text-xs font-medium text-slate-600'>
															Số lượng vật tư
														</label>
														<FormNumber
															control={form.control}
															name={
																`${basename}.contractLimitQuantity` as RowPath
															}
															placeholder='Nhập số lượng'
														/>
													</>
												)}
												<div
													className={cn(
														'mt-1 text-xs',
														isValidTotal ? 'text-green-600' : 'text-red-600',
													)}
												>
													Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
												</div>
											</div>
										)}
								</>
							)}
						</div>
					</TableCell>

					{/* Tài sản */}
					<TableCell className='w-[8%] min-w-28 border-b border-slate-200 px-4 py-4'>
						<div className='flex flex-col items-center gap-3'>
							<div className='flex justify-center *:w-auto!'>
								<FormCheckBox
									control={form.control}
									name={`${basename}.showAssetDropdown` as RowPath}
								/>
							</div>
							{showAssetDropdown && (
								<div className='w-full'>
									<label className='mb-1.5 block text-xs font-medium text-slate-600'>
										Số lượng
									</label>
									<FormNumber
										control={form.control}
										name={`${basename}.assetQuantity` as RowPath}
										placeholder='Nhập số lượng'
									/>
									<div
										className={cn(
											'mt-1 text-xs',
											isValidTotal ? 'text-green-600' : 'text-red-600',
										)}
									>
										Tổng cộng: {exportedQty} Đã nhập: {totalQuantity}
									</div>
								</div>
							)}
						</div>
					</TableCell>
				</TableRow>
			)}
		</>
	);
});
