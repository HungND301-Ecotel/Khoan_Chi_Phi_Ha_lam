import { ActionDialogProps, DataTable } from '@/components/datatable';
import { FormComboBox } from '@/components/form/form-combo-box';
import {
	FIXED_KEY_TYPE_OPTIONS,
	FixedKey,
} from '@/constants/fixed-key';
import { API } from '@/constants/api-enpoint';
import { MasterDataForm } from './action';
import { MASTER_DATA_COLUMNS } from './columns';
import { useMemo, useState } from 'react';

const ALL_FIXED_KEY_TYPE = 'all';

export function MasterDataPage() {
	const [selectedType, setSelectedType] = useState<string>(ALL_FIXED_KEY_TYPE);

	const query = useMemo(
		() => ({
			ignorePagination: true,
			...(selectedType !== ALL_FIXED_KEY_TYPE
				? { type: Number(selectedType) }
				: {}),
		}),
		[selectedType],
	);

	const typeOptions = useMemo(
		() => [
			{ value: ALL_FIXED_KEY_TYPE, label: 'Tất cả loại fixed key' },
			...FIXED_KEY_TYPE_OPTIONS.map((item) => ({
				value: item.value,
				label: item.label,
			})),
		],
		[],
	);

	return (
		<div className='flex flex-col gap-4'>
			<div className='flex flex-col gap-3 rounded-2xl border bg-white p-4 shadow-sm lg:flex-row lg:items-end lg:justify-between'>
				<div>
					<div className='text-sm font-semibold uppercase text-slate-700'>
						Bộ lọc hệ thống
					</div>
					<div className='mt-1 text-sm text-slate-500'>
						Lọc danh sách khoá cố định theo nhóm semantic đang được backend sử dụng.
					</div>
				</div>
				<div className='w-full max-w-md'>
					<FormComboBox
						label='Loại fixed key'
						placeholder='Chọn loại fixed key'
						value={selectedType}
						onValueChange={setSelectedType}
						options={typeOptions}
					/>
				</div>
			</div>

			<DataTable
				key={selectedType}
				url={API.CATALOG.FIXED_KEY.LIST}
				query={query}
				columns={MASTER_DATA_COLUMNS}
				filters={[
					{ key: 'code', label: 'Mã fixed key' },
					{ key: 'name', label: 'Tên fixed key' },
				]}
				onCreate={(props: ActionDialogProps<FixedKey>) => (
					<MasterDataForm {...props} />
				)}
				onDuplicate={(props: ActionDialogProps<FixedKey>) => (
					<MasterDataForm {...props} isDuplicate />
				)}
				onUpdate={(props: ActionDialogProps<FixedKey>) => (
					<MasterDataForm {...props} />
				)}
				showDeleteAction={false}
				showUtilityActions={false}
			/>
		</div>
	);
}