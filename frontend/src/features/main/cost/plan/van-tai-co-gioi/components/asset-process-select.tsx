import type { ReactNode } from 'react';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import {
	useWatch,
	type FieldValues,
	type UseFormReturn,
} from 'react-hook-form';
import {
	getAssetCategories,
	getProcessCategories,
	hasAssetPricing,
	MOTORIZED_CATEGORY_ORDER,
	processCategoryKey,
	type MotorizedPricingIndex,
} from '../pricing-index';
import { MOTORIZED_PLAN_CATEGORIES } from '../types';

type AssetProcessSelectProps<TForm extends FieldValues> = {
	form: UseFormReturn<TForm>;
	// `motorizedMonths.${i}` (Kế hoạch) hoặc `groups.${i}` (Vận hành sản xuất)
	basePath: string;
	assignmentCodeIds: string[];
	// Đơn giá gốc — để biết nhóm nào chưa có đơn giá
	pricingIndex: MotorizedPricingIndex;
	// Index đã gắn loại xe chọn tay cho nhóm chưa có đơn giá
	effectiveIndex: MotorizedPricingIndex;
	assignmentCodes: any[];
	processes: any[];
	// Phần nhập chi tiết (khối loại xe) của Nhóm vật tư — hiện ngay dưới ô Công đoạn
	renderDetails?: (assignmentCodeId: string) => ReactNode;
};

// Giá trị option: `${procId}::${loại xe}` — Công đoạn có đơn giá ở nhiều loại xe hiện thành nhiều option
const OPTION_SEP = '::';

const categoryName = (category: string) =>
	MOTORIZED_PLAN_CATEGORIES.find((c) => c.id === category)?.name || category;

