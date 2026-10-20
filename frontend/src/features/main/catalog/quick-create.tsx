import type { ActionDialogProps } from '@/components/datatable';
import type { QuickCreateOption } from '@/components/form/quick-create';
import { PERMISSIONS } from '@/constants/permissions';
import { FactorForm } from '@/features/main/catalog/adjustment/factor/actions';
import { DepartmentForm } from '@/features/main/catalog/department/actions';
import { ClampForm } from '@/features/main/catalog/parameter/clamp/actions';
import { StrengthForm } from '@/features/main/catalog/parameter/strength/actions';
import { PositionForm } from '@/features/main/catalog/position/actions';
import { ProcessGroupForm } from '@/features/main/catalog/process/group/action';
import { ProcessStepForm } from '@/features/main/catalog/process/step/action';
import { UnitForm } from '@/features/main/catalog/unit/actions';
import { usePermission } from '@/hooks/use-permission';
import { JSX } from 'react';

type CatalogQuickCreateConfig = {
	label: string;
	permission: string;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any -- form của từng danh mục có kiểu row riêng
	render: (props: ActionDialogProps<any>) => JSX.Element;
};

/**
 * Các danh mục có thể "Thêm mới" ngay trong dropdown của form danh mục khác.
 */
const CATALOG_QUICK_CREATE = {
	unit: {
		label: 'Đơn vị tính',
		permission: PERMISSIONS.CATALOG.UNIT.CREATE,
		render: (props) => <UnitForm {...props} single />,
	},
	department: {
		label: 'Đơn vị',
		permission: PERMISSIONS.CATALOG.DEPARTMENT.CREATE,
		render: (props) => <DepartmentForm {...props} single />,
	},
	position: {
		label: 'Chức vụ',
		permission: PERMISSIONS.CATALOG.POSITION.CREATE,
		render: (props) => <PositionForm {...props} single />,
	},
	processGroup: {
		label: 'Nhóm công đoạn sản xuất',
		permission: PERMISSIONS.CATALOG.PROCESS_GROUP.CREATE,
		render: (props) => <ProcessGroupForm {...props} single />,
	},
	processStep: {
		label: 'Công đoạn sản xuất',
		permission: PERMISSIONS.CATALOG.PROCESS_STEP.CREATE,
		render: (props) => <ProcessStepForm {...props} single />,
	},
	adjustmentFactor: {
		label: 'Hệ số điều chỉnh',
		permission: PERMISSIONS.CATALOG.ADJUSTMENT_FACTOR.CREATE,
		render: (props) => <FactorForm {...props} single />,
	},
	hardness: {
		label: 'Độ kiên cố than đá',
		permission: PERMISSIONS.CATALOG.PARAMETER_HARDNESS.CREATE,
		render: (props) => <StrengthForm {...props} single />,
	},
	stoneClampRatio: {
		label: 'Tỷ lệ đá kẹp',
		permission: PERMISSIONS.CATALOG.PARAMETER_STONE_CLAMP_RATIO.CREATE,
		render: (props) => <ClampForm {...props} single />,
	},
} satisfies Record<string, CatalogQuickCreateConfig>;

export type CatalogQuickCreateKey = keyof typeof CATALOG_QUICK_CREATE;

/**
 * Trả về cấu hình "Thêm mới" cho dropdown, hoặc undefined nếu người dùng
 * không có quyền tạo mới danh mục đó.
 * @param reload hàm nạp lại danh sách lựa chọn của dropdown
 */
export function useCatalogQuickCreate(
	key: CatalogQuickCreateKey,
	reload: () => unknown,
): QuickCreateOption | undefined {
	const { hasPermission } = usePermission();
	const config: CatalogQuickCreateConfig = CATALOG_QUICK_CREATE[key];

	if (!hasPermission(config.permission)) return undefined;

	return { label: config.label, render: config.render, reload };
}
