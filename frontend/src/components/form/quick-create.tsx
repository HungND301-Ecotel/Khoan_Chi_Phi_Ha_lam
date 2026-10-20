import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditDialog } from '@/components/datatable/edit';
import type { UseDataTable } from '@/components/datatable/hook';
import { DialogContext } from '@/data/dialog/dialog-context';
import { MetaBreadcrumbOverrideContext } from '@/data/meta/meta-hook';
import {
	JSX,
	MutableRefObject,
	useCallback,
	useEffect,
	useMemo,
	useRef,
	useState,
} from 'react';

/**
 * Cấu hình "Thêm mới" ngay trong dropdown: mở form tạo mới của danh mục khác
 * (vd: Đơn vị tính) mà không phải rời trang hiện tại.
 */
export type QuickCreateOption = {
	/** Tên danh mục, vd: 'Đơn vị tính' */
	label: string;
	/** Form tạo mới của danh mục đó (cùng form dùng ở trang danh mục) */
	// eslint-disable-next-line @typescript-eslint/no-explicit-any -- form của từng danh mục có kiểu row riêng
	render: (props: ActionDialogProps<any>) => JSX.Element;
	/** Nạp lại danh sách lựa chọn của dropdown sau khi tạo xong */
	reload: () => unknown;
};

/** Giá trị đặc biệt của mục "Thêm mới" trong danh sách lựa chọn */
export const QUICK_CREATE_VALUE = '__quick_create__';

type Option = { value: string | number; label: string };

type Pending = { snapshot: Set<string>; created: boolean } | null;

export function useQuickCreate() {
	const [open, setOpen] = useState(false);
	const pendingRef = useRef<Pending>(null);

	const start = useCallback((options: Option[]) => {
		pendingRef.current = {
			snapshot: new Set(options.map((option) => String(option.value))),
			created: false,
		};
		setOpen(true);
	}, []);

	return { open, setOpen, pendingRef, start };
}

type QuickCreateHostProps = {
	quickCreate: QuickCreateOption;
	open: boolean;
	setOpen: (open: boolean) => void;
	pendingRef: MutableRefObject<Pending>;
	options: Option[];
	/** Chọn giá trị vừa tạo vào ô hiện tại */
	onSelect: (value: string | number) => void;
};

/**
 * Hộp thoại tạo mới + tự chọn giá trị vừa tạo khi danh sách lựa chọn được nạp lại.
 */
export function QuickCreateHost({
	quickCreate,
	open,
	setOpen,
	pendingRef,
	options,
	onSelect,
}: QuickCreateHostProps) {
	// Danh sách lựa chọn được nạp lại sau khi tạo: chọn giá trị mới xuất hiện
	useEffect(() => {
		const pending = pendingRef.current;
		if (!pending?.created) return;

		const added = options.find(
			(option) => !pending.snapshot.has(String(option.value)),
		);
		if (!added) return;

		pendingRef.current = null;
		onSelect(added.value);
	});

	// Form danh mục gọi data.refresh() sau khi tạo thành công
	const data = useMemo(
		() =>
			({
				refresh: async () => {
					if (pendingRef.current) pendingRef.current.created = true;
					await quickCreate.reload();
				},
				table: { toggleAllRowsSelected: () => {} },
				// eslint-disable-next-line @typescript-eslint/no-explicit-any
			}) as unknown as UseDataTable<any>,
		[quickCreate, pendingRef],
	);

	const dialog = useMemo(() => ({ open, setOpen }), [open, setOpen]);

	if (!open) return null;

	return (
		// Chặn sự kiện submit của form con lan (qua portal) lên form cha
		<div className='contents' onSubmit={(e) => e.stopPropagation()}>
			<MetaBreadcrumbOverrideContext.Provider value={quickCreate.label}>
				<DialogContext.Provider value={dialog}>
					<DataTableEditDialog
						type='Tạo mới'
						crumb={quickCreate.label}
						trigger={<span className='hidden' />}
					>
						{quickCreate.render({ data })}
					</DataTableEditDialog>
				</DialogContext.Provider>
			</MetaBreadcrumbOverrideContext.Provider>
		</div>
	);
}