// Chọn Nhóm vật tư rồi chọn Công đoạn (gom chung mọi loại xe) — Công đoạn có đơn giá ở loại xe nào
// thì dòng nhập hiện ở khối loại xe đó; có đơn giá ở nhiều loại xe thì người dùng chọn đúng loại xe.
// Nhóm chưa có đơn giá: chọn loại xe tay, mọi Công đoạn VTCG.
export function AssetProcessSelect<TForm extends FieldValues>({
	form,
	basePath,
	assignmentCodeIds,
	pricingIndex,
	effectiveIndex,
	assignmentCodes,
	processes,
	renderDetails,
}: AssetProcessSelectProps<TForm>) {
	const overridesPath = `${basePath}.assetCategoryOverrides` as any;
	const overrides: Record<string, string> =
		(useWatch({ control: form.control, name: overridesPath }) as Record<
			string,
			string
		>) || {};
	const equipmentProcesses: Record<string, string[]> =
		(useWatch({
			control: form.control,
			name: `${basePath}.equipmentProcesses` as any,
		}) as Record<string, string[]>) || {};

	const findProcess = (procId: string) =>
		processes.find((p) => p.id === procId || p.value === procId);

	const processLabel = (procId: string) => {
		const proc = findProcess(procId);
		if (!proc) return 'Công đoạn không thuộc Vận tải cơ giới';
		const name = proc.name || proc.label || procId;
		return proc.code ? `${proc.code} - ${name}` : name;
	};

	if (assignmentCodeIds.length === 0) return null;

	return (
		<div className='space-y-3'>
			{assignmentCodeIds.map((acId) => {
				const asset = assignmentCodes.find(
					(a) => a.id === acId || a.value === acId,
				);
				const title = asset
					? asset.code
						? `${asset.code} - ${asset.name || asset.label}`
						: asset.name || asset.label
					: acId;
				const isUnpriced = !hasAssetPricing(pricingIndex, acId);
				const selectedIds = equipmentProcesses[acId] || [];

				// Công đoạn có đơn giá, kèm loại xe để biết dòng nhập rơi vào khối nào
				const options: MultiSelectOption[] = isUnpriced
					? processes.map((p) => ({
							value: p.id || p.value,
							label: processLabel(p.id || p.value),
						}))
					: MOTORIZED_CATEGORY_ORDER.flatMap((category) =>
							(pricingIndex[acId]?.[category] || [])
								.filter((procId) => !!findProcess(procId))
								.map((procId) => ({
									value: `${procId}${OPTION_SEP}${category}`,
									label: `${processLabel(procId)} (${categoryName(category)})`,
								})),
						);
				const toValue = (procId: string) => {
					if (isUnpriced) return procId;
					const category = getProcessCategories(effectiveIndex, acId, [
						procId,
					])[0];
					return category ? `${procId}${OPTION_SEP}${category}` : procId;
				};
				const selectedValues = selectedIds.map(toValue);
				// Công đoạn đã chọn mà không còn trong danh sách vẫn giữ để không mất dữ liệu
				selectedValues.forEach((value) => {
					if (!options.some((o) => o.value === value)) {
						options.push({
							value,
							label: processLabel(value.split(OPTION_SEP)[0]),
						});
					}
				});
				const toOption = (value: string) =>
					options.find((o) => o.value === value) || {
						value,
						label: processLabel(value.split(OPTION_SEP)[0]),
					};

				const handleChange = (selected: MultiSelectOption[]) => {
					// Chọn option loại xe khác của Công đoạn đã chọn → thay loại xe (option vừa chọn thắng)
					const previous = new Set(selectedValues);
					const picked = new Map<string, string | undefined>();
					[
						...selected.filter((o) => previous.has(o.value)),
						...selected.filter((o) => !previous.has(o.value)),
					].forEach((option) => {
						const [procId, category] = option.value.split(OPTION_SEP);
						picked.set(procId, category);
					});

					const processCategoriesPath = `${basePath}.processCategories` as any;
					const processCategories: Record<string, string> = {
						...((form.getValues(processCategoriesPath) as Record<
							string,
							string
						>) || {}),
					};
					picked.forEach((category, procId) => {
						const key = processCategoryKey(acId, procId);
						const pricedIn = MOTORIZED_CATEGORY_ORDER.filter((c) =>
							pricingIndex[acId]?.[c]?.includes(procId),
						);
						if (category && pricedIn.length > 1) {
							processCategories[key] = category;
						} else {
							delete processCategories[key];
						}
					});
					form.setValue(processCategoriesPath, processCategories as any, {
						shouldDirty: true,
					});
					form.setValue(
						`${basePath}.equipmentProcesses.${acId}` as any,
						[...picked.keys()] as any,
						{ shouldDirty: true },
					);
				};

				return (
					<div
						key={acId}
						className={
							isUnpriced
								? 'space-y-3 rounded-lg border border-amber-200 bg-amber-50 p-4'
								: 'space-y-3 rounded-lg border border-neutral-200 bg-white p-4'
						}
					>
						<div className='flex flex-wrap items-center justify-between gap-3'>
							<span className='text-sm font-semibold text-black'>
								{title}
								{isUnpriced && (
									<span className='ml-2 font-normal text-amber-800'>
										Chưa có đơn giá VTCG
									</span>
								)}
							</span>
							{isUnpriced && (
								<Select
									value={getAssetCategories(effectiveIndex, acId)[0]}
									onValueChange={(category) =>
										form.setValue(
											overridesPath,
											{ ...overrides, [acId]: category } as any,
											{ shouldDirty: true },
										)
									}
								>
									<SelectTrigger className='w-56 bg-white'>
										<SelectValue placeholder='Chọn loại xe' />
									</SelectTrigger>
									<SelectContent>
										{MOTORIZED_PLAN_CATEGORIES.map((category) => (
											<SelectItem key={category.id} value={category.id}>
												{category.name}
											</SelectItem>
										))}
									</SelectContent>
								</Select>
							)}
						</div>
						<MultiSelect
							label='Công đoạn sản xuất'
							placeholder='Chọn công đoạn sản xuất'
							options={options}
							values={selectedValues.map(toOption)}
							onValuesChange={handleChange}
						/>
						{renderDetails?.(acId)}
					</div>
				);
			})}
		</div>
	);
}
