import { DataTable } from '@/components/datatable';
import { API } from '@/constants/api-enpoint';
import { FixedKeyForm } from '@/features/main/system/fixed-key/action';
import { CATALOG_FIXED_KEY_COLUMNS } from '@/features/main/system/fixed-key/columns';

export function MainSystemFixedKeyPage() {
	return (
		<DataTable
			url={API.SYSTEM.FIXED_KEY.LIST}
			columns={CATALOG_FIXED_KEY_COLUMNS}
			filters={[
				{ key: 'key', label: 'Key' },
				{ key: 'name', label: 'Tên khóa cấu hình' },
			]}
			onUpdate={(props) => <FixedKeyForm {...props} />}
			showCreateAction={false}
			showDeleteAction={false}
			showUtilityActions={false}
		/>
	);
}
