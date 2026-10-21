import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import {
	useWatch,
	type FieldValues,
	type UseFormReturn,
} from 'react-hook-form';
import {
	getAllowedProcessIds,
	type MotorizedPricingIndex,
} from '../pricing-index';
import type { MotorizedCategory } from '../types';

type CategoryProcessSelectProps<TForm extends FieldValues> = {
	form: UseFormReturn<TForm>;
	// Đường dẫn tới phần VTCG chứa equipmentProcesses: `motorizedMonths.${i}` (Kế hoạch) hoặc
	// `groups.${i}` (Vận hành sản xuất)
	basePath: string;
	assignmentCodeId: string;
	category: MotorizedCategory;
	pricingIndex: MotorizedPricingIndex;
	processes: any[];
};

// equipmentProcesses[Nhóm vật tư] dùng chung cho mọi khối loại xe — mỗi khối chỉ hiện/sửa phần
// Công đoạn có đơn giá ở loại xe của mình, giữ nguyên phần thuộc khối khác.
export function CategoryProcessSelect<TForm extends FieldValues>({
	form,
	basePath,
	assignmentCodeId,
	category,
	pricingIndex,
	processes,
}: CategoryProcessSelectProps<TForm>) {
	const path = `${basePath}.equipmentProcesses.${assignmentCodeId}` as any;
	const allProcessIds: string[] =
		(useWatch({ control: form.control, name: path }) as string[]) || [];
	const allowedIds = getAllowedProcessIds(
		pricingIndex,
		category,
		assignmentCodeId,
		allProcessIds,
	);

	const findProcess = (procId: string) =>
		processes.find((p) => p.id === procId || p.value === procId);

	const toOption = (procId: string): MultiSelectOption => {
		const proc = findProcess(procId);
		if (!proc) {
			// Đơn giá VTCG gắn với Công đoạn không thuộc danh sách VTCG (vd. gắn nhầm Đào lò đá)
			return { value: procId, label: 'Công đoạn không thuộc Vận tải cơ giới' };
		}
		const name = proc.name || proc.label || procId;
		return {
			value: procId,
			label: proc.code ? `${proc.code} - ${name}` : name,
		};
	};

	const selectedIds = allProcessIds.filter((id) => allowedIds.includes(id));
	// Chỉ cho chọn Công đoạn có trong danh sách; Công đoạn lạ đã chọn trước đó vẫn giữ để không mất dữ liệu
	const optionIds = allowedIds.filter(
		(id) => !!findProcess(id) || selectedIds.includes(id),
	);

	return (
		<MultiSelect
			label='Công đoạn sản xuất'
			placeholder='Chọn công đoạn sản xuất'
			options={optionIds.map(toOption)}
			values={selectedIds.map(toOption)}
			onValuesChange={(selected) => {
				const otherCategoryIds = allProcessIds.filter(
					(id) => !allowedIds.includes(id),
				);
				form.setValue(
					path,
					[
						...otherCategoryIds,
						...selected.map((option) => option.value),
					] as any,
					{ shouldDirty: true },
				);
			}}
		/>
	);
}
